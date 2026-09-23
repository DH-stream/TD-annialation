using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

// "Ultimate" merge of two earlier versions:
// - Persistence (this file): every field that needs to survive a domain reload
//   is marked [SerializeField]. Without that attribute Unity does NOT serialize
//   private fields on an EditorWindow/ScriptableObject, so the chat would silently
//   reset on every recompile -- that was a real bug in one of the earlier drafts.
// - Robust JSON (this file): requests are built and responses parsed with
//   Newtonsoft.Json (JObject/JsonConvert) instead of hand-rolled string building
//   or regex-against-raw-JSON. Hand-rolled JSON broke on edge cases (special
//   characters, nested "text" fields in response metadata).
// - Chat-vs-action routing (from the newer draft): the model is told to only
//   emit C# code when the user actually wants Unity changed, and to just reply
//   normally otherwise -- much more natural for back-and-forth discussion.
// - Diagnostics (from the newer draft): HTTP status codes and common C#
//   compiler error codes are translated into short Swedish explanations, and a
//   404 model-not-found automatically falls back to the default model and retries.
[InitializeOnLoad]
public class GeminiEditorChat : EditorWindow
{
    private const string DefaultModel = "gemini-3.6-flash";
    private static readonly string[] GeminiModels = { "gemini-3.6-flash", "gemini-2.5-flash" };

    private const string SessionKeyPendingType = "GeminiPendingExecuteType";
    private const string SessionKeyPendingFile = "GeminiPendingTempFile";
    private const string EditorPrefKeyApiKey = "Gemini_API_Key";
    private const string EditorPrefKeyModel = "Gemini_Model";

    [Serializable]
    private class ConversationMessage
    {
        public string role; // "user" or "model"
        public string text;

        public ConversationMessage(string role, string text)
        {
            this.role = role;
            this.text = text;
        }
    }

    [Serializable]
    private class ExportData
    {
        public string timestampUtc;
        public string model;
        public string prompt;
        public string agentResponse;
        public string generatedCode;
        public string rawApiResponse;
    }

    // --- Persisted state: [SerializeField] so it survives domain reloads ---
    [SerializeField] private string apiKey = "";
    [SerializeField] private string selectedModel = DefaultModel;
    [SerializeField] private string userInstruction = "";
    [SerializeField] private List<ConversationMessage> conversation = new List<ConversationMessage>();
    [SerializeField] private string latestPrompt = "";
    [SerializeField] private string latestAgentResponse = "";
    [SerializeField] private string latestGeneratedCode = "";
    [SerializeField] private string lastRawResponseJson = "";
    [SerializeField] private string lastErrorDebugText = "";
    [SerializeField] private string lastErrorSummary = "";

    // --- Transient runtime state: fine to reset on reload ---
    private Vector2 scrollPosChat;
    private Vector2 scrollPosInput;
    private bool isWorking = false;

    static GeminiEditorChat()
    {
        // [InitializeOnLoad] guarantees this static constructor re-runs after every
        // domain reload, so this subscription is always restored even though plain
        // event subscriptions do not survive a reload on their own.
        AssemblyReloadEvents.afterAssemblyReload += RunPendingExecution;
    }

    private static void RunPendingExecution()
    {
        string pendingType = SessionState.GetString(SessionKeyPendingType, "");
        if (string.IsNullOrEmpty(pendingType))
        {
            return;
        }

        SessionState.EraseString(SessionKeyPendingType);
        string tempFilePath = SessionState.GetString(SessionKeyPendingFile, "");
        SessionState.EraseString(SessionKeyPendingFile);

        GeminiEditorChat window = Resources.FindObjectsOfTypeAll<GeminiEditorChat>().FirstOrDefault();

        try
        {
            Type type = Type.GetType(pendingType);
            if (type == null)
            {
                // Type.GetType only checks the calling assembly + mscorlib by default;
                // search every loaded assembly too before giving up.
                foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
                {
                    type = asm.GetType(pendingType);
                    if (type != null) break;
                }
            }

            if (type == null)
            {
                window?.AppendMessage("model", "Koden kompilerades inte (typen hittades inte efter omladdning). Kolla Unity Console för exakta kompilatorfel.");
                return;
            }

            MethodInfo method = type.GetMethod("Execute", BindingFlags.Public | BindingFlags.Static);
            if (method == null)
            {
                window?.AppendMessage("model", "Hittade ingen Execute-metod i den genererade klassen.");
                return;
            }

            method.Invoke(null, null);
            window?.AppendMessage("model", "Klart! Åtgärden utfördes i Unity.");
        }
        catch (TargetInvocationException tie)
        {
            string detail = tie.InnerException != null ? tie.InnerException.ToString() : tie.ToString();
            if (window != null)
            {
                window.lastErrorDebugText = window.BuildDebugPayload(null, detail);
                window.AppendMessage("model", "Körningsfel:\n" + detail);
            }
        }
        catch (Exception e)
        {
            window?.AppendMessage("model", "Körningsfel:\n" + e.Message);
        }
        finally
        {
            if (!string.IsNullOrEmpty(tempFilePath) && System.IO.File.Exists(tempFilePath))
            {
                AssetDatabase.DeleteAsset(tempFilePath);
            }
        }
    }

    [MenuItem("Tools/Gemini AI Agent")]
    public static void ShowWindow()
    {
        GetWindow<GeminiEditorChat>("Gemini Agent");
    }

    private void OnEnable()
    {
        // apiKey/selectedModel are [SerializeField], so a domain reload already
        // restores them. EditorPrefs is only the fallback for a brand new window.
        if (string.IsNullOrEmpty(apiKey))
        {
            apiKey = EditorPrefs.GetString(EditorPrefKeyApiKey, "");
        }

        if (string.IsNullOrEmpty(selectedModel) || Array.IndexOf(GeminiModels, selectedModel) < 0)
        {
            selectedModel = EditorPrefs.GetString(EditorPrefKeyModel, DefaultModel);
            if (Array.IndexOf(GeminiModels, selectedModel) < 0)
            {
                selectedModel = DefaultModel;
            }
        }

        if (conversation == null)
        {
            conversation = new List<ConversationMessage>();
        }
    }

    private void OnGUI()
    {
        GUILayout.Label("Gemini Unity Agent", EditorStyles.boldLabel);

        DrawToolbar();

        EditorGUI.BeginChangeCheck();
        apiKey = EditorGUILayout.PasswordField("API Key:", apiKey);
        if (EditorGUI.EndChangeCheck())
        {
            EditorPrefs.SetString(EditorPrefKeyApiKey, apiKey);
        }

        EditorGUILayout.Space(6);
        DrawChat();

        if (isWorking)
        {
            EditorGUILayout.Space(4);
            var thinkingStyle = new GUIStyle(EditorStyles.label) { fontStyle = FontStyle.Italic };
            GUI.contentColor = new Color(0.65f, 0.85f, 1f);
            GUILayout.Label("AI tänker...", thinkingStyle);
            GUI.contentColor = Color.white;
        }

        EditorGUILayout.Space(8);
        DrawInputArea();
    }

    private void DrawToolbar()
    {
        EditorGUILayout.BeginHorizontal();

        int modelIndex = Mathf.Max(0, Array.IndexOf(GeminiModels, selectedModel));
        int newModelIndex = EditorGUILayout.Popup("Model:", modelIndex, GeminiModels, GUILayout.Width(220));
        if (newModelIndex != modelIndex)
        {
            selectedModel = GeminiModels[newModelIndex];
            EditorPrefs.SetString(EditorPrefKeyModel, selectedModel);
        }

        GUILayout.FlexibleSpace();

        GUI.enabled = !isWorking && !string.IsNullOrEmpty(latestAgentResponse);
        if (GUILayout.Button("Exportera senaste svar", GUILayout.Width(170)))
        {
            ExportLatestResponse();
        }

        GUI.enabled = !isWorking && conversation.Count > 0;
        if (GUILayout.Button("Rensa chatt", GUILayout.Width(110)))
        {
            if (EditorUtility.DisplayDialog(
                "Rensa chatt?",
                "Detta rensar hela den aktuella konversationen. Vill du fortsätta?",
                "Rensa",
                "Avbryt"))
            {
                ClearConversation();
            }
        }

        GUI.enabled = true;
        EditorGUILayout.EndHorizontal();
    }

    private void DrawChat()
    {
        scrollPosChat = EditorGUILayout.BeginScrollView(scrollPosChat, GUILayout.Height(330), GUILayout.MinHeight(200));
        GUILayout.Space(8);

        if (conversation.Count == 0)
        {
            EditorGUILayout.LabelField(
                "AI Agent redo! Jag har tillgång till hela Unity Editor API:et via C#-skript.",
                EditorStyles.wordWrappedLabel);
        }

        foreach (var message in conversation)
        {
            DrawChatMessage(message);
        }

        GUILayout.Space(8);
        EditorGUILayout.EndScrollView();
    }

    private void DrawChatMessage(ConversationMessage message)
    {
        bool isUser = string.Equals(message.role, "user", StringComparison.OrdinalIgnoreCase);

        GUILayout.BeginVertical(GUILayout.ExpandWidth(true));

        var roleStyle = new GUIStyle(EditorStyles.label) { fontStyle = FontStyle.Bold };
        var messageStyle = new GUIStyle(EditorStyles.label)
        {
            wordWrap = true,
            richText = true,
            padding = new RectOffset(0, 0, 2, 8),
            margin = new RectOffset(0, 0, 0, 8)
        };

        // Plain chat log -- no pills/bubbles/borders, just a bold sender label
        // above wrapped message text, with a subtle color to tell speakers apart.
        GUI.contentColor = isUser ? new Color(0.65f, 0.85f, 1f) : new Color(0.75f, 0.75f, 0.75f);
        GUILayout.Label(isUser ? "Du" : "AI", roleStyle);
        GUI.contentColor = Color.white;
        GUILayout.Label(message.text, messageStyle);

        GUILayout.Space(4);
        GUILayout.EndVertical();
    }

    private void DrawInputArea()
    {
        GUILayout.Label("Meddelande", EditorStyles.label);

        var textAreaStyle = new GUIStyle(EditorStyles.textArea) { wordWrap = true };
        int lineCount = string.IsNullOrEmpty(userInstruction) ? 1 : userInstruction.Split('\n').Length;
        float calculatedHeight = Mathf.Clamp(lineCount * 18 + 25, 50, 150);

        scrollPosInput = EditorGUILayout.BeginScrollView(scrollPosInput, GUILayout.Height(calculatedHeight));
        userInstruction = EditorGUILayout.TextArea(userInstruction, textAreaStyle, GUILayout.ExpandHeight(true));
        EditorGUILayout.EndScrollView();

        EditorGUILayout.Space(6);

        GUI.enabled = !isWorking && !string.IsNullOrEmpty(apiKey.Trim()) && !string.IsNullOrEmpty(userInstruction.Trim());
        if (GUILayout.Button("Skicka", GUILayout.Height(34)))
        {
            SendCurrentInstruction();
        }
        GUI.enabled = true;

        HandleKeyboardShortcut();
    }

    private void SendCurrentInstruction()
    {
        string currentPrompt = userInstruction.Trim();
        userInstruction = "";
        GUI.FocusControl(null);

        AppendMessage("user", currentPrompt);
        StartAsyncProcess(currentPrompt);
    }

    private void HandleKeyboardShortcut()
    {
        Event e = Event.current;
        if (e.type != EventType.KeyDown) return;

        if (e.keyCode == KeyCode.Return && e.control && !isWorking)
        {
            if (!string.IsNullOrWhiteSpace(userInstruction) && !string.IsNullOrWhiteSpace(apiKey))
            {
                SendCurrentInstruction();
                e.Use();
            }
        }
    }

    private void AppendMessage(string role, string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return;

        conversation.Add(new ConversationMessage(role, text));
        scrollPosChat.y = float.MaxValue;
        Repaint();
    }

    private void StartAsyncProcess(string prompt)
    {
        if (string.IsNullOrWhiteSpace(prompt)) return;

        isWorking = true;
        latestPrompt = prompt;
        latestAgentResponse = "";
        latestGeneratedCode = "";
        Repaint();

        string cleanKey = apiKey.Trim();
        string sceneContext = GetSceneContext();
        string systemPrompt = BuildSystemPrompt(sceneContext);
        string jsonBody = BuildGeminiRequestJson(systemPrompt);
        string url = "https://generativelanguage.googleapis.com/v1beta/models/" + selectedModel + ":generateContent?key=" + cleanKey;

        var www = new UnityWebRequest(url, "POST");
        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);
        www.uploadHandler = new UploadHandlerRaw(bodyRaw);
        www.downloadHandler = new DownloadHandlerBuffer();
        www.SetRequestHeader("Content-Type", "application/json");

        var asyncOp = www.SendWebRequest();
        asyncOp.completed += (op) => HandleResponse(www, prompt);
    }

    private void HandleResponse(UnityWebRequest www, string prompt)
    {
        bool retry = false;

        try
        {
            if (www.result == UnityWebRequest.Result.Success)
            {
                string rawJson = www.downloadHandler.text;
                lastRawResponseJson = rawJson;

                string responseText = ExtractTextFromGeminiResponse(rawJson);
                latestAgentResponse = responseText;

                if (string.IsNullOrWhiteSpace(responseText))
                {
                    AppendMessage("model", "Jag fick inget användbart svar från Gemini.");
                }
                else if (TryExtractCSharpCode(responseText, out string generatedCode))
                {
                    latestGeneratedCode = generatedCode;
                    AppendMessage("model", "Jag har genererat ändringen och kompilerar den nu...");
                    ExecuteCSharpCode(generatedCode);
                }
                else
                {
                    AppendMessage("model", responseText);
                }
            }
            else
            {
                string responseDetails = www.downloadHandler != null ? www.downloadHandler.text : "";
                lastErrorDebugText = BuildDebugPayload(www, responseDetails);

                if (www.responseCode == 404 && !string.Equals(selectedModel, DefaultModel, StringComparison.Ordinal))
                {
                    selectedModel = DefaultModel;
                    EditorPrefs.SetString(EditorPrefKeyModel, selectedModel);
                    AppendMessage("model", "Modellen hittades inte. Jag byter till " + DefaultModel + " och försöker igen...");
                    retry = true;
                }
                else
                {
                    lastErrorSummary = ExplainError(responseDetails + "\n" + www.error + "\nstatus=" + www.responseCode);
                    AppendMessage("model", "Fel (" + www.responseCode + "): " + lastErrorSummary);
                }
            }
        }
        catch (Exception e)
        {
            lastErrorDebugText = BuildDebugPayload(www, e.ToString());
            lastErrorSummary = ExplainError(e.ToString());
            AppendMessage("model", "Ett oväntat fel uppstod:\n" + lastErrorSummary);
        }
        finally
        {
            www.Dispose();
            isWorking = false;
            Repaint();
        }

        // Retrying happens *after* the finally block above has already cleaned up
        // this request, so the recursive call below gets a clean isWorking state
        // instead of having it stomped back to false right after starting.
        if (retry)
        {
            StartAsyncProcess(prompt);
        }
    }

    private string BuildSystemPrompt(string sceneContext)
    {
        return @"Du är en senior Unity-agent som arbetar direkt i Unity Editor.

Du har tillgång till Unity Editor API genom C#-kod som exekveras i projektet.

Du kan:
- ändra scener
- skapa och ändra GameObjects
- ändra prefabs
- skapa och ändra scripts
- ändra material och shaders
- skapa UI
- ändra projektinställningar
- analysera Unity-projektet
- hjälpa användaren att förstå och felsöka projektet

Du ska skilja på två typer av meddelanden:

1. OM användaren ber dig att faktiskt ändra eller skapa något i Unity:
Svara ENDAST med giltig C#-kod omsluten i:

```csharp
using UnityEngine;
using UnityEditor;

public class TemporaryGeminiAction
{
    public static void Execute()
    {
        // kod
    }
}
```

Ingen annan text får finnas utanför kodblocket.

2. OM användaren bara vill prata, ställa en fråga, diskutera en idé eller få en förklaring:
   Svara normalt på svenska. Generera INTE någon C#-kod.

Du får använda information från tidigare meddelanden i konversationen.

Viktigt för Unity Animator:
- Trigger-parametrar i AnimatorController använder endast AnimatorConditionMode.If eller AnimatorConditionMode.IfNot.
- Använd aldrig AnimatorConditionMode.Trigger; det finns inte i Unity API.
- Matcha exakt namn på parametrar och states som finns i controllern.

Var konkret, teknisk och ärlig. Hitta inte på Unity API, objekt, komponenter eller projektstruktur som du inte känner till.

Nuvarande scen-status:
" + sceneContext;
    }

    private string BuildGeminiRequestJson(string systemPrompt)
    {
        // Newtonsoft handles all escaping (quotes, newlines, unicode) correctly,
        // which removes the class of "unterminated string" bugs hand-rolled JSON
        // building runs into. System prompt goes in systemInstruction (sent once
        // per request by the API itself) rather than being re-prepended to every
        // user turn's text.
        var contents = conversation
            .Where(m => !string.IsNullOrWhiteSpace(m.text))
            .Select(m => new
            {
                role = m.role == "user" ? "user" : "model",
                parts = new[] { new { text = m.text } }
            })
            .ToArray();

        var body = new
        {
            systemInstruction = new { parts = new[] { new { text = systemPrompt } } },
            contents = contents
        };

        return JsonConvert.SerializeObject(body);
    }

    private string ExtractTextFromGeminiResponse(string rawJson)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(rawJson)) return "";

            JObject obj = JObject.Parse(rawJson);
            string text = (string)obj["candidates"]?[0]?["content"]?["parts"]?[0]?["text"] ?? "";
            return text.Trim();
        }
        catch (Exception e)
        {
            Debug.LogError("Fel vid tolkning av Gemini-svar: " + e.Message);
            return "";
        }
    }

    private bool TryExtractCSharpCode(string response, out string code)
    {
        code = "";
        if (string.IsNullOrWhiteSpace(response)) return false;

        Match match = Regex.Match(response, @"```(?:csharp|cs|C#)\s*([\s\S]*?)```", RegexOptions.IgnoreCase);
        if (!match.Success) return false;

        code = match.Groups[1].Value.Trim();
        return !string.IsNullOrWhiteSpace(code);
    }

    private void ExecuteCSharpCode(string code)
    {
        if (string.IsNullOrEmpty(code))
        {
            AppendMessage("model", "Koden var tom eller kunde inte tolkas.");
            return;
        }

        try
        {
            string tempFilePath = "Assets/Editor/TempGeminiAction.cs";
            System.IO.File.WriteAllText(tempFilePath, code);

            // Persist what to run once compilation + domain reload are truly
            // finished -- a plain delayCall fires after one frame, long before
            // that's actually true.
            SessionState.SetString(SessionKeyPendingType, "TemporaryGeminiAction");
            SessionState.SetString(SessionKeyPendingFile, tempFilePath);

            AppendMessage("model", "Kompilerar och kör ändringen...");
            AssetDatabase.Refresh();
        }
        catch (Exception e)
        {
            lastErrorDebugText = BuildDebugPayload(null, e.ToString());
            lastErrorSummary = ExplainError(e.ToString());
            AppendMessage("model", "Genereringsfel:\n" + lastErrorSummary);
        }
    }

    private void ExportLatestResponse()
    {
        if (string.IsNullOrWhiteSpace(latestAgentResponse))
        {
            EditorUtility.DisplayDialog("Gemini Agent", "Det finns inget AI-svar att exportera ännu.", "OK");
            return;
        }

        string path = EditorUtility.SaveFilePanel("Exportera senaste AI-svar", "", "gemini-agent-response.json", "json");
        if (string.IsNullOrEmpty(path)) return;

        var data = new ExportData
        {
            timestampUtc = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
            model = selectedModel,
            prompt = latestPrompt,
            agentResponse = latestAgentResponse,
            generatedCode = latestGeneratedCode,
            rawApiResponse = lastRawResponseJson
        };

        string json = JsonConvert.SerializeObject(data, Formatting.Indented);
        System.IO.File.WriteAllText(path, json, Encoding.UTF8);

        AppendMessage("model", "Exporterade senaste svar till:\n" + path);
        EditorUtility.RevealInFinder(path);
    }

    private string BuildDebugPayload(UnityWebRequest www, string responseDetails)
    {
        var sb = new StringBuilder();
        sb.AppendLine("=== Gemini AI Debug Export ===");
        sb.AppendLine("Model: " + selectedModel);
        sb.AppendLine("Timestamp: " + DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"));
        sb.AppendLine("API key present: " + (!string.IsNullOrWhiteSpace(apiKey)));

        if (www != null)
        {
            sb.AppendLine("HTTP status: " + www.responseCode);
            sb.AppendLine("Unity error: " + www.error);
        }

        sb.AppendLine("\n--- Raw debug ---");
        sb.AppendLine(responseDetails ?? "");

        return sb.ToString();
    }

    private void ClearConversation()
    {
        if (isWorking) return;

        conversation.Clear();
        latestPrompt = "";
        latestAgentResponse = "";
        latestGeneratedCode = "";
        lastRawResponseJson = "";
        lastErrorDebugText = "";
        lastErrorSummary = "";
        scrollPosChat = Vector2.zero;
        Repaint();
    }

    private string GetSceneContext()
    {
        GameObject[] rootObjects = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();
        var context = new StringBuilder();
        context.AppendLine("Objekt i nuvarande scen:");
        foreach (var obj in rootObjects)
        {
            context.AppendLine("- " + obj.name);
        }
        return context.ToString();
    }

    private string ExplainError(string rawError)
    {
        if (string.IsNullOrWhiteSpace(rawError)) return "Ingen feltext kunde läsas.";

        string text = rawError.Trim();

        string statusCode = Regex.Match(text, @"status\s*=\s*(\d+)", RegexOptions.IgnoreCase).Groups[1].Value;
        if (!string.IsNullOrEmpty(statusCode))
        {
            switch (statusCode)
            {
                case "404": return "404: Modellen finns inte eller är inte tillgänglig för den här API-nyckeln.";
                case "401": return "401: API-nyckeln är ogiltig, saknas eller saknar behörighet.";
                case "429": return "429: Gemini quota eller krediter är slut för den här API-nyckeln.";
            }
        }

        Match csMatch = Regex.Match(text, @"CS\d+", RegexOptions.IgnoreCase);
        if (csMatch.Success)
        {
            switch (csMatch.Value.ToUpperInvariant())
            {
                case "CS0103": return "En variabel, metod eller typ hittades inte i aktuell scope.";
                case "CS0117": return "Den refererade medlemmen finns inte på typen.";
                case "CS0246": return "En typ eller namespace hittades inte.";
                case "CS1002": return "Det verkar saknas syntax, exempelvis semikolon.";
                case "CS1061": return "Den refererade komponenten eller medlemmen finns inte på typen.";
                case "CS1503": return "Fel typ skickades som argument till en metod.";
                case "CS0118": return "Ett namn används på felaktigt sätt eller skuggas av ett annat namn.";
                case "CS0029": return "Två inkompatibla typer försöker konverteras.";
                case "CS0106": return "Ett modifierings- eller scopefel finns i koden.";
                default: return "Ett C#-kompileringsfel hittades. Kontrollera Unity Console för detaljer.";
            }
        }

        if (Regex.IsMatch(text, @"(?i)NullReferenceException|object\s+reference\s+not\s+set"))
        {
            return "NullReferenceException: koden försöker använda ett objekt som är null.";
        }

        if (Regex.IsMatch(text, @"(?i)not\s+found|unable\s+to\s+find|cannot\s+find"))
        {
            return "Unity kunde inte hitta något som koden refererade till.";
        }

        return "Ett fel uppstod i Unity eller i den AI-genererade koden. Kontrollera Unity Console.";
    }
}
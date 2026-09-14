using UnityEngine;
using UnityEditor;
using UnityEngine.Networking;
using System.Collections.Generic;
using System.Reflection;
using System;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

[InitializeOnLoad]
public class GeminiEditorChat : EditorWindow
{
    [Serializable]
    private class ChatTurn
    {
        public string role; // "user" or "model"
        public string text;
    }

    [SerializeField] private string apiKey = "";
    [SerializeField] private string userInstruction = "";
    [SerializeField] private List<ChatTurn> conversation = new List<ChatTurn>();
    [SerializeField] private string lastResponseText = "";
    [SerializeField] private string lastRawResponseJson = "";
    [SerializeField] private string lastPromptSent = "";

    private Vector2 scrollPosChat;
    private Vector2 scrollPosInput;
    private bool isWorking = false;

    private const string SessionKeyPendingType = "GeminiPendingExecuteType";
    private const string SessionKeyPendingFile = "GeminiPendingTempFile";

    // [InitializeOnLoad] guarantees this static constructor re-runs after every
    // domain reload, so this subscription is always restored even though plain
    // event subscriptions do not survive a reload on their own.
    static GeminiEditorChat()
    {
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
                    if (type != null)
                    {
                        break;
                    }
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
            window?.AppendMessage("model", "Åtgärden utfördes!");
        }
        catch (TargetInvocationException tie)
        {
            window?.AppendMessage("model", "Körningsfel: " + (tie.InnerException != null ? tie.InnerException.ToString() : tie.ToString()));
        }
        catch (Exception e)
        {
            window?.AppendMessage("model", "Körningsfel: " + e.Message);
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
        apiKey = EditorPrefs.GetString("Gemini_API_Key", "");
        if (conversation == null)
        {
            conversation = new List<ChatTurn>();
        }
    }

    private void AppendMessage(string role, string text)
    {
        conversation.Add(new ChatTurn { role = role, text = text });
        if (role == "model")
        {
            lastResponseText = text;
        }
        Repaint();
    }

    private void OnGUI()
    {
        GUILayout.Label("Gemini Unity MCP Agent", EditorStyles.boldLabel);

        EditorGUI.BeginChangeCheck();
        apiKey = EditorGUILayout.PasswordField("API Key:", apiKey);
        if (EditorGUI.EndChangeCheck())
        {
            EditorPrefs.SetString("Gemini_API_Key", apiKey);
        }

        EditorGUILayout.Space();

        // Plain chat log -- no pills/bubbles/borders, just a bold sender label above
        // wrapped message text, similar to a normal AI chat transcript.
        GUIStyle senderStyle = new GUIStyle(EditorStyles.boldLabel);
        GUIStyle textStyle = new GUIStyle(EditorStyles.wordWrappedLabel) { richText = false };

        scrollPosChat = EditorGUILayout.BeginScrollView(scrollPosChat, GUILayout.ExpandHeight(true));
        if (conversation.Count == 0)
        {
            EditorGUILayout.LabelField("AI Agent redo! Jag har tillgång till hela Unity Editor API:et via C#-skript.", textStyle);
        }
        foreach (var turn in conversation)
        {
            string sender = turn.role == "user" ? "Du" : "AI";
            EditorGUILayout.LabelField(sender, senderStyle);
            EditorGUILayout.LabelField(turn.text, textStyle);
            EditorGUILayout.Space(10);
        }
        EditorGUILayout.EndScrollView();

        EditorGUILayout.Space();

        GUILayout.Label("Instruktion till AI:", EditorStyles.label);

        GUIStyle textAreaStyle = new GUIStyle(EditorStyles.textArea) { wordWrap = true };

        int lineCount = userInstruction.Split('\n').Length;
        float calculatedHeight = Mathf.Clamp(lineCount * 18 + 25, 50, 150);

        scrollPosInput = EditorGUILayout.BeginScrollView(scrollPosInput, GUILayout.Height(calculatedHeight));
        userInstruction = EditorGUILayout.TextArea(userInstruction, textAreaStyle, GUILayout.ExpandHeight(true));
        EditorGUILayout.EndScrollView();

        EditorGUILayout.Space();

        EditorGUILayout.BeginHorizontal();

        GUI.enabled = !isWorking && !string.IsNullOrEmpty(apiKey.Trim()) && !string.IsNullOrEmpty(userInstruction.Trim());
        if (GUILayout.Button(isWorking ? "Tänker..." : "Skicka", GUILayout.Height(30)))
        {
            string currentPrompt = userInstruction;
            userInstruction = "";
            GUI.FocusControl(null);
            AppendMessage("user", currentPrompt);
            StartAsyncProcess();
        }
        GUI.enabled = true;

        GUI.enabled = !string.IsNullOrEmpty(lastResponseText);
        if (GUILayout.Button("Exportera senaste svar (JSON)", GUILayout.Height(30), GUILayout.Width(220)))
        {
            ExportLastResponseAsJson();
        }
        GUI.enabled = true;

        EditorGUILayout.EndHorizontal();
    }

    private void ExportLastResponseAsJson()
    {
        string path = EditorUtility.SaveFilePanel("Exportera senaste AI-svar", Application.dataPath, "gemini_last_response", "json");
        if (string.IsNullOrEmpty(path))
        {
            return;
        }

        var export = new
        {
            timestamp = DateTime.UtcNow.ToString("o"),
            prompt = lastPromptSent,
            extractedResponse = lastResponseText,
            rawApiResponse = lastRawResponseJson
        };

        string json = JsonConvert.SerializeObject(export, Formatting.Indented);
        System.IO.File.WriteAllText(path, json);
        EditorUtility.RevealInFinder(path);
    }

    private void StartAsyncProcess()
    {
        isWorking = true;
        Repaint();

        string cleanKey = apiKey.Trim();
        string sceneContext = GetSceneContext();

        string systemPrompt =
@"Du är en expertnivå-agent i Unity Editor. Du för en pågående konversation med användaren
och ska generera ren C#-kod som exekveras direkt i Unity Editor för varje uppgift.
Svara BARA med giltig C#-kod omsluten av ```csharp och ```. Ingen annan förklarande text före eller efter.
Använd tidigare meddelanden i konversationen som kontext när det är relevant för uppgiften.

Koden MÅSTE följa denna struktur exakt:
using UnityEngine;
using UnityEditor;

public class TemporaryGeminiAction
{
    public static void Execute()
    {
        // Din kod här
    }
}

Nuvarande scen-status:
" + sceneContext;

        lastPromptSent = conversation.Count > 0 ? conversation[conversation.Count - 1].text : "";

        string url = "https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent?key=" + cleanKey;
        string jsonBody = BuildRequestBody(systemPrompt);

        UnityWebRequest www = new UnityWebRequest(url, "POST");
        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);
        www.uploadHandler = new UploadHandlerRaw(bodyRaw);
        www.downloadHandler = new DownloadHandlerBuffer();
        www.SetRequestHeader("Content-Type", "application/json");

        var asyncOp = www.SendWebRequest();
        asyncOp.completed += (op) =>
        {
            lastRawResponseJson = www.downloadHandler != null ? www.downloadHandler.text : "";

            if (www.result == UnityWebRequest.Result.Success)
            {
                string responseText = ExtractTextFromGeminiResponse(lastRawResponseJson);
                AppendMessage("model", responseText);
                ExecuteCSharpCode(responseText);
            }
            else
            {
                AppendMessage("model", "Fel (" + www.responseCode + "): " + www.error + "\n" + lastRawResponseJson);
            }
            www.Dispose();
            isWorking = false;
            Repaint();
        };
    }

    // Building the request via Newtonsoft.Json (instead of hand-escaped string
    // concatenation) removes the whole class of "unterminated string" bugs we hit
    // earlier -- the serializer handles every quote/newline/backslash correctly.
    private string BuildRequestBody(string systemPrompt)
    {
        var contents = conversation.Select(t => new
        {
            role = t.role == "user" ? "user" : "model",
            parts = new[] { new { text = t.text } }
        }).ToArray();

        var body = new
        {
            systemInstruction = new { parts = new[] { new { text = systemPrompt } } },
            contents = contents
        };

        return JsonConvert.SerializeObject(body);
    }

    private string GetSceneContext()
    {
        GameObject[] rootObjects = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();
        string context = "Objekt i nuvarande scen:\n";
        foreach (var obj in rootObjects)
        {
            context += "- " + obj.name + "\n";
        }
        return context;
    }

    private string ExtractTextFromGeminiResponse(string rawJson)
    {
        try
        {
            JObject obj = JObject.Parse(rawJson);
            string text = (string)obj["candidates"]?[0]?["content"]?["parts"]?[0]?["text"] ?? "";

            if (text.Contains("```csharp"))
            {
                int codeStart = text.IndexOf("```csharp") + 9;
                int codeEnd = text.IndexOf("```", codeStart);
                if (codeEnd > codeStart)
                {
                    text = text.Substring(codeStart, codeEnd - codeStart);
                }
            }
            return text.Trim();
        }
        catch (Exception e)
        {
            Debug.LogError("Fel vid tolkning av Gemini-svar: " + e.Message);
            return "";
        }
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

            // Persist what to run once compilation + domain reload are truly finished --
            // a plain delayCall fires after one frame, long before that's actually true.
            SessionState.SetString(SessionKeyPendingType, "TemporaryGeminiAction");
            SessionState.SetString(SessionKeyPendingFile, tempFilePath);

            AppendMessage("model", "Kompilerar och kör efter omladdning...");
            AssetDatabase.Refresh();
        }
        catch (Exception e)
        {
            AppendMessage("model", "Genereringsfel: " + e.Message);
            Debug.LogError(e);
        }
    }
}
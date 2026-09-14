using UnityEngine;
using UnityEditor;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using System;

public class GeminiEditorChat : EditorWindow
{
    private string apiKey = "";
    private string userInstruction = "";
    private string chatHistory = "AI Agent redo! Jag har tillgång till hela Unity Editor API:et via C#-skript.\n";
    private Vector2 scrollPosChat;
    private Vector2 scrollPosInput;
    private bool isWorking = false;

    [MenuItem("Tools/Gemini AI Agent")]
    public static void ShowWindow()
    {
        GetWindow<GeminiEditorChat>("Gemini Agent");
    }

    private void OnEnable()
    {
        apiKey = EditorPrefs.GetString("Gemini_API_Key", "");
    }

    private void OnGUI()
    {
        GUILayout.Label("Gemini Unity MCP Agent", EditorStyles.boldLabel);

        // API Key Fält
        EditorGUI.BeginChangeCheck();
        apiKey = EditorGUILayout.PasswordField("API Key:", apiKey);
        if (EditorGUI.EndChangeCheck())
        {
            EditorPrefs.SetString("Gemini_API_Key", apiKey);
        }

        EditorGUILayout.Space();

        // Chatt-historik (Expanderar för att fylla utrymmet)
        scrollPosChat = EditorGUILayout.BeginScrollView(scrollPosChat, GUILayout.ExpandHeight(true));
        EditorGUILayout.TextArea(chatHistory, GUILayout.ExpandHeight(true));
        EditorGUILayout.EndScrollView();

        EditorGUILayout.Space();

        // Expanderande Input-ruta
        GUILayout.Label("Instruktion till AI:", EditorStyles.label);
        
        GUIStyle textAreaStyle = new GUIStyle(EditorStyles.textArea) 
        { 
            wordWrap = true 
        };
        
        // Beräkna höjden dynamiskt baserat på antal rader i texten (Mellan 50px och 150px)
        int lineCount = userInstruction.Split('\n').Length;
        float calculatedHeight = Mathf.Clamp(lineCount * 18 + 25, 50, 150);

        scrollPosInput = EditorGUILayout.BeginScrollView(scrollPosInput, GUILayout.Height(calculatedHeight));
        userInstruction = EditorGUILayout.TextArea(userInstruction, textAreaStyle, GUILayout.ExpandHeight(true));
        EditorGUILayout.EndScrollView();

        EditorGUILayout.Space();

        // Knappen Verkställ
        GUI.enabled = !isWorking && !string.IsNullOrEmpty(apiKey.Trim()) && !string.IsNullOrEmpty(userInstruction.Trim());
        if (GUILayout.Button(isWorking ? "Tänker..." : "Verkställ i Unity", GUILayout.Height(30)))
        {
            chatHistory += "\nDu: " + userInstruction + "\n";
            string currentPrompt = userInstruction;
            userInstruction = "";
            GUI.FocusControl(null); // Töm fokus från textrutan
            StartAsyncProcess(currentPrompt);
        }
        GUI.enabled = true;
    }

    private void StartAsyncProcess(string prompt)
    {
        isWorking = true;
        Repaint();

        // Rensa bort osynliga tecken och mellanslag som kan ge 404
        string cleanKey = apiKey.Trim();

        string sceneContext = GetSceneContext();
        string systemPrompt = @"
Du är en expertnivå-agent i Unity Editor. Du ska generera Ren C#-kod som ska exekveras direkt i Unity Editor.
Svara BARA med giltig C#-kod omsluten av ```csharp och ```. Ingen annan förklarande text före eller efter.

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

        string fullPrompt = systemPrompt + "\n\nUppgift att utföra: " + prompt;
        
        // gemini-1.5-flash is no longer available for new API requests.
        string url = "https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent?key=" + cleanKey;
        
        string escapedPrompt = fullPrompt.Replace("\\", "\\\\")
                                         .Replace("\"", "\\\"")
                                         .Replace("\n", "\\n")
                                         .Replace("\r", "")
                                         .Replace("\t", "\\t");
                                         
        string jsonBody = "{\"contents\":[{\"parts\":[{\"text\":\"" + escapedPrompt + "\"}]}]}";

        UnityWebRequest www = new UnityWebRequest(url, "POST");
        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);
        www.uploadHandler = new UploadHandlerRaw(bodyRaw);
        www.downloadHandler = new DownloadHandlerBuffer();
        www.SetRequestHeader("Content-Type", "application/json");

        var asyncOp = www.SendWebRequest();
        asyncOp.completed += (op) =>
        {
            if (www.result == UnityWebRequest.Result.Success)
            {
                string responseText = ExtractTextFromGeminiResponse(www.downloadHandler.text);
                chatHistory += "AI: Genererade kod. Exekverar...\n";
                ExecuteCSharpCode(responseText);
            }
            else
            {
                string responseDetails = www.downloadHandler != null ? www.downloadHandler.text : "";
                chatHistory += "AI Fel (" + www.responseCode + "): " + www.error + "\n" + responseDetails + "\n";
            }
            www.Dispose();
            isWorking = false;
            Repaint();
        };
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
            int textStartIndex = rawJson.IndexOf("\"text\": \"") + 9;
            if (textStartIndex < 9) return "";
            
            int textEndIndex = rawJson.IndexOf("\"}\n", textStartIndex);
            if (textEndIndex == -1) textEndIndex = rawJson.IndexOf("\"}", textStartIndex);

            string text = rawJson.Substring(textStartIndex, textEndIndex - textStartIndex);
            text = System.Text.RegularExpressions.Regex.Unescape(text);

            if (text.Contains("```csharp"))
            {
                int codeStart = text.IndexOf("```csharp") + 9;
                int codeEnd = text.IndexOf("```", codeStart);
                text = text.Substring(codeStart, codeEnd - codeStart);
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
            chatHistory += "AI: Koden var tom eller kunde inte tolkas.\n";
            return;
        }

        try
        {
            string tempFilePath = "Assets/Editor/TempGeminiAction.cs";
            System.IO.File.WriteAllText(tempFilePath, code);
            AssetDatabase.Refresh();

            EditorApplication.delayCall += () =>
            {
                Type type = Type.GetType("TemporaryGeminiAction");
                if (type != null)
                {
                    MethodInfo method = type.GetMethod("Execute", BindingFlags.Public | BindingFlags.Static);
                    if (method != null)
                    {
                        method.Invoke(null, null);
                        chatHistory += "AI: Åtgärden utfördes!\n";
                    }
                }
                AssetDatabase.DeleteAsset(tempFilePath);
            };
        }
        catch (Exception e)
        {
            chatHistory += "Kompileringsfel: " + e.Message + "\n";
            Debug.LogError(e);
        }
    }
}
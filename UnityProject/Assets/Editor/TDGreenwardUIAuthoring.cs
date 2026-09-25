using TDAnnihilation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

public static class TDGreenwardUIAuthoring
{
    private const string UiFolder = "Assets/Resources/TDAnnihilation/UI";
    private const string PanelPath = UiFolder + "/GreenwardPanelSettings.asset";
    private const string RootPath = UiFolder + "/GreenwardRoot.uxml";

    [MenuItem("TD Annihilation/UI/Ensure Greenward UI")]
    public static void EnsureGreenwardUI()
    {
        TDVerticalSliceBootstrap bootstrap = Object.FindAnyObjectByType<TDVerticalSliceBootstrap>();
        if (bootstrap == null)
        {
            Debug.LogError("Open Greenward.unity with a TDVerticalSliceBootstrap before configuring UI.");
            return;
        }

        EnsureFolder("Assets/Resources");
        EnsureFolder("Assets/Resources/TDAnnihilation");
        EnsureFolder(UiFolder);

        PanelSettings panel = AssetDatabase.LoadAssetAtPath<PanelSettings>(PanelPath);
        if (panel == null)
        {
            panel = ScriptableObject.CreateInstance<PanelSettings>();
            AssetDatabase.CreateAsset(panel, PanelPath);
        }
        panel.scaleMode = PanelScaleMode.ScaleWithScreenSize;
        panel.referenceResolution = new Vector2Int(1280, 720);
        panel.match = 0.5f;
        EditorUtility.SetDirty(panel);

        VisualTreeAsset root = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(RootPath);
        if (root == null)
        {
            Debug.LogError("Missing GreenwardRoot.uxml at " + RootPath + ".");
            return;
        }

        UIDocument document = bootstrap.GetComponent<UIDocument>();
        if (document == null) document = bootstrap.gameObject.AddComponent<UIDocument>();
        document.panelSettings = panel;
        document.visualTreeAsset = root;
        EditorUtility.SetDirty(document);
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Configured Greenward UI Toolkit document and PanelSettings.");
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = System.IO.Path.GetDirectoryName(path).Replace('\\', '/');
        string folder = System.IO.Path.GetFileName(path);
        if (!AssetDatabase.IsValidFolder(parent)) EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, folder);
    }
}

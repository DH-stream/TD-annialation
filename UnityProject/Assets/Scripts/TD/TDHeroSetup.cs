using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

namespace TDAnnihilation
{
    /// <summary>
    /// Editor helper to set up the hero in the scene.
    /// </summary>
    public class TDHeroSetup : MonoBehaviour
    {
#if UNITY_EDITOR
        [MenuItem("TD Annihilation/Setup Hero in Scene")]
        public static void SetupHeroInScene()
        {
            // Find or create the hero
            TDHeroController existingHero = Object.FindAnyObjectByType<TDHeroController>();
            if (existingHero != null)
            {
                EditorUtility.DisplayDialog("Hero Already Exists", "A hero with TDHeroController already exists in the scene at: " + existingHero.gameObject.name, "OK");
                return;
            }

            // Load the evenlowerpoly model
            GameObject heroModel = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Main Char/evenlowerpoly.fbx");
            if (heroModel == null)
            {
                EditorUtility.DisplayDialog("Error", "Could not load Assets/Main Char/evenlowerpoly.fbx", "OK");
                return;
            }

            // Instantiate it in the scene
            Vector3 spawnPos = new Vector3(4f, 1f, 4f); // Default spawn position
            GameObject hero = null;
            
            // Try to find canvas/UI parent for instantiation
            Transform parent = null;
            RectTransform rectTransform = Object.FindAnyObjectByType<RectTransform>();
            if (rectTransform != null) parent = rectTransform.transform.parent;
            
            if (parent != null)
            {
                hero = PrefabUtility.InstantiatePrefab(heroModel, parent) as GameObject;
            }
            
            if (hero == null)
            {
                hero = Object.Instantiate(heroModel, spawnPos, Quaternion.Euler(0f, 25f, 0f));
            }

            hero.name = "Hero - Warden of Greenward";
            hero.transform.localScale = Vector3.one * TDPresentationScale.Hero;

            // Add the TDHeroController component
            TDHeroController controller = hero.GetComponent<TDHeroController>();
            if (controller == null)
            {
                controller = hero.AddComponent<TDHeroController>();
            }

            // Mark scene as dirty and select the hero
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Selection.activeGameObject = hero;

            EditorUtility.DisplayDialog("Success", "Hero created at " + hero.transform.position + " with scale " + hero.transform.localScale, "OK");
        }
#endif
    }
}

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace TDAnnihilation
{
    /// <summary>
    /// One-click editor authoring for Greenward's presentation baseline.
    /// This writes normal serialized scene objects/assets that remain directly editable
    /// in the Unity Inspector. Nothing here runs at game runtime.
    /// </summary>
    public static class GreenwardPresentationAuthoring
    {
        private const string PresentationRootName = "Greenward Presentation";
        private const string VolumeObjectName = "Greenward Global Volume";
        private const string ProbeObjectName = "Greenward Reflection Probe";
        private const string VolumeProfilePath = "Assets/Settings/GreenwardVolumeProfile.asset";
        private const string SkyMaterialFolder = "Assets/Art/Greenward/Materials";
        private const string SkyMaterialPath = SkyMaterialFolder + "/Greenward_Sky.mat";

        [MenuItem("TD Annihilation/Greenward/Presentation/Apply Suggested Baseline")]
        public static void ApplySuggestedBaseline()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid())
            {
                Debug.LogError("Open the Greenward scene before authoring presentation settings.");
                return;
            }

            Undo.SetCurrentGroupName("Author Greenward Presentation");
            int undoGroup = Undo.GetCurrentGroup();

            GameObject root = GetOrCreateRoot(PresentationRootName);

            Light sun = GetOrCreateSun(root.transform);
            ConfigureSun(sun);
            ConfigureEnvironment(sun);

            EnsureLocalLight(root.transform, "Forge Firelight",
                new Vector3(-7f, 2.1f, -4.8f), new Color(1f, 0.30f, 0.045f), 2.35f, 7f);
            EnsureLocalLight(root.transform, "Portal Glow",
                new Vector3(-43f, 3f, -10f), new Color(0.55f, 0.10f, 1f), 2.8f, 8.5f);
            EnsureLocalLight(root.transform, "Arcane Village Glow",
                new Vector3(-5f, 2.4f, -2f), new Color(0.16f, 0.30f, 1f), 1.25f, 5.5f);

            ReflectionProbe probe = GetOrCreateReflectionProbe(root.transform);
            ConfigureReflectionProbe(probe);

            Volume volume = GetOrCreateVolume(root.transform);
            ConfigureVolume(volume);

            Camera camera = Camera.main != null ? Camera.main : Object.FindAnyObjectByType<Camera>();
            if (camera != null)
            {
                Undo.RecordObject(camera, "Configure Greenward camera presentation");
                camera.allowHDR = true;
                camera.clearFlags = CameraClearFlags.Skybox;
                EditorUtility.SetDirty(camera);
            }

            EditorSceneManager.MarkSceneDirty(scene);
            AssetDatabase.SaveAssets();
            EditorSceneManager.SaveScene(scene);
            Undo.CollapseUndoOperations(undoGroup);

            Selection.activeGameObject = root;
            Debug.Log(
                "Greenward presentation baseline authored into the scene. " +
                "Tune the Sun, Greenward Global Volume, Reflection Probe, fog and sky directly in the Inspector from now on.");
        }

        [MenuItem("TD Annihilation/Greenward/Presentation/Select Presentation Rig")]
        public static void SelectPresentationRig()
        {
            GameObject root = GameObject.Find(PresentationRootName);
            if (root == null)
            {
                Debug.LogWarning("Greenward Presentation does not exist yet. Run Apply Suggested Baseline first.");
                return;
            }

            Selection.activeGameObject = root;
            EditorGUIUtility.PingObject(root);
        }

        private static GameObject GetOrCreateRoot(string name)
        {
            GameObject root = GameObject.Find(name);
            if (root != null) return root;

            root = new GameObject(name);
            Undo.RegisterCreatedObjectUndo(root, "Create " + name);
            return root;
        }

        private static Light GetOrCreateSun(Transform parent)
        {
            Light sun = RenderSettings.sun;
            if (sun == null || sun.type != LightType.Directional)
            {
                Light[] lights = Object.FindObjectsByType<Light>(FindObjectsSortMode.None);
                for (int i = 0; i < lights.Length; i++)
                {
                    if (lights[i] != null && lights[i].type == LightType.Directional)
                    {
                        sun = lights[i];
                        break;
                    }
                }
            }

            if (sun == null)
            {
                GameObject go = new GameObject("Greenward Sun");
                Undo.RegisterCreatedObjectUndo(go, "Create Greenward Sun");
                sun = go.AddComponent<Light>();
            }

            Undo.RecordObject(sun.gameObject, "Configure Greenward Sun");
            sun.gameObject.name = "Greenward Sun";
            sun.transform.SetParent(parent, true);
            return sun;
        }

        private static void ConfigureSun(Light sun)
        {
            Undo.RecordObject(sun, "Configure Greenward Sun");
            Undo.RecordObject(sun.transform, "Rotate Greenward Sun");

            sun.type = LightType.Directional;
            sun.transform.rotation = Quaternion.Euler(43f, -31f, 0f);
            sun.color = new Color(1f, 0.83f, 0.65f);
            sun.intensity = 1.18f;
            sun.shadows = LightShadows.Soft;
            sun.shadowStrength = 0.90f;
            sun.shadowBias = 0.055f;
            sun.shadowNormalBias = 0.25f;

            RenderSettings.sun = sun;
            EditorUtility.SetDirty(sun);
            EditorUtility.SetDirty(sun.transform);
        }

        private static void ConfigureEnvironment(Light sun)
        {
            Material sky = GetOrCreateSkyMaterial();
            if (sky != null)
            {
                if (sky.HasProperty("_SkyTint"))
                    sky.SetColor("_SkyTint", new Color(0.16f, 0.23f, 0.36f));
                if (sky.HasProperty("_GroundColor"))
                    sky.SetColor("_GroundColor", new Color(0.055f, 0.05f, 0.045f));
                if (sky.HasProperty("_AtmosphereThickness"))
                    sky.SetFloat("_AtmosphereThickness", 0.78f);
                if (sky.HasProperty("_Exposure"))
                    sky.SetFloat("_Exposure", 0.76f);
                if (sky.HasProperty("_SunSize"))
                    sky.SetFloat("_SunSize", 0.035f);
                if (sky.HasProperty("_SunSizeConvergence"))
                    sky.SetFloat("_SunSizeConvergence", 5f);
                EditorUtility.SetDirty(sky);
                RenderSettings.skybox = sky;
            }

            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.27f, 0.36f, 0.46f);
            RenderSettings.ambientEquatorColor = new Color(0.20f, 0.26f, 0.22f);
            RenderSettings.ambientGroundColor = new Color(0.085f, 0.075f, 0.065f);
            RenderSettings.ambientIntensity = 0.48f;
            RenderSettings.reflectionIntensity = 0.62f;

            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = new Color(0.20f, 0.27f, 0.32f);
            RenderSettings.fogStartDistance = 42f;
            RenderSettings.fogEndDistance = 96f;

            DynamicGI.UpdateEnvironment();
        }

        private static Material GetOrCreateSkyMaterial()
        {
            EnsureFolder(SkyMaterialFolder);

            Material sky = AssetDatabase.LoadAssetAtPath<Material>(SkyMaterialPath);
            if (sky != null) return sky;

            Shader shader = Shader.Find("Skybox/Procedural");
            if (shader == null)
            {
                Debug.LogWarning("Skybox/Procedural shader was not found. Sky material was not created.");
                return null;
            }

            sky = new Material(shader) { name = "Greenward Sky" };
            AssetDatabase.CreateAsset(sky, SkyMaterialPath);
            return sky;
        }

        private static void EnsureLocalLight(
            Transform parent,
            string name,
            Vector3 position,
            Color color,
            float intensity,
            float range)
        {
            Transform existing = parent.Find(name);
            GameObject go;
            if (existing == null)
            {
                go = new GameObject(name);
                Undo.RegisterCreatedObjectUndo(go, "Create " + name);
                go.transform.SetParent(parent);
            }
            else
            {
                go = existing.gameObject;
            }

            Undo.RecordObject(go.transform, "Position " + name);
            go.transform.position = position;

            Light light = go.GetComponent<Light>();
            if (light == null) light = Undo.AddComponent<Light>(go);
            Undo.RecordObject(light, "Configure " + name);

            light.type = LightType.Point;
            light.color = color;
            light.intensity = intensity;
            light.range = range;
            light.shadows = LightShadows.None;

            EditorUtility.SetDirty(light);
            EditorUtility.SetDirty(go.transform);
        }

        private static ReflectionProbe GetOrCreateReflectionProbe(Transform parent)
        {
            GameObject go = GameObject.Find(ProbeObjectName);
            if (go == null)
            {
                go = new GameObject(ProbeObjectName);
                Undo.RegisterCreatedObjectUndo(go, "Create Greenward Reflection Probe");
            }

            go.transform.SetParent(parent, true);

            ReflectionProbe probe = go.GetComponent<ReflectionProbe>();
            if (probe == null) probe = Undo.AddComponent<ReflectionProbe>(go);
            return probe;
        }

        private static void ConfigureReflectionProbe(ReflectionProbe probe)
        {
            Undo.RecordObject(probe.transform, "Position Greenward Reflection Probe");
            Undo.RecordObject(probe, "Configure Greenward Reflection Probe");

            probe.transform.position = new Vector3(4f, 7f, 2f);
            probe.mode = ReflectionProbeMode.Realtime;
            probe.refreshMode = ReflectionProbeRefreshMode.OnAwake;
            probe.timeSlicingMode = ReflectionProbeTimeSlicingMode.IndividualFaces;
            probe.size = new Vector3(82f, 24f, 52f);
            probe.intensity = 0.55f;
            probe.boxProjection = true;

            EditorUtility.SetDirty(probe);
            EditorUtility.SetDirty(probe.transform);
        }

        private static Volume GetOrCreateVolume(Transform parent)
        {
            GameObject go = GameObject.Find(VolumeObjectName);
            if (go == null)
            {
                go = new GameObject(VolumeObjectName);
                Undo.RegisterCreatedObjectUndo(go, "Create Greenward Global Volume");
            }

            go.transform.SetParent(parent, true);

            Volume volume = go.GetComponent<Volume>();
            if (volume == null) volume = Undo.AddComponent<Volume>(go);
            Undo.RecordObject(volume, "Configure Greenward Global Volume");
            volume.isGlobal = true;
            volume.priority = 50f;
            volume.weight = 1f;
            return volume;
        }

        private static void ConfigureVolume(Volume volume)
        {
            VolumeProfile profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(VolumeProfilePath);
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<VolumeProfile>();
                profile.name = "GreenwardVolumeProfile";
                AssetDatabase.CreateAsset(profile, VolumeProfilePath);
            }

            Undo.RecordObject(volume, "Assign Greenward Volume Profile");
            volume.sharedProfile = profile;
            EditorUtility.SetDirty(volume);

            Tonemapping tonemapping = GetOrAdd<Tonemapping>(profile);
            tonemapping.active = true;
            tonemapping.mode.Override(TonemappingMode.ACES);

            ColorAdjustments color = GetOrAdd<ColorAdjustments>(profile);
            color.active = true;
            color.postExposure.Override(0.05f);
            color.contrast.Override(11f);
            color.saturation.Override(3f);
            color.colorFilter.Override(new Color(1f, 0.985f, 0.96f, 1f));

            WhiteBalance balance = GetOrAdd<WhiteBalance>(profile);
            balance.active = true;
            balance.temperature.Override(-3f);
            balance.tint.Override(1f);

            Bloom bloom = GetOrAdd<Bloom>(profile);
            bloom.active = true;
            bloom.threshold.Override(1.05f);
            bloom.intensity.Override(0.20f);
            bloom.scatter.Override(0.56f);
            bloom.highQualityFiltering.Override(true);

            Vignette vignette = GetOrAdd<Vignette>(profile);
            vignette.active = true;
            vignette.color.Override(Color.black);
            vignette.center.Override(new Vector2(0.5f, 0.5f));
            vignette.intensity.Override(0.12f);
            vignette.smoothness.Override(0.34f);
            vignette.rounded.Override(false);

            FilmGrain grain = GetOrAdd<FilmGrain>(profile);
            grain.active = false;
            grain.intensity.Override(0f);

            DepthOfField dof = GetOrAdd<DepthOfField>(profile);
            dof.active = false;

            EditorUtility.SetDirty(profile);
        }

        private static T GetOrAdd<T>(VolumeProfile profile) where T : VolumeComponent
        {
            if (profile.TryGet(out T component)) return component;
            return profile.Add<T>(true);
        }

        private static void EnsureFolder(string folderPath)
        {
            if (AssetDatabase.IsValidFolder(folderPath)) return;

            string[] parts = folderPath.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }
    }
}
#endif

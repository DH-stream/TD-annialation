#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TDAnnihilation
{
    public static class GreenwardSceneAuthoring
    {
        private const string WorldRootName = "Greenward Static Environment";
        private const string ReferencesRootName = "Greenward Gameplay References";

        [MenuItem("TD Annihilation/Greenward/Bake Static Scene")]
        public static void BakeStaticScene()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid())
            {
                Debug.LogError("Open SampleScene before baking Greenward static content.");
                return;
            }

            TDVerticalSliceBootstrap bootstrap = Object.FindFirstObjectByType<TDVerticalSliceBootstrap>();
            if (bootstrap == null)
            {
                Debug.LogError("SampleScene needs a TDVerticalSliceBootstrap before baking Greenward static content.");
                return;
            }

            GameObject oldWorld = GameObject.Find(WorldRootName);
            if (oldWorld != null) Object.DestroyImmediate(oldWorld);
            GameObject oldReferences = GameObject.Find(ReferencesRootName);
            if (oldReferences != null) Object.DestroyImmediate(oldReferences);

            EnsureAnimationControllers();
            var route = GreenwardWorldLayout.CreateRoute();
            GameObject world = GreenwardWorldBuilder.Build(route);
            world.name = WorldRootName;
            foreach (Collider collider in world.GetComponentsInChildren<Collider>(true))
                if (collider.gameObject.name == "Roadside Gravel") Object.DestroyImmediate(collider);

            GameObject references = new GameObject(ReferencesRootName);
            Transform pathRoot = new GameObject("Authored Path Waypoints").transform;
            pathRoot.SetParent(references.transform);
            Transform[] waypoints = new Transform[route.Count];
            for (int i = 0; i < route.Count; i++)
            {
                GameObject waypoint = new GameObject($"Path Waypoint {i:00}");
                waypoint.transform.SetParent(pathRoot);
                waypoint.transform.position = route[i];
                waypoints[i] = waypoint.transform;
            }

            Transform heroSpawn = CreateMarker(references.transform, "Hero Spawn Point", new Vector3(4f, GreenwardWorldLayout.HeightAt(4f, 4f) + 0.25f, 4f));
            Transform enemySpawn = CreateMarker(references.transform, "Enemy Spawn Point", route[0]);
            Transform castleTarget = CreateMarker(references.transform, "Castle Target", route[route.Count - 1]);

            GreenwardLightingBuilder.Configure(Camera.main);
            GreenwardBoundaryMist.CreateForEditor(world.transform);
            SerializedObject serializedBootstrap = new SerializedObject(bootstrap);
            serializedBootstrap.FindProperty("authoredWorldRoot").objectReferenceValue = world.transform;
            serializedBootstrap.FindProperty("heroSpawnPoint").objectReferenceValue = heroSpawn;
            serializedBootstrap.FindProperty("enemySpawnPoint").objectReferenceValue = enemySpawn;
            serializedBootstrap.FindProperty("castleTarget").objectReferenceValue = castleTarget;
            SerializedProperty pathProperty = serializedBootstrap.FindProperty("pathWaypoints");
            pathProperty.arraySize = waypoints.Length;
            for (int i = 0; i < waypoints.Length; i++)
                pathProperty.GetArrayElementAtIndex(i).objectReferenceValue = waypoints[i];
            SerializedProperty buildArea = serializedBootstrap.FindProperty("buildArea");
            buildArea.FindPropertyRelative("m_Center").vector3Value = new Vector3(0f, 0f, 0f);
            buildArea.FindPropertyRelative("m_Extent").vector3Value = new Vector3(45f, 8f, 29f);
            serializedBootstrap.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Selection.activeGameObject = world;
            Debug.Log($"Baked Greenward static environment with {route.Count} authored path waypoints into {scene.path}.");
        }

        [MenuItem("TD Annihilation/Greenward/Bake Static Scene", true)]
        private static bool ValidateBakeStaticScene()
        {
            return SceneManager.GetActiveScene().IsValid();
        }

        private static Transform CreateMarker(Transform parent, string name, Vector3 position)
        {
            GameObject marker = new GameObject(name);
            marker.transform.SetParent(parent);
            marker.transform.position = position;
            return marker.transform;
        }

        [MenuItem("TD Annihilation/Greenward/Build Animation Controllers")]
        public static void EnsureAnimationControllers()
        {
            BuildController("Assets/Resources/TDAnnihilation/WarriorRpg.controller", false);
            BuildController("Assets/Resources/TDAnnihilation/VillagerRpg.controller", true);
            AssetDatabase.SaveAssets();
        }

        private static void BuildController(string assetPath, bool villager)
        {
            AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(assetPath);
            if (controller == null)
            {
                controller = AnimatorController.CreateAnimatorControllerAtPath(assetPath);
            }
            AddParameter(controller, "Speed", AnimatorControllerParameterType.Float);
            AddParameter(controller, "Grounded", AnimatorControllerParameterType.Bool);
            AddParameter(controller, "Jump", AnimatorControllerParameterType.Trigger);
            AddParameter(controller, "Attack", AnimatorControllerParameterType.Trigger);
            if (villager)
            {
                AddParameter(controller, "Scared", AnimatorControllerParameterType.Bool);
                AddParameter(controller, "Working", AnimatorControllerParameterType.Bool);
            }
            AnimatorStateMachine machine = controller.layers[0].stateMachine;
            foreach (ChildAnimatorState child in machine.states) machine.RemoveState(child.state);
            foreach (AnimatorStateTransition transition in machine.anyStateTransitions)
                machine.RemoveAnyStateTransition(transition);
            AnimationClip idle = LoadBlinkClip("Movement/Idle.fbx", "Idle");
            AnimationClip run = LoadBlinkClip("Movement/RunForward.fbx", "RunForward");
            AnimationClip sprint = LoadBlinkClip("Movement/Sprint.fbx", "Sprint");
            AnimationClip jump = LoadBlinkClip("Movement/Jumps.fbx", "Jumps");
            AnimationClip attack = LoadBlinkClip("Combat/MeleeAttack_OneHanded.fbx", "MeleeAttack_OneHanded");
            AnimationClip gather = LoadBlinkClip("Gathering/Gathering.fbx", "Gathering");
            AnimatorState idleState = machine.AddState("Idle");
            idleState.motion = idle;
            AnimatorState walkState = machine.AddState("Walk");
            walkState.motion = run;
            AnimatorState sprintState = machine.AddState("Sprint");
            sprintState.motion = sprint;
            AnimatorState jumpState = machine.AddState("Jump");
            jumpState.motion = jump;
            AnimatorState attackState = machine.AddState("Attack");
            attackState.motion = attack;
            machine.defaultState = idleState;
            AddFloatTransition(idleState, walkState, "Speed", AnimatorConditionMode.Greater, 0.1f);
            AddFloatTransition(walkState, idleState, "Speed", AnimatorConditionMode.Less, 0.1f);
            AddFloatTransition(walkState, sprintState, "Speed", AnimatorConditionMode.Greater, 1.5f);
            AddFloatTransition(sprintState, walkState, "Speed", AnimatorConditionMode.Less, 1.5f);
            AddTriggerTransition(machine, jumpState, "Jump");
            AddTriggerTransition(machine, attackState, "Attack");
            AddExitTransition(jumpState, idleState, 0.88f);
            AddExitTransition(attackState, idleState, 0.82f);
            if (villager)
            {
                AnimatorState workState = machine.AddState("Work");
                workState.motion = gather;
                AnimatorState fleeState = machine.AddState("Flee");
                fleeState.motion = run;
                AddBoolTransition(idleState, workState, "Working", true);
                AddBoolTransition(workState, idleState, "Working", false);
                AddBoolTransition(machine, fleeState, "Scared", true);
                AddBoolTransition(fleeState, walkState, "Scared", false);
            }
            EditorUtility.SetDirty(controller);
        }

        private static void AddParameter(AnimatorController controller, string name, AnimatorControllerParameterType type)
        {
            foreach (AnimatorControllerParameter parameter in controller.parameters)
                if (parameter.name == name) return;
            controller.AddParameter(name, type);
        }

        private static AnimationClip LoadBlinkClip(string suffix, string clipName)
        {
            string[] guids = AssetDatabase.FindAssets("t:Model " + clipName);
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (!path.Replace('\\', '/').Contains("Animations_Starter_Pack/" + suffix)) continue;
                AnimationClip fallback = null;
                foreach (Object asset in AssetDatabase.LoadAllAssetsAtPath(path))
                {
                    if (!(asset is AnimationClip clip) || clip.name.StartsWith("__preview__")) continue;
                    fallback = fallback ?? clip;
                    if (clip.name == clipName || clip.name.ToLowerInvariant().Contains(clipName.ToLowerInvariant().Replace("jumps", "jump"))) return clip;
                }
                if (fallback != null) return fallback;
            }
            Debug.LogWarning("Blink animation clip not found: " + clipName);
            return null;
        }

        private static void AddFloatTransition(AnimatorState from, AnimatorState to, string parameter, AnimatorConditionMode mode, float threshold)
        {
            AnimatorStateTransition transition = from.AddTransition(to);
            transition.hasExitTime = false;
            transition.duration = 0.12f;
            transition.AddCondition(mode, threshold, parameter);
        }

        private static void AddBoolTransition(AnimatorState from, AnimatorState to, string parameter, bool value)
        {
            AnimatorStateTransition transition = from.AddTransition(to);
            transition.hasExitTime = false;
            transition.duration = 0.12f;
            transition.AddCondition(value ? AnimatorConditionMode.If : AnimatorConditionMode.IfNot, 0f, parameter);
        }

        private static void AddBoolTransition(AnimatorStateMachine machine, AnimatorState to, string parameter, bool value)
        {
            AnimatorStateTransition transition = machine.AddAnyStateTransition(to);
            transition.hasExitTime = false;
            transition.duration = 0.12f;
            transition.AddCondition(value ? AnimatorConditionMode.If : AnimatorConditionMode.IfNot, 0f, parameter);
        }

        private static void AddTriggerTransition(AnimatorStateMachine machine, AnimatorState to, string parameter)
        {
            AnimatorStateTransition transition = machine.AddAnyStateTransition(to);
            transition.hasExitTime = false;
            transition.duration = 0.08f;
            transition.AddCondition(AnimatorConditionMode.If, 0f, parameter);
        }

        private static void AddExitTransition(AnimatorState from, AnimatorState to, float exitTime)
        {
            AnimatorStateTransition transition = from.AddTransition(to);
            transition.hasExitTime = true;
            transition.exitTime = exitTime;
            transition.duration = 0.12f;
        }
    }
}
#endif

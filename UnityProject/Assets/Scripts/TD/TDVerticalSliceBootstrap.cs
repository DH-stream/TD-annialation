using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TDAnnihilation
{
    public sealed class TDVerticalSliceBootstrap : MonoBehaviour
    {
        [Header("Free Quaternius art")]
        [SerializeField] private GameObject warriorPrefab;
        [SerializeField] private GameObject demonPrefab;
        [SerializeField] private GameObject arcaneTowerPrefab;
        [SerializeField] private GameObject archerTowerPrefab;

        [Header("Slice tuning")]
        [SerializeField] private int startingGold = 120;
        [SerializeField] private int startingLives = 20;
        [SerializeField] private int stageWaveCount = 3;
        [SerializeField] private int firstClearSkulls = 10;
        [SerializeField] private int repeatClearSkulls = 3;
        [SerializeField] private int endlessBaseSkulls = 1;
        [SerializeField] private int endlessSkullsPerWave = 1;
        [SerializeField] private string mapId = "greenward";

        [Header("Authored scene references")]
        [SerializeField] private Transform authoredWorldRoot;
        [SerializeField] private Transform heroSpawnPoint;
        [SerializeField] private Transform enemySpawnPoint;
        [SerializeField] private Transform castleTarget;
        [SerializeField] private Transform[] pathWaypoints;
        [SerializeField] private Bounds buildArea = new Bounds(Vector3.zero, new Vector3(90f, 8f, 58f));

        private readonly List<TDEnemyController> enemies = new List<TDEnemyController>();
        private readonly List<Vector3> path = new List<Vector3>();
        private TDResourceState state;
        private TDTowerController tower;
        private TDGameFlow flow;
        private TDHeroController heroController;
        private bool ready;
        private TDSkillTreeState progression;
        private int runSkullsEarned;
        private readonly List<Transform> placedTowers = new List<Transform>();
        private GameObject placementGhost;
        private Material placementMaterial;
        private Collider placementGroundCollider;
        private bool placementMode;
        private bool placementValid;
        private bool placementArcher;
        private float previewOverrideTimer;
        private const int BaseTowerCost = 40;
        private TDSkillModifiers activeModifiers = new TDSkillModifiers();
        private const string SavedRunKey = "TDAnnihilation.Greenward.BuildCheckpoint.v1";
        [System.Serializable] private sealed class TowerCheckpoint { public Vector3 position; public bool archer; }
        [System.Serializable] private sealed class RunCheckpoint
        {
            public string map;
            public int wave;
            public int gold;
            public int lives;
            public int defeated;
            public int skullsEarned;
            public TDRunMode mode;
            public Vector3 heroPosition;
            public List<TowerCheckpoint> towers = new List<TowerCheckpoint>();
        }
        private int CurrentTowerCost => Mathf.Max(1, BaseTowerCost - activeModifiers.TowerCostReduction);

        private void Awake()
        {
            if (ready) return;
            Application.runInBackground = true;
            ready = true;
            state = gameObject.AddComponent<TDResourceState>();
            flow = new TDGameFlow(stageWaveCount);
            progression = TDSkillTreeState.Load();
            state.gold = startingGold;
            state.lives = startingLives;
            LoadArtIfNeeded();
            LoadAuthoredArena();
            Transform hero = SpawnHero();
            heroController = hero.GetComponent<TDHeroController>();
            SpawnTower(new Vector3(-5f, GreenwardWorldLayout.HeightAt(-5f, -2f) + 0.3f, -2f));
            SpawnTower(new Vector3(22f, GreenwardWorldLayout.HeightAt(22f, 3f) + 0.3f, 3f), useArcher: true);
            Camera camera = Camera.main;
            if (camera != null)
            {
                TDStrategicCamera strategic = camera.GetComponent<TDStrategicCamera>();
                if (strategic == null) strategic = camera.gameObject.AddComponent<TDStrategicCamera>();
                strategic.SetMenuView();
                camera.fieldOfView = 62f;
            }
            // Lighting and atmosphere are authored in the Unity scene. Do not overwrite artist-tuned values at runtime.
            if (GetComponent<TDVerticalSliceHUD>() == null) gameObject.AddComponent<TDVerticalSliceHUD>();
        }

        private void Update()
        {
            enemies.RemoveAll(enemy => enemy == null);
            if (state == null || flow == null) return;
            if (Time.timeScale == 0f) return;
            HandlePlacementInput();
            if (!placementMode && flow.Phase == TDGamePhase.Wave && Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
                heroController?.Attack();
            if (state.lives <= 0)
            {
                flow.Lose();
                PlayerPrefs.DeleteKey(SavedRunKey);
                return;
            }
            if (flow.Phase == TDGamePhase.Wave && enemies.Count == 0 && flow.CompleteWave())
            {
                if (flow.RunMode == TDRunMode.Endless)
                    runSkullsEarned += progression.AwardEndlessWave(flow.Wave, endlessBaseSkulls, endlessSkullsPerWave);
                else if (flow.Phase == TDGamePhase.Victory)
                    runSkullsEarned += progression.AwardMapClear(mapId, firstClearSkulls, repeatClearSkulls);
                if (flow.Phase == TDGamePhase.Build) SaveBuildCheckpoint();
                else PlayerPrefs.DeleteKey(SavedRunKey);
            }
        }

        private void LoadArtIfNeeded()
        {
            if (warriorPrefab == null) warriorPrefab = Resources.Load<GameObject>("TDAnnihilation/Warrior");
            if (demonPrefab == null) demonPrefab = Resources.Load<GameObject>("TDAnnihilation/Demon");
            if (arcaneTowerPrefab == null) arcaneTowerPrefab = Resources.Load<GameObject>("TDAnnihilation/ArcaneTower");
            if (archerTowerPrefab == null) archerTowerPrefab = Resources.Load<GameObject>("TDAnnihilation/ArcherTower");
        }

        private void LoadAuthoredArena()
        {
            path.Clear();
            if (authoredWorldRoot == null)
                Debug.LogError("Greenward authored scene is missing its static environment reference. Use TD Annihilation/Greenward/Bake Static Scene.");
            if (pathWaypoints != null)
                for (int i = 0; i < pathWaypoints.Length; i++)
                    if (pathWaypoints[i] != null) path.Add(pathWaypoints[i].position);
            if (castleTarget != null && path.Count > 0) path[path.Count - 1] = castleTarget.position;
            if (path.Count < 2)
            {
                Debug.LogError("Greenward authored scene is missing path waypoints. Use TD Annihilation/Greenward/Bake Static Scene.");
            }
            EnsurePlacementGroundCollider();
        }

        private void EnsurePlacementGroundCollider()
        {
            if (authoredWorldRoot == null) return;
            MeshFilter[] meshes = authoredWorldRoot.GetComponentsInChildren<MeshFilter>(true);
            for (int i = 0; i < meshes.Length; i++)
            {
                if (meshes[i] == null || meshes[i].sharedMesh == null || meshes[i].gameObject.name != "Sculpted Meadow Terrain") continue;
                MeshCollider collider = meshes[i].GetComponent<MeshCollider>();
                if (collider == null) collider = meshes[i].gameObject.AddComponent<MeshCollider>();
                collider.sharedMesh = meshes[i].sharedMesh;
                placementGroundCollider = collider;
                return;
            }
        }

        private Transform SpawnHero()
        {
            // Check if a hero already exists in the scene (placed by editor)
            TDHeroController existingHero = FindAnyObjectByType<TDHeroController>();
            if (existingHero != null)
            {
                RestoreHeroAppearance(existingHero.gameObject);
                Debug.Log("Found existing hero in scene: " + existingHero.gameObject.name);
                return existingHero.transform;
            }

            Vector3 heroPosition = heroSpawnPoint != null
                ? heroSpawnPoint.position
                : new Vector3(4f, GreenwardWorldLayout.HeightAt(4f, 4f) + 0.25f, 4f);
            
            GameObject hero = null;
            GameObject authoredHeroPrefab = Resources.Load<GameObject>("TDAnnihilation/GreenwardHero");
            if (authoredHeroPrefab != null)
                hero = Instantiate(authoredHeroPrefab, heroPosition, Quaternion.Euler(0f, 25f, 0f));
            
            // Try to load the evenlowerpoly model first
            #if UNITY_EDITOR
            GameObject evenLowerPolyPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Main Char/evenlowerpoly.fbx");
            if (hero == null && evenLowerPolyPrefab != null)
            {
                hero = Instantiate(evenLowerPolyPrefab, heroPosition, Quaternion.Euler(0f, 25f, 0f));
                Debug.Log("Spawned evenlowerpoly model as hero");
            }
            #endif
            
            // Fall back to warrior prefab if available
            if (hero == null && warriorPrefab != null)
            {
                hero = Instantiate(warriorPrefab, heroPosition, Quaternion.Euler(0f, 25f, 0f));
                Debug.Log("Spawned warrior prefab as hero");
            }
            
            // Create fallback if neither is available
            if (hero == null)
            {
                hero = MakePrimitive("Hero fallback", PrimitiveType.Capsule, heroPosition + Vector3.up, Vector3.one, new Color(0.12f, 0.28f, 0.42f));
                Debug.LogWarning("Using capsule fallback for hero - no model found");
            }
            
            hero.name = "Hero - Warden of Greenward";
            hero.transform.localScale = Vector3.one * TDPresentationScale.Hero;
            RestoreHeroAppearance(hero);
            
            // Ensure hero has an Animator component (evenlowerpoly model may not have one)
            Animator animator = hero.GetComponent<Animator>();
            if (animator == null)
            {
                animator = hero.GetComponentInChildren<Animator>();
                if (animator == null)
                {
                    animator = hero.AddComponent<Animator>();
                    Debug.Log("Added Animator component to hero root");
                }
            }
            
            // Now add TDHeroController - it will initialize the animator in its Awake
            if (hero.GetComponent<TDHeroController>() == null) 
            {
                hero.AddComponent<TDHeroController>();
            }
            
            // Apply color palette while preserving existing materials
            ApplyFantasyPalette(hero, true);
            return hero.transform;
        }

        private static void RestoreHeroAppearance(GameObject hero)
        {
            Material material = Resources.Load<Material>("TDAnnihilation/GreenwardHeroMaterial");
            if (material == null) return;
            foreach (SkinnedMeshRenderer renderer in hero.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                renderer.sharedMaterial = material;
        }

        private void SpawnTower(Vector3 position, bool animateArrival = false, bool useArcher = false)
        {
            GameObject selectedPrefab = useArcher && archerTowerPrefab != null ? archerTowerPrefab : arcaneTowerPrefab;
            GameObject towerRoot = new GameObject(useArcher ? "Archer Watchtower" : "Arcane Watchtower");
            towerRoot.transform.position = position;
            if (selectedPrefab != null)
            {
                GameObject visualRoot = Instantiate(selectedPrefab, towerRoot.transform);
                visualRoot.name = selectedPrefab.name;
                visualRoot.transform.localPosition = Vector3.zero;
                visualRoot.transform.localRotation = selectedPrefab.transform.localRotation;
                visualRoot.transform.localScale = Vector3.one * 3f;
            }
            else
            {
                MakePrimitive("Tower Base", PrimitiveType.Cylinder, towerRoot.transform.position + Vector3.up * 0.8f, new Vector3(1.5f, 1.6f, 1.5f), new Color(0.32f, 0.29f, 0.36f), towerRoot.transform);
                MakePrimitive("Tower Rune", PrimitiveType.Sphere, towerRoot.transform.position + Vector3.up * 2.45f, Vector3.one * 0.75f, new Color(0.38f, 0.25f, 0.96f), towerRoot.transform);
            }
            tower = towerRoot.GetComponent<TDTowerController>();
            if (tower == null) tower = towerRoot.AddComponent<TDTowerController>();
            tower.state = state;
            tower.projectileColor = useArcher ? new Color(0.66f, 0.46f, 0.24f) : new Color(0.58f, 0.35f, 1f);
            tower.ApplySkillModifiers(activeModifiers, useArcher);
            placedTowers.Add(towerRoot.transform);
            if (animateArrival) towerRoot.AddComponent<TDTowerPlacementAnimation>().Play();
        }

        private void HandlePlacementInput()
        {
            Keyboard keyboard = Keyboard.current;
            Mouse mouse = Mouse.current;
            if (keyboard != null && keyboard.bKey.wasPressedThisFrame) TogglePlacementMode();
            if (!placementMode) return;
            if ((keyboard != null && keyboard.escapeKey.wasPressedThisFrame) || (mouse != null && mouse.rightButton.wasPressedThisFrame))
            {
                ExitPlacementMode();
                return;
            }
            if (previewOverrideTimer > 0f) previewOverrideTimer -= Time.deltaTime;
            else UpdatePlacementGhost(mouse);
            if (mouse != null && mouse.leftButton.wasPressedThisFrame && placementValid)
                TryPlaceTowerAt(placementGhost.transform.position);
        }

        public void TogglePlacementMode()
        {
            if (placementMode) ExitPlacementMode();
            else BeginPlacementMode();
        }

        public void BeginPlacementMode(bool useArcher = false)
        {
            if (flow == null || flow.Phase != TDGamePhase.Build || state.gold < CurrentTowerCost) return;
            if (useArcher && archerTowerPrefab == null) return;
            if (placementGhost != null && placementArcher != useArcher)
            {
                Destroy(placementGhost);
                placementGhost = null;
                if (placementMaterial != null) Destroy(placementMaterial);
            }
            placementArcher = useArcher;
            placementMode = true;
            EnsurePlacementGhost();
            placementGhost.SetActive(false);
        }

        public void ExitPlacementMode()
        {
            placementMode = false;
            placementValid = false;
            if (placementGhost != null) placementGhost.SetActive(false);
        }

        public void PreviewPlacementAt(Vector3 worldPosition)
        {
            if (!placementMode) BeginPlacementMode();
            if (!placementMode) return;
            EnsurePlacementGhost();
            previewOverrideTimer = 0.35f;
            SetPlacementGhost(worldPosition);
        }

        public bool TryPlaceTowerAt(Vector3 worldPosition)
        {
            if (!placementMode) BeginPlacementMode();
            if (!placementMode) return false;
            SetPlacementGhost(worldPosition);
            if (!placementValid || state.gold < CurrentTowerCost) return false;
            state.gold -= CurrentTowerCost;
            SpawnTower(placementGhost.transform.position, true, placementArcher);
            ExitPlacementMode();
            SaveBuildCheckpoint();
            return true;
        }

        private void UpdatePlacementGhost(Mouse mouse)
        {
            if (mouse == null || Camera.main == null)
            {
                if (placementGhost != null) placementGhost.SetActive(false);
                placementValid = false;
                return;
            }
            Ray ray = Camera.main.ScreenPointToRay(mouse.position.ReadValue());
            if (placementGroundCollider != null && placementGroundCollider.Raycast(ray, out RaycastHit hit, 200f))
            {
                SetPlacementGhost(hit.point);
                return;
            }
            Plane fallbackPlane = new Plane(Vector3.up, Vector3.zero);
            if (fallbackPlane.Raycast(ray, out float distance)) SetPlacementGhost(ray.GetPoint(distance));
            else
            {
                placementGhost.SetActive(false);
                placementValid = false;
            }
        }

        private void SetPlacementGhost(Vector3 worldPosition)
        {
            EnsurePlacementGhost();
            Vector3 position = new Vector3(worldPosition.x, GreenwardWorldLayout.HeightAt(worldPosition.x, worldPosition.z), worldPosition.z);
            if (!buildArea.Contains(position))
            {
                placementGhost.SetActive(false);
                placementValid = false;
                return;
            }
            placementGhost.transform.position = position;
            placementValid = CanPlaceTower(position);
            placementMaterial.color = placementValid ? new Color(0.18f, 1f, 0.35f, 0.48f) : new Color(1f, 0.16f, 0.12f, 0.48f);
            placementMaterial.SetColor("_BaseColor", placementMaterial.color);
            placementGhost.SetActive(true);
        }

        private bool CanPlaceTower(Vector3 position)
        {
            Vector2 point = new Vector2(position.x, position.z);
            if (!GreenwardWorldLayout.IsBuildableSurface(point, path)) return false;
            foreach (Transform towerRoot in placedTowers)
                if (towerRoot != null && Vector3.Distance(towerRoot.position, position) < 3.5f) return false;
            return true;
        }

        private void EnsurePlacementGhost()
        {
            if (placementGhost != null) return;
            GameObject selectedPrefab = placementArcher ? archerTowerPrefab : arcaneTowerPrefab;
            if (selectedPrefab != null)
            {
                placementGhost = new GameObject((placementArcher ? "Archer" : "Arcane") + " Watchtower Placement Ghost");
                GameObject ghostVisual = Instantiate(selectedPrefab, placementGhost.transform);
                ghostVisual.transform.localPosition = Vector3.zero;
                ghostVisual.transform.localRotation = selectedPrefab.transform.localRotation;
                ghostVisual.transform.localScale = Vector3.one * 3f;
                placementMaterial = CreateGhostMaterial();
                foreach (Renderer renderer in placementGhost.GetComponentsInChildren<Renderer>(true))
                    renderer.sharedMaterial = placementMaterial;
                foreach (Collider collider in placementGhost.GetComponentsInChildren<Collider>(true))
                    Destroy(collider);
                placementGhost.SetActive(false);
                return;
            }
            placementGhost = new GameObject("Arcane Watchtower Placement Ghost");
            GameObject basePart = MakePrimitive("Ghost Base", PrimitiveType.Cylinder, Vector3.up * 0.8f, new Vector3(1.5f, 1.6f, 1.5f), Color.white, placementGhost.transform);
            GameObject runePart = MakePrimitive("Ghost Rune", PrimitiveType.Sphere, Vector3.up * 2.45f, Vector3.one * 0.75f, Color.white, placementGhost.transform);
            placementMaterial = CreateGhostMaterial();
            basePart.GetComponent<Renderer>().sharedMaterial = placementMaterial;
            runePart.GetComponent<Renderer>().sharedMaterial = placementMaterial;
            foreach (Collider collider in placementGhost.GetComponentsInChildren<Collider>()) Destroy(collider);
            placementGhost.SetActive(false);
        }

        private static Material CreateGhostMaterial()
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Unlit/Color");
            Material material = new Material(shader) { name = "Tower Placement Ghost" };
            material.color = new Color(0.18f, 1f, 0.35f, 0.48f);
            material.SetFloat("_Surface", 1f);
            material.SetFloat("_Blend", 0f);
            material.SetFloat("_ZWrite", 0f);
            material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            return material;
        }

        private void SpawnWave(int count)
        {
            if (path.Count < 2)
            {
                Debug.LogError("Cannot spawn a wave because no authored path waypoints are assigned.");
                return;
            }
            Vector3 spawn = enemySpawnPoint != null ? enemySpawnPoint.position : path[0];
            for (int i = 0; i < count; i++)
            {
                int currentWave = flow == null ? 1 : flow.Wave;
                bool spawnElite = currentWave > 1 && currentWave % 4 == 0 && i == count - 1;
                TDEnemyArchetype archetype = spawnElite ? TDEnemyArchetype.Elite : TDEnemyArchetype.Raider;
                GameObject enemyObject = demonPrefab != null
                    ? Instantiate(demonPrefab, spawn + Vector3.up * (0.25f + i * 0.02f) + Vector3.back * i * 0.75f, Quaternion.Euler(0f, 90f, 0f))
                    : MakePrimitive("Demon fallback", PrimitiveType.Capsule, spawn + Vector3.back * i * 0.75f, Vector3.one, new Color(0.48f, 0.09f, 0.09f));
                enemyObject.name = (spawnElite ? "Elite Demon Brute " : "Demon Raider ") + (i + 1);
                enemyObject.transform.localScale = Vector3.one * archetype.Scale;
                EnsureAnimator(enemyObject, "Demon");
                ApplyFantasyPalette(enemyObject, false);
                TDEnemyController enemy = enemyObject.AddComponent<TDEnemyController>();
                enemy.Configure(path.ToArray(), (30f + currentWave * 5f) * archetype.HealthMultiplier, 1.2f + currentWave * 0.05f, state);
                enemies.Add(enemy);
            }
        }

        private static void EnsureAnimator(GameObject root, string controllerName)
        {
            Animator animator = root.GetComponentInChildren<Animator>();
            if (animator == null) animator = root.AddComponent<Animator>();
            string resourceName = controllerName == "Warrior" && Resources.Load<RuntimeAnimatorController>("TDAnnihilation/WarriorRpg") != null
                ? "TDAnnihilation/WarriorRpg"
                : "TDAnnihilation/" + controllerName;
            animator.runtimeAnimatorController = Resources.Load<RuntimeAnimatorController>(resourceName);
            animator.enabled = true;
        }

        private static void ApplyFantasyPalette(GameObject root, bool hero)
        {
            // For hero, preserve existing materials from imported models (e.g., evenlowerpoly)
            if (hero) return;
            
            foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                string part = renderer.name.ToLowerInvariant();
                Color color;
                if (hero)
                {
                    color = part.Contains("face") ? new Color(0.82f, 0.49f, 0.30f) :
                        part.Contains("sword") ? new Color(0.72f, 0.82f, 0.92f) :
                        part.Contains("shoulder") ? new Color(0.86f, 0.58f, 0.16f) :
                        new Color(0.12f, 0.28f, 0.42f);
                }
                else
                {
                    color = part.Contains("trident") ? new Color(0.16f, 0.13f, 0.18f) :
                        new Color(0.48f, 0.08f, 0.08f);
                }
                renderer.material.color = color;
            }
        }

        private static GameObject MakePrimitive(string objectName, PrimitiveType primitive, Vector3 position, Vector3 scale, Color color, Transform parent = null)
        {
            GameObject item = GameObject.CreatePrimitive(primitive);
            item.name = objectName;
            item.transform.position = position;
            item.transform.localScale = scale;
            if (parent != null) item.transform.SetParent(parent, true);
            Renderer renderer = item.GetComponent<Renderer>();
            if (renderer != null)
            {
                Material material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                material.color = color;
                renderer.sharedMaterial = material;
            }
            return item;
        }

        public void RegisterEnemy(TDEnemyController enemy) => enemies.Remove(enemy);
        public IReadOnlyList<Vector3> Path => path;
        public TDResourceState State => state;
        public int StartingLives => startingLives;
        public int MaxLives => startingLives + activeModifiers.StartingLivesBonus;
        public TDSkillTreeState Progression => progression;
        public TDRunMode RunMode => flow == null ? TDRunMode.Normal : flow.RunMode;
        public int RunSkullsEarned => runSkullsEarned;
        public bool IsEndlessUnlocked => progression != null && progression.IsEndlessUnlocked(mapId);
        public int TowerBuildCost => CurrentTowerCost;

        public void StartSoloStages(TDRunMode mode = TDRunMode.Normal)
        {
            if (state == null || flow == null)
            {
                Debug.LogError("StartSoloStages called but state or flow is null. Game not initialized.");
                return;
            }
            if (mode == TDRunMode.Endless && !IsEndlessUnlocked)
            {
                Debug.LogWarning("Endless is locked until this map has been cleared in Normal mode.");
                return;
            }
            activeModifiers = progression.GetModifiers(TDSkillTreeCatalog.CreateDefault());
            heroController?.ApplySkillModifiers(activeModifiers);
            ClearRunActors(true);
            SpawnTower(new Vector3(-5f, GreenwardWorldLayout.HeightAt(-5f, -2f) + 0.3f, -2f));
            SpawnTower(new Vector3(22f, GreenwardWorldLayout.HeightAt(22f, 3f) + 0.3f, 3f), useArcher: true);
            state.gold = startingGold + activeModifiers.StartingGoldBonus;
            state.lives = startingLives + activeModifiers.StartingLivesBonus;
            state.defeated = 0;
            runSkullsEarned = 0;
            flow.SelectSoloStages(mode);
            Camera.main?.GetComponent<TDStrategicCamera>()?.SetTarget(heroController.transform);
            SaveBuildCheckpoint();
        }

        public bool HasSavedRun => ReadBuildCheckpoint() != null;
        public string SavedRunLabel
        {
            get
            {
                RunCheckpoint save = ReadBuildCheckpoint();
                return save == null ? "" : "WAVE " + (save.wave + 1) + " · GREENWARD";
            }
        }

        public bool ContinueSavedRun()
        {
            RunCheckpoint save = ReadBuildCheckpoint();
            if (save == null) return false;
            ClearRunActors(true);
            activeModifiers = progression.GetModifiers(TDSkillTreeCatalog.CreateDefault());
            heroController?.ApplySkillModifiers(activeModifiers);
            foreach (TowerCheckpoint savedTower in save.towers)
                SpawnTower(savedTower.position, useArcher: savedTower.archer);
            if (heroController != null) heroController.transform.position = save.heroPosition;
            state.gold = save.gold;
            state.lives = save.lives;
            state.defeated = save.defeated;
            runSkullsEarned = save.skullsEarned;
            flow.ResumeBuildPhase(save.wave, save.mode);
            Camera.main?.GetComponent<TDStrategicCamera>()?.SetTarget(heroController.transform);
            return true;
        }

        private void ClearRunActors(bool clearTowers)
        {
            ExitPlacementMode();
            foreach (TDEnemyController enemy in enemies)
                if (enemy != null) Destroy(enemy.gameObject);
            enemies.Clear();
            if (!clearTowers) return;
            foreach (Transform placed in placedTowers)
                if (placed != null) Destroy(placed.gameObject);
            placedTowers.Clear();
        }

        private RunCheckpoint ReadBuildCheckpoint()
        {
            string json = PlayerPrefs.GetString(SavedRunKey, "");
            if (string.IsNullOrEmpty(json)) return null;
            try
            {
                RunCheckpoint save = JsonUtility.FromJson<RunCheckpoint>(json);
                return save != null && save.map == mapId && save.wave >= 0 && save.lives > 0 && save.towers != null
                    && (save.mode == TDRunMode.Normal || save.mode == TDRunMode.Endless)
                    && (save.mode != TDRunMode.Normal || save.wave < stageWaveCount)
                    && (save.mode != TDRunMode.Endless || IsEndlessUnlocked) ? save : null;
            }
            catch (System.ArgumentException) { return null; }
        }

        private void SaveBuildCheckpoint()
        {
            if (flow == null || flow.Phase != TDGamePhase.Build || state == null) return;
            var save = new RunCheckpoint
            {
                map = mapId, wave = flow.Wave, mode = flow.RunMode, gold = state.gold,
                lives = state.lives, defeated = state.defeated, skullsEarned = runSkullsEarned,
                heroPosition = heroController == null ? Vector3.zero : heroController.transform.position
            };
            foreach (Transform placed in placedTowers)
            {
                if (placed == null) continue;
                TDTowerController controller = placed.GetComponent<TDTowerController>();
                save.towers.Add(new TowerCheckpoint { position = placed.position, archer = controller != null && controller.IsArcher });
            }
            PlayerPrefs.SetString(SavedRunKey, JsonUtility.ToJson(save));
            PlayerPrefs.Save();
        }

        public void StartNextWave()
        {
            if (flow.Phase != TDGamePhase.Build) return;
            flow.StartWave();
            SpawnWave(4 + flow.Wave);
        }

        public void AttackHero(TDAttackType type = TDAttackType.Light)
        {
            if (flow == null || flow.Phase != TDGamePhase.Wave) return;
            heroController?.TryAttack(type);
        }

        public float AttackCooldownRemaining(TDAttackType type) => heroController == null ? 0f : heroController.AttackCooldownRemaining(type);
        public float AttackCooldownDuration(TDAttackType type) => heroController == null ? 1f : heroController.AttackCooldownDuration(type);

        public void ReturnToMenu()
        {
            ClearRunActors(false);
            flow.ReturnToMenu();
            Camera.main?.GetComponent<TDStrategicCamera>()?.SetMenuView();
        }
        public int CurrentWave => flow == null ? 0 : flow.Wave;
        public TDGamePhase Phase => flow == null ? TDGamePhase.MainMenu : flow.Phase;
        public string CurrentStageName => "GREENWARD";
        public bool IsPlacementMode => placementMode;
        public bool IsPlacingArcher => placementMode && placementArcher;
        public IReadOnlyList<Vector3> Route => path;
        public Bounds PlayableBounds => buildArea;
        public Transform Goal => castleTarget;
        public Transform Hero => heroController == null ? null : heroController.transform;
        public IReadOnlyList<TDEnemyController> ActiveEnemies => enemies;
        public IReadOnlyList<Transform> PlacedTowers => placedTowers;
    }

    public sealed class TDResourceState : MonoBehaviour
    {
        public int gold;
        public int lives;
        public int defeated;
    }

    public sealed class TDEnemyController : MonoBehaviour
    {
        private Vector3[] path;
        private TDResourceState state;
        private float health;
        private float speed;
        private int waypoint;
        private Animator animator;
        private bool hasHitTrigger;

        public void Configure(Vector3[] route, float maxHealth, float moveSpeed, TDResourceState resourceState)
        {
            path = route;
            health = maxHealth;
            speed = moveSpeed;
            state = resourceState;
            animator = GetComponentInChildren<Animator>();
            if (animator != null)
            {
                foreach (AnimatorControllerParameter parameter in animator.parameters)
                    if (parameter.name == "Hit" && parameter.type == AnimatorControllerParameterType.Trigger) hasHitTrigger = true;
                animator.applyRootMotion = false;
                animator.speed = 1f;
                animator.Play("Walk", 0, 0f);
            }
        }

        private void Update()
        {
            if (path == null || waypoint >= path.Length) return;
            Vector3 destination = path[waypoint];
            transform.position = Vector3.MoveTowards(transform.position, destination, speed * Time.deltaTime);
            Vector3 direction = destination - transform.position;
            if (direction.sqrMagnitude > 0.05f) transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction), Time.deltaTime * 8f);
            if (Vector3.Distance(transform.position, destination) < 0.18f)
            {
                waypoint++;
                if (waypoint >= path.Length) ReachGate();
            }
        }

        public void TakeDamage(float amount)
        {
            if (hasHitTrigger)
            {
                animator.ResetTrigger("Hit");
                animator.SetTrigger("Hit");
            }
            health -= amount;
            if (health <= 0f) Defeat();
        }

        public int WaypointIndex => waypoint;
        public float AnimatorNormalizedTime => animator == null ? -1f : animator.GetCurrentAnimatorStateInfo(0).normalizedTime;

        private void ReachGate()
        {
            if (state != null) state.lives--;
            Destroy(gameObject);
        }

        private void Defeat()
        {
            if (state != null)
            {
                state.defeated++;
            }
            SpawnCoin();
            Destroy(gameObject);
        }

        private void SpawnCoin()
        {
            TDCoinPickup.Spawn(state, 12, transform.position + Vector3.up * 0.8f);
        }

        private static Material MakeMaterial(Color color)
        {
            Material material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            material.color = color;
            return material;
        }
    }

    public sealed class TDTowerController : MonoBehaviour
    {
        [HideInInspector] public TDResourceState state;
        [HideInInspector] public Color projectileColor = Color.magenta;
        [SerializeField] private float range = 8f;
        [SerializeField] private float fireRate = 1.1f;
        private float cooldown;
        private TDArcherTowerDrawAnimation archerAnimation;
        private TDSkillModifiers skillModifiers = new TDSkillModifiers();
        private bool isArcher;

        public bool IsArcher => isArcher;
        public float EffectiveRange => range * (1f + (isArcher ? skillModifiers.ArcherRangeBonus : 0f));
        public float EffectiveDamage => 16f * (1f + (isArcher ? skillModifiers.ArcherDamageBonus : skillModifiers.ArcaneDamageBonus));
        public float EffectiveFireInterval => Mathf.Max(0.05f, fireRate * (1f - (isArcher ? 0f : skillModifiers.ArcaneFireIntervalReduction)));

        public void ApplySkillModifiers(TDSkillModifiers modifiers, bool archer)
        {
            skillModifiers = modifiers ?? new TDSkillModifiers();
            isArcher = archer;
        }

        private void Awake()
        {
            archerAnimation = GetComponentInChildren<TDArcherTowerDrawAnimation>();
        }

        private void Update()
        {
            cooldown -= Time.deltaTime;
            TDEnemyController[] targets = FindObjectsByType<TDEnemyController>();
            TDEnemyController nearest = null;
            float best = EffectiveRange * EffectiveRange;
            foreach (TDEnemyController target in targets)
            {
                float distance = (target.transform.position - transform.position).sqrMagnitude;
                if (distance < best) { best = distance; nearest = target; }
            }
            if (nearest == null) return;
            if (archerAnimation != null)
            {
                archerAnimation.AimAt(nearest.transform.position + Vector3.up);
            }
            if (cooldown > 0f) return;
            cooldown = EffectiveFireInterval;
            archerAnimation?.PlayShot();
            bool isArrow = archerAnimation != null;
            GameObject projectile = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            projectile.name = isArrow ? "Archer Bolt" : "Arcane Bolt";
            projectile.transform.position = isArrow ? archerAnimation.ShotPosition : transform.position + Vector3.up * 2.4f;
            if (isArrow) projectile.transform.rotation = archerAnimation.ShotRotation;
            projectile.transform.localScale = isArrow ? Vector3.one * 0.06f : Vector3.one * 0.24f;
            projectile.GetComponent<Renderer>().sharedMaterial = MakeMaterial(projectileColor);
            projectile.AddComponent<TDProjectile>().Configure(nearest, EffectiveDamage, projectileColor, isArrow);
        }

        private static Material MakeMaterial(Color color)
        {
            Material material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            material.color = color;
            return material;
        }
    }

    public sealed class TDProjectile : MonoBehaviour
    {
        private TDEnemyController target;
        private float damage;
        private Vector3 velocity;
        private bool damageApplied;
        private Color projectileColor;

        private const float InitialSpeed = 3.5f;
        private const float MaxSpeed = 15f;
        private const float Acceleration = 24f;
        private const float Steering = 18f;

        public void Configure(TDEnemyController enemy, float hitDamage, Color color, bool arrow = false)
        {
            target = enemy;
            damage = hitDamage;
            projectileColor = color;
            velocity = transform.forward * InitialSpeed;

            if (arrow) CreateArrowVisual();

            TrailRenderer trail = gameObject.AddComponent<TrailRenderer>();
            trail.time = 0.28f;
            trail.startWidth = 0.16f;
            trail.endWidth = 0f;
            trail.minVertexDistance = 0.04f;
            trail.material = MakeParticleMaterial(color);

            GameObject particlesObject = new GameObject("Arcane Bolt Trail");
            particlesObject.transform.SetParent(transform, false);
            ParticleSystem particles = particlesObject.AddComponent<ParticleSystem>();
            var main = particles.main;
            main.loop = true;
            main.startLifetime = 0.35f;
            main.startSpeed = 0.18f;
            main.startSize = 0.08f;
            main.startColor = color;
            main.maxParticles = 40;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.stopAction = ParticleSystemStopAction.Destroy;
            var emission = particles.emission;
            emission.rateOverTime = 30f;
            ParticleSystemRenderer renderer = particlesObject.GetComponent<ParticleSystemRenderer>();
            renderer.material = MakeParticleMaterial(color);
        }

        private void Update()
        {
            if (target == null) { Destroy(gameObject); return; }
            Vector3 targetPosition = target.transform.position + Vector3.up;
            float currentSpeed = Mathf.MoveTowards(velocity.magnitude, MaxSpeed, Acceleration * Time.deltaTime);
            Vector3 desiredVelocity = (targetPosition - transform.position).normalized * currentSpeed;
            velocity = Vector3.MoveTowards(velocity, desiredVelocity, Steering * Time.deltaTime);
            velocity += Vector3.down * 1.8f * Time.deltaTime;
            velocity = Vector3.ClampMagnitude(velocity, MaxSpeed);
            transform.position += velocity * Time.deltaTime;
            if (velocity.sqrMagnitude > 0.01f) transform.rotation = Quaternion.LookRotation(velocity);
            if (!damageApplied && Vector3.Distance(transform.position, targetPosition) < 0.25f)
            {
                damageApplied = true;
                target.TakeDamage(damage);
                SpawnImpactBurst(transform.position, projectileColor);
                Destroy(gameObject);
            }
        }

        private void CreateArrowVisual()
        {
            GetComponent<Renderer>().enabled = false;
            Material shaftMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit")) { color = new Color(0.42f, 0.25f, 0.11f) };
            Material tipMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit")) { color = new Color(0.78f, 0.66f, 0.42f) };
            GameObject shaft = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            shaft.name = "Arrow shaft";
            shaft.transform.SetParent(transform, false);
            shaft.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            shaft.transform.localScale = new Vector3(0.035f, 0.22f, 0.035f);
            shaft.GetComponent<Renderer>().sharedMaterial = shaftMaterial;
            Destroy(shaft.GetComponent<Collider>());

            GameObject tip = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            tip.name = "Arrowhead";
            tip.transform.SetParent(transform, false);
            tip.transform.localPosition = Vector3.forward * 0.25f;
            tip.transform.localScale = new Vector3(0.075f, 0.075f, 0.14f);
            tip.GetComponent<Renderer>().sharedMaterial = tipMaterial;
            Destroy(tip.GetComponent<Collider>());
        }

        private static Material MakeParticleMaterial(Color color)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit") ?? Shader.Find("Unlit/Color");
            Material material = new Material(shader);
            material.color = color;
            return material;
        }

        private static void SpawnImpactBurst(Vector3 position, Color color)
        {
            GameObject burst = new GameObject("Arcane Bolt Impact");
            burst.transform.position = position;
            ParticleSystem particles = burst.AddComponent<ParticleSystem>();
            particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = particles.main;
            main.playOnAwake = false;
            main.loop = false;
            main.duration = 0.16f;
            main.startLifetime = 0.35f;
            main.startSpeed = 2.8f;
            main.startSize = 0.1f;
            main.startColor = color;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.stopAction = ParticleSystemStopAction.Destroy;
            var emission = particles.emission;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 10) });
            var shape = particles.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.08f;
            ParticleSystemRenderer renderer = burst.GetComponent<ParticleSystemRenderer>();
            renderer.material = MakeParticleMaterial(color);
            particles.Play();
        }
    }

    public sealed class TDCoinPickup : MonoBehaviour
    {
        private const float MergeRadius = 1.6f;
        [SerializeField] private float magnetRadius = 5.5f;
        [SerializeField] private float magnetSpeed = 12f;
        [SerializeField] private float magnetAcceleration = 30f;
        private float lifetime = 8f;
        private float currentSpeed;
        private float collectTimer;
        private bool collecting;
        private Vector3 initialScale;
        private TDResourceState state;
        private int value;
        private Transform hero;
        private GameObject visual;
        private bool collected;

        public int Value => value;

        public static TDCoinPickup Spawn(TDResourceState resource, int coinValue, Vector3 position)
        {
            foreach (TDCoinPickup pickup in FindObjectsByType<TDCoinPickup>())
            {
                if (pickup.state != resource || pickup.collecting || pickup.collectTimer > 0f || pickup.collected) continue;
                Vector3 offset = pickup.transform.position - position;
                if (offset.sqrMagnitude > MergeRadius * MergeRadius) continue;
                pickup.value += coinValue;
                pickup.lifetime = 8f;
                pickup.RefreshVisual();
                return pickup;
            }

            GameObject coin = new GameObject("Coin Drop");
            coin.transform.position = position;
            TDCoinPickup created = coin.AddComponent<TDCoinPickup>();
            created.Configure(resource, coinValue);
            return created;
        }

        public void Configure(TDResourceState resource, int coinValue)
        {
            state = resource;
            value = coinValue;
            initialScale = transform.localScale;
            RefreshVisual();
        }

        private void RefreshVisual()
        {
            bool stack = value > 10;
            string prefabPath = stack ? "TDAnnihilation/CoinStackVisual" : "TDAnnihilation/CoinVisual";
            GameObject prefab = Resources.Load<GameObject>(prefabPath);
            if (prefab == null)
            {
                Debug.LogError("Coin pickup visual missing: " + prefabPath);
                return;
            }
            if (visual != null)
            {
                if (Application.isPlaying) Destroy(visual);
                else DestroyImmediate(visual);
            }
            visual = Instantiate(prefab, transform);
            visual.name = prefab.name;
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localRotation = Quaternion.identity;
            foreach (Collider collider in visual.GetComponentsInChildren<Collider>(true)) collider.enabled = false;
            gameObject.name = stack ? "Coin Stack Drop" : "Coin Drop";
        }

        public void Collect()
        {
            if (collected) return;
            collected = true;
            if (state != null) state.gold += value;
            if (Application.isPlaying)
            {
                SpawnBurst(transform.position);
                Destroy(gameObject);
            }
        }

        private void Update()
        {
            if (hero == null)
            {
                TDHeroController heroController = FindAnyObjectByType<TDHeroController>();
                if (heroController != null) hero = heroController.transform;
            }
            if (collectTimer <= 0f && !collecting && hero != null && Vector3.Distance(transform.position, hero.position) <= magnetRadius)
            {
                collecting = true;
            }
            if (collecting && hero != null)
            {
                currentSpeed = Mathf.MoveTowards(currentSpeed, magnetSpeed, magnetAcceleration * Time.deltaTime);
                transform.position = Vector3.MoveTowards(transform.position, hero.position + Vector3.up * 0.7f, currentSpeed * Time.deltaTime);
                if (Vector3.Distance(transform.position, hero.position + Vector3.up * 0.7f) <= 0.45f)
                {
                    collecting = false;
                    collectTimer = 0.0001f;
                }
            }
            if (collectTimer > 0f)
            {
                collectTimer += Time.deltaTime;
                float t = collectTimer < 0.08f ? collectTimer / 0.08f : 1f - Mathf.Clamp01((collectTimer - 0.08f) / 0.10f);
                transform.localScale = initialScale * Mathf.Lerp(1f, 1.7f, Mathf.Clamp01(t));
                if (collectTimer >= 0.18f)
                {
                    Collect();
                }
                return;
            }
            transform.Rotate(0f, 180f * Time.deltaTime, 0f);
            transform.position += Vector3.up * Mathf.Sin(Time.time * 4f) * Time.deltaTime * 0.06f;
            lifetime -= Time.deltaTime;
            if (lifetime <= 0f) Destroy(gameObject);
        }

        private static void SpawnBurst(Vector3 position)
        {
            GameObject burst = new GameObject("Coin Pickup Burst");
            burst.transform.position = position;
            ParticleSystem particles = burst.AddComponent<ParticleSystem>();
            particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = particles.main;
            main.playOnAwake = false;
            main.duration = 0.2f;
            main.startLifetime = 0.35f;
            main.startSpeed = 3.2f;
            main.startSize = 0.12f;
            main.startColor = new Color(1f, 0.78f, 0.18f, 1f);
            main.maxParticles = 12;
            var emission = particles.emission;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 8) });
            var shape = particles.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.1f;
            ParticleSystemRenderer renderer = burst.GetComponent<ParticleSystemRenderer>();
            Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (shader == null) shader = Shader.Find("Unlit/Color");
            renderer.material = new Material(shader);
            renderer.material.color = new Color(1f, 0.78f, 0.18f, 1f);
            particles.Play();
            Destroy(burst, 0.6f);
        }
    }

}

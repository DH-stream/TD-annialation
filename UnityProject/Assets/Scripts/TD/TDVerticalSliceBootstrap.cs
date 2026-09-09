using System.Collections.Generic;
using UnityEngine;

namespace TDAnnihilation
{
    public sealed class TDVerticalSliceBootstrap : MonoBehaviour
    {
        [Header("Free Quaternius art")]
        [SerializeField] private GameObject warriorPrefab;
        [SerializeField] private GameObject demonPrefab;

        [Header("Slice tuning")]
        [SerializeField] private int startingGold = 120;
        [SerializeField] private int startingLives = 20;
        [SerializeField] private int stageWaveCount = 3;

        private readonly List<TDEnemyController> enemies = new List<TDEnemyController>();
        private readonly List<Vector3> path = new List<Vector3>();
        private TDResourceState state;
        private TDTowerController tower;
        private TDGameFlow flow;
        private bool ready;

        private void Awake()
        {
            if (ready) return;
            Application.runInBackground = true;
            ready = true;
            state = gameObject.AddComponent<TDResourceState>();
            flow = new TDGameFlow(stageWaveCount);
            state.gold = startingGold;
            state.lives = startingLives;
            LoadArtIfNeeded();
            BuildArena();
            Transform hero = SpawnHero();
            SpawnTower(new Vector3(-5f, GreenwardWorldLayout.HeightAt(-5f, -2f) + 0.3f, -2f));
            SpawnTower(new Vector3(22f, GreenwardWorldLayout.HeightAt(22f, 3f) + 0.3f, 3f));
            Camera camera = Camera.main;
            if (camera != null)
            {
                TDStrategicCamera strategic = camera.GetComponent<TDStrategicCamera>();
                if (strategic == null) strategic = camera.gameObject.AddComponent<TDStrategicCamera>();
                strategic.SetTarget(hero);
                camera.fieldOfView = 48f;
            }
            GreenwardLightingBuilder.Configure(camera);
            if (GetComponent<TDVerticalSliceHUD>() == null) gameObject.AddComponent<TDVerticalSliceHUD>();
        }

        private void Update()
        {
            enemies.RemoveAll(enemy => enemy == null);
            if (state == null || flow == null) return;
            if (state.lives <= 0)
            {
                flow.Lose();
                return;
            }
            if (flow.Phase == TDGamePhase.Wave && enemies.Count == 0) flow.CompleteWave();
        }

        private void LoadArtIfNeeded()
        {
            if (warriorPrefab == null) warriorPrefab = Resources.Load<GameObject>("TDAnnihilation/Warrior");
            if (demonPrefab == null) demonPrefab = Resources.Load<GameObject>("TDAnnihilation/Demon");
        }

        private void BuildArena()
        {
            path.AddRange(GreenwardWorldLayout.CreateRoute());
            GreenwardWorldBuilder.Build(path);
            return;
#pragma warning disable CS0162
            path.Add(new Vector3(-10f, 0.25f, 0f));
            path.Add(new Vector3(-5f, 0.25f, 0f));
            path.Add(new Vector3(-1f, 0.25f, 2.4f));
            path.Add(new Vector3(4f, 0.25f, 2.4f));
            path.Add(new Vector3(9f, 0.25f, 0f));

            MakePrimitive("Meadow", PrimitiveType.Plane, new Vector3(0f, 0f, 0f), new Vector3(2.2f, 1f, 1.4f), new Color(0.18f, 0.38f, 0.16f));
            for (int i = 0; i < path.Count - 1; i++)
            {
                Vector3 a = path[i];
                Vector3 b = path[i + 1];
                Vector3 midpoint = (a + b) * 0.5f;
                Vector3 delta = b - a;
                GameObject segment = MakePrimitive("Ancient Road", PrimitiveType.Cube, midpoint + Vector3.down * 0.16f, new Vector3(delta.magnitude, 0.22f, 2.2f), new Color(0.48f, 0.34f, 0.22f));
                segment.transform.rotation = Quaternion.Euler(0f, Mathf.Atan2(delta.z, delta.x) * Mathf.Rad2Deg, 0f);
            }

            for (int i = 0; i < 14; i++)
            {
                float angle = i * 1.71f;
                Vector3 position = new Vector3(Mathf.Cos(angle) * 10f, 0.45f, Mathf.Sin(angle) * 4.7f);
                MakePrimitive("Mossy Boulder", PrimitiveType.Sphere, position, Vector3.one * (0.45f + (i % 3) * 0.15f), new Color(0.24f, 0.30f, 0.19f));
            }

            MakePrimitive("Crystal Grove", PrimitiveType.Cylinder, new Vector3(7f, 0.85f, -2.5f), new Vector3(0.9f, 1.6f, 0.9f), new Color(0.18f, 0.60f, 0.78f));
            MakePrimitive("Crystal Glow", PrimitiveType.Sphere, new Vector3(7f, 2.2f, -2.5f), Vector3.one * 0.65f, new Color(0.38f, 0.78f, 1f));
            MakePrimitive("Kingdom Beacon", PrimitiveType.Cylinder, new Vector3(-7.6f, 1.2f, 3.6f), new Vector3(1.1f, 2.4f, 1.1f), new Color(0.55f, 0.32f, 0.14f));
            MakePrimitive("Beacon Flame", PrimitiveType.Sphere, new Vector3(-7.6f, 3.2f, 3.6f), Vector3.one * 0.8f, new Color(1f, 0.42f, 0.12f));
#pragma warning restore CS0162
        }

        private Transform SpawnHero()
        {
            Vector3 heroPosition = new Vector3(4f, GreenwardWorldLayout.HeightAt(4f, 4f) + 0.25f, 4f);
            GameObject hero = warriorPrefab != null
                ? Instantiate(warriorPrefab, heroPosition, Quaternion.Euler(0f, 25f, 0f))
                : MakePrimitive("Hero fallback", PrimitiveType.Capsule, heroPosition + Vector3.up, Vector3.one, new Color(0.12f, 0.28f, 0.42f));
            hero.name = "Hero - Warden of Greenward";
            hero.transform.localScale = Vector3.one * TDPresentationScale.Hero;
            EnsureAnimator(hero, "Warrior");
            if (hero.GetComponent<TDHeroController>() == null) hero.AddComponent<TDHeroController>();
            ApplyFantasyPalette(hero, true);
            return hero.transform;
        }

        private void SpawnTower(Vector3 position)
        {
            GameObject towerRoot = new GameObject("Arcane Watchtower");
            towerRoot.transform.position = position;
            MakePrimitive("Tower Base", PrimitiveType.Cylinder, towerRoot.transform.position + Vector3.up * 0.8f, new Vector3(1.5f, 1.6f, 1.5f), new Color(0.32f, 0.29f, 0.36f), towerRoot.transform);
            MakePrimitive("Tower Rune", PrimitiveType.Sphere, towerRoot.transform.position + Vector3.up * 2.45f, Vector3.one * 0.75f, new Color(0.38f, 0.25f, 0.96f), towerRoot.transform);
            tower = towerRoot.AddComponent<TDTowerController>();
            tower.state = state;
            tower.projectileColor = new Color(0.58f, 0.35f, 1f);
        }

        private void SpawnWave(int count)
        {
            if (path.Count == 0) path.AddRange(GreenwardWorldLayout.CreateRoute());
            for (int i = 0; i < count; i++)
            {
                int currentWave = flow == null ? 1 : flow.Wave;
                bool spawnElite = currentWave > 1 && currentWave % 4 == 0 && i == count - 1;
                TDEnemyArchetype archetype = spawnElite ? TDEnemyArchetype.Elite : TDEnemyArchetype.Raider;
                GameObject enemyObject = demonPrefab != null
                    ? Instantiate(demonPrefab, path[0] + Vector3.up * (0.25f + i * 0.02f) + Vector3.back * i * 0.75f, Quaternion.Euler(0f, 90f, 0f))
                    : MakePrimitive("Demon fallback", PrimitiveType.Capsule, path[0] + Vector3.back * i * 0.75f, Vector3.one, new Color(0.48f, 0.09f, 0.09f));
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
            animator.runtimeAnimatorController = Resources.Load<RuntimeAnimatorController>("TDAnnihilation/" + controllerName);
            animator.enabled = true;
        }

        private static void ApplyFantasyPalette(GameObject root, bool hero)
        {
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
        public void StartSoloStages()
        {
            state.gold = startingGold;
            state.lives = startingLives;
            state.defeated = 0;
            flow.SelectSoloStages();
        }

        public void StartNextWave()
        {
            if (flow.Phase != TDGamePhase.Build) return;
            flow.StartWave();
            SpawnWave(4 + flow.Wave);
        }

        public void ReturnToMenu() => flow.ReturnToMenu();
        public int CurrentWave => flow == null ? 0 : flow.Wave;
        public TDGamePhase Phase => flow == null ? TDGamePhase.MainMenu : flow.Phase;
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

        public void Configure(Vector3[] route, float maxHealth, float moveSpeed, TDResourceState resourceState)
        {
            path = route;
            health = maxHealth;
            speed = moveSpeed;
            state = resourceState;
            animator = GetComponentInChildren<Animator>();
            if (animator != null)
            {
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
                state.gold += 12;
                state.defeated++;
            }
            SpawnCoin();
            Destroy(gameObject);
        }

        private void SpawnCoin()
        {
            GameObject coin = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            coin.name = "Coin Drop";
            coin.transform.position = transform.position + Vector3.up * 0.8f;
            coin.transform.localScale = new Vector3(0.22f, 0.08f, 0.22f);
            coin.GetComponent<Renderer>().sharedMaterial = MakeMaterial(new Color(1f, 0.72f, 0.1f));
            coin.AddComponent<TDCoinPickup>();
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

        private void Update()
        {
            cooldown -= Time.deltaTime;
            if (cooldown > 0f) return;
            TDEnemyController[] targets = FindObjectsByType<TDEnemyController>();
            TDEnemyController nearest = null;
            float best = range * range;
            foreach (TDEnemyController target in targets)
            {
                float distance = (target.transform.position - transform.position).sqrMagnitude;
                if (distance < best) { best = distance; nearest = target; }
            }
            if (nearest == null) return;
            cooldown = fireRate;
            GameObject projectile = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            projectile.name = "Arcane Bolt";
            projectile.transform.position = transform.position + Vector3.up * 2.4f;
            projectile.transform.localScale = Vector3.one * 0.24f;
            projectile.GetComponent<Renderer>().sharedMaterial = MakeMaterial(projectileColor);
            projectile.AddComponent<TDProjectile>().Configure(nearest, 16f);
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

        public void Configure(TDEnemyController enemy, float hitDamage)
        {
            target = enemy;
            damage = hitDamage;
        }

        private void Update()
        {
            if (target == null) { Destroy(gameObject); return; }
            transform.position = Vector3.MoveTowards(transform.position, target.transform.position + Vector3.up, 13f * Time.deltaTime);
            if (Vector3.Distance(transform.position, target.transform.position + Vector3.up) < 0.25f)
            {
                target.TakeDamage(damage);
                Destroy(gameObject);
            }
        }
    }

    public sealed class TDCoinPickup : MonoBehaviour
    {
        private float lifetime = 8f;

        private void Update()
        {
            transform.Rotate(0f, 180f * Time.deltaTime, 0f);
            transform.position += Vector3.up * Mathf.Sin(Time.time * 4f) * Time.deltaTime * 0.06f;
            lifetime -= Time.deltaTime;
            if (lifetime <= 0f) Destroy(gameObject);
        }
    }

    public sealed class TDVerticalSliceHUD : MonoBehaviour
    {
        private TDVerticalSliceBootstrap game;
        private void Start() => game = FindAnyObjectByType<TDVerticalSliceBootstrap>();

        private void OnGUI()
        {
            if (game == null || game.State == null) return;
            if (game.Phase == TDGamePhase.MainMenu)
            {
                DrawMainMenu();
                return;
            }
            GUIStyle title = new GUIStyle(GUI.skin.label) { fontSize = 22, fontStyle = FontStyle.Bold, normal = { textColor = Color.white } };
            GUIStyle body = new GUIStyle(GUI.skin.label) { fontSize = 16, normal = { textColor = Color.white } };
            GUI.Box(new Rect(18f, 18f, 280f, 128f), GUIContent.none);
            GUI.Label(new Rect(34f, 28f, 250f, 30f), "GREENWARD WATCH", title);
            GUI.Label(new Rect(34f, 64f, 250f, 24f), "Gold  " + game.State.gold + "     Lives  " + game.State.lives, body);
            GUI.Label(new Rect(34f, 90f, 250f, 24f), "Wave  " + game.CurrentWave + "     Defeated  " + game.State.defeated, body);
            GUI.Label(new Rect(34f, 116f, 250f, 24f), "Arcane tower online", body);
            if (game.Phase == TDGamePhase.Build && GUI.Button(new Rect(18f, 158f, 180f, 42f), "START NEXT WAVE")) game.StartNextWave();
            if (game.Phase == TDGamePhase.Victory || game.Phase == TDGamePhase.Defeat)
            {
                string result = game.Phase == TDGamePhase.Victory ? "GREENWARD DEFENDED" : "GREENWARD HAS FALLEN";
                GUI.Box(new Rect(Screen.width * 0.5f - 190f, Screen.height * 0.5f - 90f, 380f, 180f), GUIContent.none);
                GUI.Label(new Rect(Screen.width * 0.5f - 145f, Screen.height * 0.5f - 55f, 310f, 38f), result, title);
                if (GUI.Button(new Rect(Screen.width * 0.5f - 100f, Screen.height * 0.5f + 18f, 200f, 44f), "RETURN TO MENU")) game.ReturnToMenu();
            }
        }

        private void DrawMainMenu()
        {
            float left = Screen.width * 0.5f - 240f;
            float top = Screen.height * 0.5f - 170f;
            GUIStyle heading = new GUIStyle(GUI.skin.label) { fontSize = 34, alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold, normal = { textColor = new Color(1f, 0.78f, 0.32f) } };
            GUIStyle copy = new GUIStyle(GUI.skin.label) { fontSize = 17, alignment = TextAnchor.MiddleCenter, normal = { textColor = Color.white } };
            GUI.Box(new Rect(left, top, 480f, 340f), GUIContent.none);
            GUI.Label(new Rect(left + 20f, top + 34f, 440f, 52f), "TD ANNIHILATION", heading);
            GUI.Label(new Rect(left + 30f, top + 92f, 420f, 52f), "Defend the royal heart. Shape the battlefield. Hold the line.", copy);
            if (GUI.Button(new Rect(left + 90f, top + 180f, 300f, 64f), "SOLO — GREENWARD STAGES")) game.StartSoloStages();
            GUI.Label(new Rect(left + 50f, top + 270f, 380f, 30f), "Stages • Endless and progression coming next", copy);
        }
    }
}

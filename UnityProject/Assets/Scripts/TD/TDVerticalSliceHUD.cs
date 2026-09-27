using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.UIElements;

namespace TDAnnihilation
{
    public sealed class TDVerticalSliceHUD : MonoBehaviour
    {
        private TDVerticalSliceBootstrap game;
        private UIDocument document;
        private VisualElement root;
        private readonly List<VisualElement> screens = new List<VisualElement>();
        private static readonly string[] SkillBranchNames = { "ARCHER", "ATTACK", "MAGE", "ECONOMY", "UTILITY", "SUPPORT", "CRIT", "COOLDOWN", "RANGE", "ELEMENTAL", "DEFENSE" };
        private static readonly Vector2[] SkillBranchPositions =
        {
            new Vector2(.29f, .23f), new Vector2(.44f, .15f), new Vector2(.64f, .20f),
            new Vector2(.79f, .35f), new Vector2(.79f, .59f), new Vector2(.68f, .77f),
            new Vector2(.54f, .83f), new Vector2(.39f, .79f), new Vector2(.25f, .68f),
            new Vector2(.18f, .51f), new Vector2(.22f, .35f)
        };
        private static readonly Color[] SkillBranchColors =
        {
            new Color(.98f,.68f,.20f), new Color(1f,.29f,.19f), new Color(.25f,.59f,1f),
            new Color(.76f,.34f,.98f), new Color(.70f,.83f,.97f), new Color(.22f,.80f,.64f),
            new Color(1f,.69f,.23f), new Color(.65f,.35f,.94f), new Color(.13f,.68f,.95f),
            new Color(1f,.40f,.16f), new Color(.38f,.84f,.33f)
        };
        private static readonly string[] SkillBranchGlyphs = { "A", "⚔", "M", "¤", "U", "S", "★", "⌛", "R", "✹", "D" };
        private const float SkillCanvasWidth = 1400f;
        private const float SkillCanvasHeight = 1100f;
        private static readonly Vector2 SkillCanvasCenter = new Vector2(SkillCanvasWidth * .5f, SkillCanvasHeight * .5f);
        private static readonly float[] SkillNodeRadii = { 170f, 260f, 265f, 365f, 375f, 490f };
        private static readonly float[] SkillNodeOffsets = { 0f, -28f, 45f, -68f, 95f, -76f };
        private readonly Dictionary<string, Vector2> skillPositions = new Dictionary<string, Vector2>();
        private readonly List<SkillNodeAura> skillAuras = new List<SkillNodeAura>();
        private static Texture2D skillGlowTexture;
        private sealed class SkillNodeAura
        {
            public Image halo;
            public VisualElement[] particles;
            public Vector2 center;
            public float phase;
        }
        private IReadOnlyList<TDSkillNodeDefinition> skillNodes;
        private const float CoinPulseDuration = 0.55f;
        private VisualElement mainMenuScreen;
        private VisualElement stageSelectScreen;
        private VisualElement skillTreeScreen;
        private VisualElement settingsScreen;
        private VisualElement gameplayScreen;
        private VisualElement resultScreen;
        private VisualElement pauseOverlay;
        private VisualElement buildPanel;
        private VisualElement buildWheelOverlay;
        private VisualElement buildWheel;
        private VisualElement minimapFrame;
        private TDGreenwardMinimap minimap;
        private VisualElement skillGraph;
        private VisualElement skillCanvas;
        private VisualElement skillHoverCard;
        private Label skillHoverName;
        private Label skillHoverEffect;
        private Label skillHoverState;
        private Vector2 skillPan;
        private Vector2 skillDragStart;
        private float skillZoom = .47f;
        private bool skillPanning;
        private Image gameLogo;
        private Image hudCoinsImage;
        private Image hudManaImage;
        private Image hudHealthImage;
        private VisualElement hudManaZone;
        private VisualElement hudHealthZone;
        private Image hudHealthAura;
        private readonly List<VisualElement> manaDust = new List<VisualElement>();
        private Image hudAttackImage;
        private Image hudHeavyImage;
        private Image hudMegaImage;
        private Button buildButton;
        private Button arcaneBuildOption;
        private Button archerBuildOption;
        private Button attackButton;
        private Button heavyAttackButton;
        private Button megaAttackButton;
        private TDHudCooldownRing lightCooldownRing;
        private TDHudCooldownRing heavyCooldownRing;
        private TDHudCooldownRing megaCooldownRing;
        private VisualElement[] lightChargePips;
        private VisualElement[] heavyChargePips;
        private VisualElement[] megaChargePips;
        private Button bindLightButton;
        private Button bindHeavyButton;
        private Button bindMegaButton;
        private Label lightKeycap;
        private Label heavyKeycap;
        private Label megaKeycap;
        private Label keybindStatus;
        private TDAttackKeyBindings attackKeyBindings;
        private TDAttackType? pendingBinding;
        private Label stageValue;
        private Label stageWaveValue;
        private Label waveValue;
        private Label defeatedValue;
        private Label phaseValue;
        private Label statusValue;
        private Label hudCoinsValue;
        private Label hudManaValue;
        private Label hudHealthValue;
        private Label hudAttackValue;
        private Label hudHeavyValue;
        private Label hudMegaValue;
        private Label resultTitle;
        private Label resultCopy;
        private Label skillDetailsTitle;
        private Label skillDetailsCopy;
        private Label skillDetailsCost;
        private Label skillDetailsEffect;
        private Label skillCurrency;
        private Button skillPurchaseButton;
        private Button endlessStageButton;
        private Button continueButton;
        private Label continueDetail;
        private TDMenuScreen currentScreen;
        private string selectedSkillId;
        private bool wheelDragging;
        private bool wheelSelected;
        private bool wheelArcherSelected;
        private int displayedGold = int.MinValue;
        private float coinPickupPulse;
        private bool paused;

        private void Start()
        {
            game = GetComponent<TDVerticalSliceBootstrap>();
            document = GetComponent<UIDocument>();
            if (document == null) document = gameObject.AddComponent<UIDocument>();

            PanelSettings panelSettings = Resources.Load<PanelSettings>("TDAnnihilation/UI/GreenwardPanelSettings");
            if (panelSettings == null)
            {
                panelSettings = ScriptableObject.CreateInstance<PanelSettings>();
                panelSettings.scaleMode = PanelScaleMode.ScaleWithScreenSize;
                panelSettings.referenceResolution = new Vector2Int(1280, 720);
                panelSettings.match = 0.5f;
            }

            VisualTreeAsset visualTree = Resources.Load<VisualTreeAsset>("TDAnnihilation/UI/GreenwardRoot");
            if (visualTree == null)
            {
                Debug.LogError("Greenward UI root could not be loaded from Resources/TDAnnihilation/UI/GreenwardRoot.");
                return;
            }

            document.enabled = false;
            document.panelSettings = panelSettings;
            document.visualTreeAsset = visualTree;
            document.enabled = true;

            root = document.rootVisualElement;
            root.style.width = Length.Percent(100);
            root.style.height = Length.Percent(100);
            StyleSheet style = Resources.Load<StyleSheet>("TDAnnihilation/UI/Greenward");
            if (style != null && !root.styleSheets.Contains(style)) root.styleSheets.Add(style);
            skillNodes = TDSkillTreeCatalog.CreateDefault();
            attackKeyBindings = TDAttackKeyBindings.Load();
            CacheElements();
            Texture2D skillBackdrop = Resources.Load<Texture2D>("TDAnnihilation/UI/SkillTreeBackdrop");
            if (skillBackdrop != null) skillTreeScreen.style.backgroundImage = new StyleBackground(skillBackdrop);
            InitializeMinimap();
            InitializeCooldownRings();
            RefreshAttackKeyLabels();
            Texture2D logo = Resources.Load<Texture2D>("TDAnnihilation/UI/TDAnnihilationLogo");
            if (logo != null)
            {
                gameLogo.image = logo;
                gameLogo.scaleMode = ScaleMode.ScaleToFit;
            }
            AssignHudImage(hudCoinsImage, "Coins");
            AssignHudImage(hudManaImage, "Mana");
            AssignHudImage(hudHealthImage, "Health");
            InitializeHudEffects();
            AssignHudImage(hudAttackImage, "Attack");
            AssignHudImage(hudHeavyImage, "HeavyAttack");
            AssignHudImage(hudMegaImage, "MegaAttack");
            RegisterCallbacks();
            RegisterWheelInput();
            RegisterSkillGraphInput();
            BuildSkillGraph();
            ShowMainMenu();
        }

        private void Update()
        {
            if (game == null || game.State == null || root == null) return;
            RefreshGameplayValues();
            UpdateHudEffects();
            HandleKeyboardInput();
            if (currentScreen == TDMenuScreen.Gameplay) minimap?.Refresh();
            if (currentScreen == TDMenuScreen.SkillTree) UpdateSkillEffects();
            if (game.Phase == TDGamePhase.Victory || game.Phase == TDGamePhase.Defeat)
            {
                if (currentScreen != TDMenuScreen.Result) ShowResult();
            }
            else if (game.Phase == TDGamePhase.Build || game.Phase == TDGamePhase.Wave)
            {
                if (currentScreen != TDMenuScreen.Gameplay) ShowGameplay();
            }
        }

        public void ShowMainMenu()
        {
            SetScreen(mainMenuScreen, TDMenuScreen.MainMenu);
            continueButton.SetEnabled(game.HasSavedRun);
            continueDetail.text = game.HasSavedRun ? game.SavedRunLabel : "NO SAVED RUN";
        }

        public void ShowStageSelect()
        {
            SetScreen(stageSelectScreen, TDMenuScreen.StageSelect);
            RefreshStageSelect();
        }

        public void ShowSkillTree()
        {
            SetScreen(skillTreeScreen, TDMenuScreen.SkillTree);
            BuildSkillGraph();
        }

        public void ShowSettings()
        {
            SetScreen(settingsScreen, TDMenuScreen.Settings);
        }

        public void ShowGameplay()
        {
            SetScreen(gameplayScreen, TDMenuScreen.Gameplay);
            RefreshGameplayValues();
        }

        public void ShowResult()
        {
            if (game == null || resultScreen == null) return;
            resultTitle.text = game.Phase == TDGamePhase.Victory ? "GREENWARD DEFENDED" : "GREENWARD HAS FALLEN";
            resultCopy.text = (game.Phase == TDGamePhase.Victory
                ? "The valley holds. Enemies defeated: " + game.State.defeated + ". "
                : "The gate has fallen. Enemies defeated: " + game.State.defeated + ". ")
                + "Skulls earned this run: +" + game.RunSkullsEarned
                + " · Total skulls: " + game.Progression.SkullBalance;
            SetScreen(resultScreen, TDMenuScreen.Result);
        }

        private void CacheElements()
        {
            mainMenuScreen = root.Q<VisualElement>("main-menu-screen");
            stageSelectScreen = root.Q<VisualElement>("stage-select-screen");
            skillTreeScreen = root.Q<VisualElement>("skill-tree-screen");
            settingsScreen = root.Q<VisualElement>("settings-screen");
            gameplayScreen = root.Q<VisualElement>("gameplay-screen");
            resultScreen = root.Q<VisualElement>("result-screen");
            buildPanel = root.Q<VisualElement>("build-panel");
            buildWheelOverlay = root.Q<VisualElement>("build-wheel-overlay");
            buildWheel = root.Q<VisualElement>("build-wheel");
            minimapFrame = root.Q<VisualElement>("minimap-frame");
            skillGraph = root.Q<VisualElement>("skill-graph");
            skillGraph.RegisterCallback<GeometryChangedEvent>(_ => PositionSkillCanvas());
            gameLogo = root.Q<Image>("game-logo");
            continueButton = root.Q<Button>("continue-button");
            continueDetail = root.Q<Label>("continue-detail");
            hudCoinsImage = root.Q<Image>("hud-coins-image");
            hudManaImage = root.Q<Image>("hud-mana-image");
            hudHealthImage = root.Q<Image>("hud-health-image");
            hudManaZone = root.Q<VisualElement>("hud-mana-zone");
            hudHealthZone = root.Q<VisualElement>("hud-health-zone");
            hudAttackImage = root.Q<Image>("hud-attack-image");
            hudHeavyImage = root.Q<Image>("hud-heavy-image");
            hudMegaImage = root.Q<Image>("hud-mega-image");
            buildButton = root.Q<Button>("build-button");
            arcaneBuildOption = root.Q<Button>("placement-button");
            archerBuildOption = root.Q<Button>("wheel-archer-button");
            attackButton = root.Q<Button>("attack-button");
            heavyAttackButton = root.Q<Button>("heavy-attack-button");
            megaAttackButton = root.Q<Button>("mega-attack-button");
            bindLightButton = root.Q<Button>("bind-light-button");
            bindHeavyButton = root.Q<Button>("bind-heavy-button");
            bindMegaButton = root.Q<Button>("bind-mega-button");
            lightKeycap = root.Q<Label>("light-keycap");
            heavyKeycap = root.Q<Label>("heavy-keycap");
            megaKeycap = root.Q<Label>("mega-keycap");
            keybindStatus = root.Q<Label>("keybind-status");
            pauseOverlay = root.Q<VisualElement>("pause-overlay");
            stageValue = root.Q<Label>("stage-value");
            stageWaveValue = root.Q<Label>("stage-wave-value");
            waveValue = root.Q<Label>("wave-value");
            defeatedValue = root.Q<Label>("defeated-value");
            phaseValue = root.Q<Label>("phase-value");
            statusValue = root.Q<Label>("status-value");
            hudCoinsValue = root.Q<Label>("hud-coins-value");
            hudManaValue = root.Q<Label>("hud-mana-value");
            hudHealthValue = root.Q<Label>("hud-health-value");
            hudAttackValue = root.Q<Label>("hud-attack-value");
            hudHeavyValue = root.Q<Label>("hud-heavy-value");
            hudMegaValue = root.Q<Label>("hud-mega-value");
            resultTitle = root.Q<Label>("result-title");
            resultCopy = root.Q<Label>("result-copy");
            skillDetailsTitle = root.Q<Label>("skill-details-title");
            skillDetailsCopy = root.Q<Label>("skill-details-copy");
            skillDetailsCost = root.Q<Label>("skill-details-cost");
            skillDetailsEffect = root.Q<Label>("skill-details-effect");
            skillCurrency = root.Q<Label>("skill-currency");
            skillPurchaseButton = root.Q<Button>("skill-purchase-button");
            endlessStageButton = root.Q<Button>("stage-greenward-endless-button");
            endlessStageButton.style.display = DisplayStyle.None;
            screens.Add(mainMenuScreen);
            screens.Add(stageSelectScreen);
            screens.Add(skillTreeScreen);
            screens.Add(settingsScreen);
            screens.Add(gameplayScreen);
            screens.Add(resultScreen);
        }

        private void InitializeMinimap()
        {
            if (minimapFrame == null) return;
            VisualElement placeholder = minimapFrame.Q<VisualElement>("minimap-canvas");
            if (placeholder != null) placeholder.RemoveFromHierarchy();
            minimap = new TDGreenwardMinimap();
            minimap.AddToClassList("minimap-canvas");
            minimapFrame.Add(minimap);
            minimap.Bind(game);
        }

        private void RegisterCallbacks()
        {
            root.Q<Button>("play-button").clicked += ShowStageSelect;
            continueButton.clicked += ContinueGreenward;
            root.Q<Button>("skill-tree-button").clicked += ShowSkillTree;
            root.Q<Button>("settings-button").clicked += ShowSettings;
            root.Q<Button>("quit-button").clicked += QuitGame;
            root.Q<Button>("stage-greenward-button").clicked += () => StartGreenward();
            endlessStageButton.clicked += () => StartGreenward(TDRunMode.Endless);
            skillPurchaseButton.clicked += PurchaseSelectedSkill;
            root.Q<Button>("stage-two-button").SetEnabled(false);
            root.Q<Button>("stage-three-button").SetEnabled(false);
            root.Q<Button>("stage-select-back-button").clicked += ShowMainMenu;
            root.Q<Button>("skill-tree-back-button").clicked += ShowMainMenu;
            root.Q<Button>("settings-back-button").clicked += ShowMainMenu;
            bindLightButton.clicked += () => BeginKeyBinding(TDAttackType.Light);
            bindHeavyButton.clicked += () => BeginKeyBinding(TDAttackType.Heavy);
            bindMegaButton.clicked += () => BeginKeyBinding(TDAttackType.Mega);
            buildButton.clicked += ToggleBuildWheel;
            attackButton.clicked += () => PerformAttack(TDAttackType.Light);
            heavyAttackButton.clicked += () => PerformAttack(TDAttackType.Heavy);
            megaAttackButton.clicked += () => PerformAttack(TDAttackType.Mega);
            root.Q<Button>("pause-resume-button").clicked += ResumeGame;
            root.Q<Button>("pause-menu-button").clicked += ReturnToTitle;
            root.Q<Button>("placement-button").clicked += SelectArcaneTower;
            root.Q<Button>("wheel-archer-button").clicked += SelectArcherTower;
            root.Q<Button>("wheel-cancel").clicked += CloseBuildWheel;
            root.Q<Button>("wave-button").clicked += StartNextWave;
            root.Q<Button>("result-replay-button").clicked += Replay;
            root.Q<Button>("result-stage-select-button").clicked += OpenStageSelectFromResult;
            root.Q<Button>("result-menu-button").clicked += OpenMainMenuFromResult;
        }

        private void InitializeCooldownRings()
        {
            lightCooldownRing = ReplaceCooldownRing("light-cooldown-ring", new Color(1f, 0.68f, 0.16f));
            heavyCooldownRing = ReplaceCooldownRing("heavy-cooldown-ring", new Color(0.24f, 0.68f, 1f));
            megaCooldownRing = ReplaceCooldownRing("mega-cooldown-ring", new Color(1f, 0.28f, 0.14f));
            lightChargePips = CreateChargePips("light-charge-row");
            heavyChargePips = CreateChargePips("heavy-charge-row");
            megaChargePips = CreateChargePips("mega-charge-row");
        }

        private VisualElement[] CreateChargePips(string rowName)
        {
            VisualElement row = root.Q<VisualElement>(rowName);
            if (row == null) return null;
            var pips = new VisualElement[4];
            for (int i = 0; i < pips.Length; i++)
            {
                pips[i] = new VisualElement();
                pips[i].AddToClassList("hud-charge-pip");
                pips[i].pickingMode = PickingMode.Ignore;
                row.Add(pips[i]);
            }
            return pips;
        }

        private TDHudCooldownRing ReplaceCooldownRing(string elementName, Color color)
        {
            VisualElement placeholder = root.Q<VisualElement>(elementName);
            var ring = new TDHudCooldownRing(color) { name = elementName };
            ring.AddToClassList("hud-cooldown-ring");
            if (placeholder != null)
            {
                foreach (string className in placeholder.GetClasses())
                    if (className != "hud-cooldown-ring") ring.AddToClassList(className);
                placeholder.parent.Insert(placeholder.parent.IndexOf(placeholder), ring);
                placeholder.RemoveFromHierarchy();
            }
            return ring;
        }

        private void BeginKeyBinding(TDAttackType type)
        {
            pendingBinding = type;
            keybindStatus.text = "PRESS A KEY FOR " + type.ToString().ToUpperInvariant() + " · ESC TO CANCEL";
            GetBindingButton(type).text = "...";
        }

        private Button GetBindingButton(TDAttackType type)
        {
            switch (type)
            {
                case TDAttackType.Heavy: return bindHeavyButton;
                case TDAttackType.Mega: return bindMegaButton;
                default: return bindLightButton;
            }
        }

        private void RefreshAttackKeyLabels()
        {
            SetKeyLabel(lightKeycap, bindLightButton, attackKeyBindings.GetKey(TDAttackType.Light));
            SetKeyLabel(heavyKeycap, bindHeavyButton, attackKeyBindings.GetKey(TDAttackType.Heavy));
            SetKeyLabel(megaKeycap, bindMegaButton, attackKeyBindings.GetKey(TDAttackType.Mega));
        }

        private static void SetKeyLabel(Label keycap, Button button, Key key)
        {
            string label = key.ToString().Replace("Digit", "");
            if (keycap != null) keycap.text = label;
            if (button != null) button.text = label;
        }

        private static void AssignHudImage(Image target, string resourceName)
        {
            if (target == null) return;
            Texture2D image = Resources.Load<Texture2D>("TDAnnihilation/UI/HUD/" + resourceName);
            if (image == null)
            {
                Debug.LogWarning("Missing HUD texture: " + resourceName);
                return;
            }
            target.image = image;
            target.scaleMode = ScaleMode.ScaleToFit;
            if (resourceName == "Attack") target.sourceRect = new Rect(0f, 80f, 470f, 840f);
            else if (resourceName == "HeavyAttack" || resourceName == "MegaAttack")
                target.sourceRect = new Rect(90f, 70f, 844f, 520f);
        }

        private void InitializeHudEffects()
        {
            if (hudHealthImage != null && hudHealthZone != null && hudHealthImage.image != null)
            {
                hudHealthAura = new Image { image = hudHealthImage.image, scaleMode = ScaleMode.ScaleToFit };
                hudHealthAura.tintColor = new Color(1f, 0.12f, 0.08f, 1f);
                hudHealthAura.pickingMode = PickingMode.Ignore;
                hudHealthAura.style.position = Position.Absolute;
                hudHealthAura.style.left = 0;
                hudHealthAura.style.top = 0;
                hudHealthAura.style.width = Length.Percent(100);
                hudHealthAura.style.height = Length.Percent(100);
                hudHealthZone.Insert(0, hudHealthAura);
            }

            if (hudManaZone == null) return;
            for (int i = 0; i < 4; i++)
            {
                VisualElement mote = new VisualElement();
                mote.AddToClassList("hud-mana-dust");
                mote.pickingMode = PickingMode.Ignore;
                hudManaZone.Insert(1, mote);
                manaDust.Add(mote);
            }
        }

        private void UpdateHudEffects()
        {
            float time = Time.unscaledTime;
            for (int i = 0; i < manaDust.Count; i++)
            {
                float progress = Mathf.Repeat(time * 0.22f + i * 0.25f, 1f);
                float shimmer = Mathf.Sin(progress * Mathf.PI);
                manaDust[i].style.left = Length.Percent(12f + Mathf.Sin(time * 1.4f + i * 1.7f) * 11f + progress * 18f);
                manaDust[i].style.top = Length.Percent(68f - progress * 58f + Mathf.Cos(time * 1.8f + i) * 7f);
                manaDust[i].style.opacity = shimmer * 0.82f;
                manaDust[i].style.scale = new Scale(Vector2.one * (0.7f + shimmer * 0.55f));
            }

            int maxLives = Mathf.Max(1, game.StartingLives);
            float healthRisk = 1f - Mathf.Clamp01((float)game.State.lives / maxLives);
            float pulse = (Mathf.Sin(time * (3f + healthRisk * 5f)) + 1f) * 0.5f;
            if (hudHealthAura != null)
            {
                hudHealthAura.style.opacity = (0.12f + healthRisk * 0.48f) * (0.6f + pulse * 0.4f);
                float size = 1.045f + healthRisk * (0.05f + pulse * 0.12f);
                hudHealthAura.style.scale = new Scale(Vector2.one * size);
            }

            coinPickupPulse = Mathf.Max(0f, coinPickupPulse - Time.unscaledDeltaTime);
            float coinPulse = coinPickupPulse <= 0f ? 0f : Mathf.Sin((1f - coinPickupPulse / CoinPulseDuration) * Mathf.PI);
            if (hudCoinsImage != null)
            {
                hudCoinsImage.style.scale = new Scale(Vector2.one * (1f + coinPulse * 0.2f));
                hudCoinsImage.tintColor = Color.Lerp(Color.white, new Color(1f, 0.82f, 0.42f), coinPulse * 0.45f);
            }
            if (hudCoinsValue != null) hudCoinsValue.style.scale = new Scale(Vector2.one * (1f + coinPulse * 0.18f));
        }

        private void HandleKeyboardInput()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null) return;
            if (pendingBinding.HasValue)
            {
                if (keyboard.escapeKey.wasPressedThisFrame)
                {
                    pendingBinding = null;
                    keybindStatus.text = "KEY BINDING CANCELLED";
                    RefreshAttackKeyLabels();
                    return;
                }
                for (int i = 0; i < keyboard.allKeys.Count; i++)
                {
                    KeyControl key = keyboard.allKeys[i];
                    if (!key.wasPressedThisFrame) continue;
                    TDAttackType type = pendingBinding.Value;
                    pendingBinding = null;
                    if (attackKeyBindings.TrySetKey(type, key.keyCode))
                    {
                        attackKeyBindings.Save();
                        keybindStatus.text = "KEY BINDING SAVED";
                    }
                    else keybindStatus.text = "KEY IN USE · CHOOSE ANOTHER";
                    RefreshAttackKeyLabels();
                    return;
                }
                return;
            }
            if (currentScreen == TDMenuScreen.Gameplay && keyboard.escapeKey.wasPressedThisFrame)
            {
                if (paused) ResumeGame();
                else if (buildWheelOverlay.style.display == DisplayStyle.Flex) CloseBuildWheel();
                else PauseGame();
                return;
            }
            if (currentScreen != TDMenuScreen.Gameplay || paused) return;
            if (keyboard[attackKeyBindings.GetKey(TDAttackType.Light)].wasPressedThisFrame) PerformAttack(TDAttackType.Light);
            if (keyboard[attackKeyBindings.GetKey(TDAttackType.Heavy)].wasPressedThisFrame) PerformAttack(TDAttackType.Heavy);
            if (keyboard[attackKeyBindings.GetKey(TDAttackType.Mega)].wasPressedThisFrame) PerformAttack(TDAttackType.Mega);
            if (keyboard.spaceKey.wasPressedThisFrame && game.Phase == TDGamePhase.Build) StartNextWave();
        }

        private void PauseGame()
        {
            paused = true;
            Time.timeScale = 0f;
            pauseOverlay.style.display = DisplayStyle.Flex;
        }

        private void ResumeGame()
        {
            paused = false;
            Time.timeScale = 1f;
            if (pauseOverlay != null) pauseOverlay.style.display = DisplayStyle.None;
        }

        private void ReturnToTitle()
        {
            ResumeGame();
            game.ReturnToMenu();
            ShowMainMenu();
        }

        private void OnDisable()
        {
            if (paused) ResumeGame();
        }

        private void RegisterWheelInput()
        {
            if (buildWheel == null) return;
            buildWheel.RegisterCallback<PointerDownEvent>(evt =>
            {
                wheelDragging = true;
                UpdateWheelSelection(evt.position);
                evt.StopPropagation();
            });
            buildWheel.RegisterCallback<PointerMoveEvent>(evt =>
            {
                if (!wheelDragging) return;
                UpdateWheelSelection(evt.position);
                evt.StopPropagation();
            });
            buildWheel.RegisterCallback<PointerUpEvent>(evt =>
            {
                if (wheelSelected)
                {
                    if (wheelArcherSelected) SelectArcherTower();
                    else SelectArcaneTower();
                }
                else CloseBuildWheel();
                evt.StopPropagation();
            });
        }

        private void UpdateWheelSelection(Vector2 panelPosition)
        {
            Vector2 delta = panelPosition - buildWheel.worldBound.center;
            wheelArcherSelected = delta.x > 35f && Mathf.Abs(delta.y) < 118f;
            wheelSelected = (delta.y < -35f && Mathf.Abs(delta.x) < 118f) || wheelArcherSelected;
            if (wheelSelected) buildWheel.AddToClassList("has-selection");
            else buildWheel.RemoveFromClassList("has-selection");
            if (wheelArcherSelected) buildWheel.AddToClassList("selecting-archer");
            else buildWheel.RemoveFromClassList("selecting-archer");
        }

        private void BuildSkillGraph()
        {
            if (skillGraph == null || skillNodes == null) return;
            skillGraph.Clear();
            skillPositions.Clear();
            skillAuras.Clear();
            skillCanvas = new VisualElement { name = "skill-canvas" };
            skillCanvas.AddToClassList("skill-canvas");
            skillCanvas.style.width = SkillCanvasWidth;
            skillCanvas.style.height = SkillCanvasHeight;
            skillCanvas.generateVisualContent += DrawSkillConnections;
            skillGraph.Add(skillCanvas);

            for (int branchIndex = 0; branchIndex < SkillBranchNames.Length; branchIndex++)
            {
                Vector2 direction = (SkillBranchPositions[branchIndex] - new Vector2(.5f, .5f)).normalized;
                Vector2 tangent = new Vector2(-direction.y, direction.x);
                int nodeIndex = 0;
                for (int i = 0; i < skillNodes.Count; i++)
                {
                    TDSkillNodeDefinition node = skillNodes[i];
                    if ((int)node.Branch != branchIndex) continue;
                    int positionIndex = Mathf.Min(nodeIndex, SkillNodeRadii.Length - 1);
                    skillPositions[node.Id] = SkillCanvasCenter + direction * SkillNodeRadii[positionIndex]
                        + tangent * SkillNodeOffsets[positionIndex];
                    nodeIndex++;
                }
            }

            AddSkillAura(SkillCanvasCenter, new Color(1f, .68f, .18f), -1);
            VisualElement core = new VisualElement();
            core.AddToClassList("skill-core");
            core.Add(new Label("✦") { name = "skill-core-glyph" });
            core.Add(new Label("FORTRESS CORE") { name = "skill-core-title" });
            core.Add(new Label("LAST BASTION") { name = "skill-core-subtitle" });
            core.style.left = SkillCanvasCenter.x;
            core.style.top = SkillCanvasCenter.y;
            skillCanvas.Add(core);
            for (int branchIndex = 0; branchIndex < SkillBranchNames.Length; branchIndex++)
            {
                int branchNodeIndex = 0;
                for (int i = 0; i < skillNodes.Count; i++)
                {
                    TDSkillNodeDefinition node = skillNodes[i];
                    if ((int)node.Branch != branchIndex) continue;
                    int depth = SkillRevealDepth(node);
                    Vector2 position = skillPositions[node.Id];
                    if (depth > 3) { branchNodeIndex++; continue; }
                    if (depth == 3)
                    {
                        VisualElement veil = new VisualElement();
                        veil.AddToClassList("skill-veil");
                        veil.style.left = position.x;
                        veil.style.top = position.y;
                        veil.Add(new Label("?"));
                        skillCanvas.Add(veil);
                        branchNodeIndex++;
                        continue;
                    }
                    if (game.Progression.IsUnlocked(node.Id)) AddSkillAura(position, SkillBranchColors[branchIndex], i);
                    TDSkillNodeDefinition selectedNode = node;
                    Button button = new Button(() => SelectSkill(selectedNode))
                    {
                        text = branchNodeIndex == 0 ? SkillBranchGlyphs[branchIndex] : SkillGlyph(node.Effect)
                    };
                    button.AddToClassList(branchNodeIndex == 0 ? "skill-hub" : "skill-leaf");
                    button.AddToClassList("skill-color-" + branchIndex);
                    ApplySkillNodeState(button, node);
                    if (selectedSkillId == node.Id) button.AddToClassList("is-selected");
                    button.style.left = position.x;
                    button.style.top = position.y;
                    button.RegisterCallback<PointerEnterEvent>(_ => ShowSkillHover(node, button, depth));
                    button.RegisterCallback<PointerLeaveEvent>(_ => HideSkillHover());
                    skillCanvas.Add(button);
                    if (branchNodeIndex == 0)
                    {
                        Label branchLabel = new Label(SkillBranchNames[branchIndex]);
                        branchLabel.AddToClassList("skill-branch-label");
                        branchLabel.pickingMode = PickingMode.Ignore;
                        branchLabel.style.left = position.x;
                        branchLabel.style.top = position.y + 44f;
                        skillCanvas.Add(branchLabel);
                    }
                    else if (!game.Progression.IsUnlocked(node.Id))
                    {
                        Label cost = new Label("☠ " + node.Cost);
                        cost.AddToClassList("skill-cost-tag");
                        cost.pickingMode = PickingMode.Ignore;
                        cost.style.left = position.x;
                        cost.style.top = position.y + 27f;
                        skillCanvas.Add(cost);
                    }
                    branchNodeIndex++;
                }
            }

            skillHoverCard = new VisualElement { name = "skill-hover-card" };
            skillHoverCard.AddToClassList("skill-hover-card");
            skillHoverCard.pickingMode = PickingMode.Ignore;
            skillHoverName = new Label();
            skillHoverName.AddToClassList("skill-hover-name");
            skillHoverEffect = new Label();
            skillHoverEffect.AddToClassList("skill-hover-effect");
            skillHoverState = new Label();
            skillHoverState.AddToClassList("skill-hover-state");
            skillHoverCard.Add(skillHoverName);
            skillHoverCard.Add(skillHoverEffect);
            skillHoverCard.Add(skillHoverState);
            skillGraph.Add(skillHoverCard);
            HideSkillHover();
            PositionSkillCanvas();
            skillCanvas.MarkDirtyRepaint();
            RefreshSkillCurrency();
            RefreshSelectedSkill();
        }

        private void ApplySkillNodeState(VisualElement element, TDSkillNodeDefinition node)
        {
            if (game.Progression.IsUnlocked(node.Id)) element.AddToClassList("is-purchased");
            else if (game.Progression.CanUnlock(node.Id, skillNodes)) element.AddToClassList("is-available");
            else element.AddToClassList("is-locked");
        }

        private void DrawSkillConnections(MeshGenerationContext context)
        {
            Painter2D painter = context.painter2D;
            for (int i = 0; i < skillNodes.Count; i++)
            {
                TDSkillNodeDefinition node = skillNodes[i];
                int depth = SkillRevealDepth(node);
                if (depth > 3 || !skillPositions.TryGetValue(node.Id, out Vector2 target)) continue;
                Vector2 origin = string.IsNullOrEmpty(node.PrerequisiteId) ? SkillCanvasCenter
                    : skillPositions.TryGetValue(node.PrerequisiteId, out Vector2 parent) ? parent : SkillCanvasCenter;
                bool owned = game.Progression.IsUnlocked(node.Id);
                Color color = SkillBranchColors[(int)node.Branch];
                Color shadow = new Color(.05f, .07f, .09f, .9f);
                painter.strokeColor = shadow;
                painter.lineWidth = 10f;
                painter.BeginPath();
                painter.MoveTo(origin);
                painter.LineTo(target);
                painter.Stroke();
                color.a = owned ? .27f : depth == 1 ? .16f : .05f;
                painter.strokeColor = color;
                painter.lineWidth = owned ? 12f : 7f;
                painter.BeginPath();
                painter.MoveTo(origin);
                painter.LineTo(target);
                painter.Stroke();
                color.a = owned ? .95f : depth == 1 ? .60f : .20f;
                painter.strokeColor = color;
                painter.lineWidth = owned ? 2.6f : 1.6f;
                painter.BeginPath();
                painter.MoveTo(origin);
                painter.LineTo(target);
                painter.Stroke();
                if (owned)
                {
                    float travel = Mathf.Repeat(Time.unscaledTime * .25f + i * .17f, 1f);
                    Vector2 spark = Vector2.Lerp(origin, target, travel);
                    DrawSkillSpark(painter, spark, color);
                }
            }
        }

        private static void DrawSkillSpark(Painter2D painter, Vector2 center, Color color)
        {
            painter.BeginPath();
            for (int i = 0; i < 8; i++)
            {
                float angle = i * Mathf.PI * .25f;
                Vector2 point = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * 4f;
                if (i == 0) painter.MoveTo(point);
                else painter.LineTo(point);
            }
            painter.ClosePath();
            color.a = .9f;
            painter.fillColor = color;
            painter.Fill();
        }

        private int SkillRevealDepth(TDSkillNodeDefinition node)
        {
            if (game.Progression.IsUnlocked(node.Id)) return 0;
            int depth = 1;
            string prerequisite = node.PrerequisiteId;
            while (!string.IsNullOrEmpty(prerequisite) && depth < 4)
            {
                if (game.Progression.IsUnlocked(prerequisite)) break;
                depth++;
                TDSkillNodeDefinition parent = null;
                for (int i = 0; i < skillNodes.Count; i++)
                    if (skillNodes[i].Id == prerequisite) { parent = skillNodes[i]; break; }
                if (parent == null) break;
                prerequisite = parent.PrerequisiteId;
            }
            return depth;
        }

        private static string SkillGlyph(TDSkillEffect effect)
        {
            switch (effect)
            {
                case TDSkillEffect.ArcherDamage: return "⌁";
                case TDSkillEffect.ArcherRange: return "◎";
                case TDSkillEffect.HeroDamage: return "⚔";
                case TDSkillEffect.ArcaneDamage: return "✦";
                case TDSkillEffect.ArcaneFireInterval: return "✧";
                case TDSkillEffect.StartingGold: return "¤";
                case TDSkillEffect.TowerCost: return "◇";
                case TDSkillEffect.StartingLives: return "♥";
                case TDSkillEffect.HeroCriticalChance: return "✶";
                case TDSkillEffect.HeroCooldown: return "⌛";
                case TDSkillEffect.HeroRange: return "◉";
                case TDSkillEffect.MegaRadius: return "✹";
                case TDSkillEffect.HeavyDamage: return "◆";
                default: return "✦";
            }
        }

        private void AddSkillAura(Vector2 center, Color color, int seed)
        {
            if (skillGlowTexture == null)
            {
                const int size = 64;
                skillGlowTexture = new Texture2D(size, size, TextureFormat.RGBA32, false);
                skillGlowTexture.name = "Skill Node Glow";
                skillGlowTexture.hideFlags = HideFlags.HideAndDontSave;
                var pixels = new Color[size * size];
                for (int y = 0; y < size; y++)
                    for (int x = 0; x < size; x++)
                    {
                        float distance = Vector2.Distance(new Vector2(x, y), new Vector2(31.5f, 31.5f)) / 31.5f;
                        float alpha = Mathf.Pow(Mathf.Clamp01(1f - distance), 2f) * .85f;
                        pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
                    }
                skillGlowTexture.SetPixels(pixels);
                skillGlowTexture.Apply(false, true);
            }
            Image halo = new Image { image = skillGlowTexture, tintColor = color, scaleMode = ScaleMode.StretchToFill };
            halo.AddToClassList("skill-node-aura");
            halo.pickingMode = PickingMode.Ignore;
            halo.style.left = center.x;
            halo.style.top = center.y;
            skillCanvas.Add(halo);
            var particles = new VisualElement[3];
            for (int i = 0; i < particles.Length; i++)
            {
                particles[i] = new VisualElement();
                particles[i].AddToClassList("skill-node-particle");
                particles[i].pickingMode = PickingMode.Ignore;
                particles[i].style.backgroundColor = color;
                skillCanvas.Add(particles[i]);
            }
            skillAuras.Add(new SkillNodeAura { halo = halo, particles = particles, center = center, phase = seed * .73f });
        }

        private void UpdateSkillEffects()
        {
            if (skillCanvas == null) return;
            float time = Time.unscaledTime;
            for (int i = 0; i < skillAuras.Count; i++)
            {
                SkillNodeAura aura = skillAuras[i];
                aura.halo.style.opacity = .58f + .2f * Mathf.Sin(time * 2.4f + aura.phase);
                for (int particleIndex = 0; particleIndex < aura.particles.Length; particleIndex++)
                {
                    float travel = Mathf.Repeat(time * .22f + particleIndex * .34f + aura.phase, 1f);
                    float angle = (particleIndex * 2.09f + aura.phase + time * .5f);
                    float radius = 22f + travel * 24f;
                    VisualElement particle = aura.particles[particleIndex];
                    particle.style.left = aura.center.x + Mathf.Cos(angle) * radius;
                    particle.style.top = aura.center.y + Mathf.Sin(angle) * radius - travel * 18f;
                    particle.style.opacity = Mathf.Sin(travel * Mathf.PI) * .9f;
                }
            }
            skillCanvas.MarkDirtyRepaint();
        }

        private void ShowSkillHover(TDSkillNodeDefinition node, VisualElement source, int depth)
        {
            if (skillHoverCard == null) return;
            skillHoverName.text = depth >= 3 ? "UNDISCOVERED" : node.DisplayName;
            skillHoverEffect.text = depth >= 3 ? "Progress along this path to reveal the next upgrade." : node.Description;
            skillHoverState.text = game.Progression.IsUnlocked(node.Id) ? "OWNED · POWER ACTIVE"
                : depth == 1 ? "AVAILABLE · " + node.Cost + " SKULLS" : "LOCKED · REVEAL THE PREVIOUS NODE";
            Vector2 anchor = skillGraph.WorldToLocal(source.worldBound.center);
            skillHoverCard.style.left = Mathf.Clamp(anchor.x + 20f, 8f, Mathf.Max(8f, skillGraph.contentRect.width - 256f));
            skillHoverCard.style.top = Mathf.Clamp(anchor.y - 18f, 8f, Mathf.Max(8f, skillGraph.contentRect.height - 110f));
            skillHoverCard.style.display = DisplayStyle.Flex;
        }

        private void HideSkillHover()
        {
            if (skillHoverCard != null) skillHoverCard.style.display = DisplayStyle.None;
        }

        private void RegisterSkillGraphInput()
        {
            if (skillGraph == null) return;
            skillGraph.RegisterCallback<WheelEvent>(evt =>
            {
                skillZoom = Mathf.Clamp(skillZoom * (evt.delta.y > 0f ? .9f : 1.1f), .38f, 1.5f);
                PositionSkillCanvas();
                evt.StopPropagation();
            });
            skillGraph.RegisterCallback<PointerDownEvent>(evt =>
            {
                if (evt.target != skillGraph && evt.target != skillCanvas) return;
                skillPanning = true;
                skillDragStart = evt.position;
                skillGraph.CapturePointer(evt.pointerId);
            });
            skillGraph.RegisterCallback<PointerMoveEvent>(evt =>
            {
                if (!skillPanning) return;
                skillPan += new Vector2(evt.position.x, evt.position.y) - skillDragStart;
                skillDragStart = evt.position;
                PositionSkillCanvas();
            });
            skillGraph.RegisterCallback<PointerUpEvent>(evt =>
            {
                if (!skillPanning) return;
                skillPanning = false;
                if (skillGraph.HasPointerCapture(evt.pointerId)) skillGraph.ReleasePointer(evt.pointerId);
            });
        }

        private void PositionSkillCanvas()
        {
            if (skillCanvas == null || skillGraph == null) return;
            skillCanvas.style.left = (skillGraph.contentRect.width - SkillCanvasWidth) * .5f + skillPan.x;
            skillCanvas.style.top = (skillGraph.contentRect.height - SkillCanvasHeight) * .5f + skillPan.y;
            skillCanvas.style.scale = new Scale(Vector2.one * skillZoom);
        }

        private void SelectSkill(TDSkillNodeDefinition node)
        {
            selectedSkillId = node.Id;
            skillDetailsTitle.text = node.DisplayName;
            skillDetailsCopy.text = "Permanent upgrade · applies at the start of your next run.";
            BuildSkillGraph();
        }

        private void RefreshSelectedSkill()
        {
            TDSkillNodeDefinition node = FindSelectedSkill();
            bool purchased = node != null && game.Progression.IsUnlocked(node.Id);
            bool canPurchase = node != null && game.Progression.CanUnlock(node.Id, skillNodes)
                && game.Progression.SkullBalance >= node.Cost;
            skillPurchaseButton.text = purchased ? "PURCHASED" : canPurchase ? "UNLOCK NODE" : "LOCKED / NEED SKULLS";
            skillPurchaseButton.SetEnabled(canPurchase);
            if (node == null)
            {
                skillDetailsCost.text = "SELECT A NODE";
                skillDetailsEffect.text = "Choose a glowing branch node to view its effect.";
                return;
            }
            skillDetailsTitle.text = node.DisplayName;
            skillDetailsCopy.text = "Permanent upgrade · applies at the start of your next run.";
            skillDetailsCost.text = purchased ? "PURCHASED" : "COST · " + node.Cost + (node.Cost == 1 ? " SKULL" : " SKULLS");
            skillDetailsEffect.text = "EFFECT · " + node.Description;
        }

        private TDSkillNodeDefinition FindSelectedSkill()
        {
            if (string.IsNullOrEmpty(selectedSkillId) || skillNodes == null) return null;
            for (int i = 0; i < skillNodes.Count; i++)
                if (skillNodes[i].Id == selectedSkillId) return skillNodes[i];
            return null;
        }

        private void RefreshSkillCurrency()
        {
            if (skillCurrency != null && game != null && game.Progression != null)
                skillCurrency.text = "☠  " + game.Progression.SkullBalance + " SKULLS  ·  FORTRESS PATHS";
        }

        private void PurchaseSelectedSkill()
        {
            if (game.Progression.TryPurchase(selectedSkillId, skillNodes)) BuildSkillGraph();
            else RefreshSelectedSkill();
        }

        private void RefreshStageSelect()
        {
            endlessStageButton.style.display = game.IsEndlessUnlocked ? DisplayStyle.Flex : DisplayStyle.None;
            endlessStageButton.SetEnabled(game.IsEndlessUnlocked);
        }

        private void StartGreenward(TDRunMode mode = TDRunMode.Normal)
        {
            ResumeGame();
            game.StartSoloStages(mode);
            ShowGameplay();
        }

        private void ContinueGreenward()
        {
            if (!game.ContinueSavedRun()) return;
            ResumeGame();
            ShowGameplay();
        }

        private void ToggleBuildWheel()
        {
            if (game.Phase != TDGamePhase.Build || game.State.gold < 40) return;
            buildWheelOverlay.style.display = DisplayStyle.Flex;
            wheelDragging = false;
            wheelSelected = false;
            wheelArcherSelected = false;
        }

        private void SelectArcaneTower()
        {
            CloseBuildWheel();
            game.BeginPlacementMode();
            RefreshGameplayValues();
        }

        private void SelectArcherTower()
        {
            CloseBuildWheel();
            game.BeginPlacementMode(useArcher: true);
            RefreshGameplayValues();
        }

        private void CloseBuildWheel()
        {
            wheelDragging = false;
            wheelSelected = false;
            wheelArcherSelected = false;
            if (buildWheelOverlay != null) buildWheelOverlay.style.display = DisplayStyle.None;
        }

        private void StartNextWave()
        {
            game.StartNextWave();
            RefreshGameplayValues();
        }

        private void PerformAttack(TDAttackType type = TDAttackType.Light)
        {
            if (!paused && game.Phase == TDGamePhase.Wave) game.AttackHero(type);
        }

        private void Replay()
        {
            ResumeGame();
            game.ReturnToMenu();
            game.StartSoloStages();
            ShowGameplay();
        }

        private void OpenStageSelectFromResult()
        {
            ResumeGame();
            game.ReturnToMenu();
            ShowStageSelect();
        }

        private void OpenMainMenuFromResult()
        {
            ResumeGame();
            game.ReturnToMenu();
            ShowMainMenu();
        }

        private void QuitGame()
        {
            if (!Application.isEditor) Application.Quit();
        }

        private void SetScreen(VisualElement visibleScreen, TDMenuScreen screen)
        {
            if (visibleScreen == null) return;
            for (int i = 0; i < screens.Count; i++)
                if (screens[i] != null) screens[i].RemoveFromClassList("is-visible");
            visibleScreen.AddToClassList("is-visible");
            visibleScreen.AddToClassList("is-entering");
            visibleScreen.schedule.Execute(() => visibleScreen.RemoveFromClassList("is-entering")).StartingIn(16);
            currentScreen = screen;
        }

        private void RefreshGameplayValues()
        {
            if (game.State == null || hudCoinsValue == null) return;
            stageValue.text = game.CurrentStageName;
            int gold = game.State.gold;
            if (displayedGold != int.MinValue && gold > displayedGold) coinPickupPulse = CoinPulseDuration;
            displayedGold = gold;
            hudCoinsValue.text = gold.ToString();
            hudManaValue.text = "100/100";
            hudHealthValue.text = game.State.lives + "/" + game.MaxLives;
            waveValue.text = "WAVE  " + game.CurrentWave;
            stageWaveValue.text = "WAVE " + game.CurrentWave;
            defeatedValue.text = "DEFEATED  " + game.State.defeated;
            phaseValue.text = game.Phase == TDGamePhase.Build ? "BUILD PHASE" : "WAVE PHASE";
            buildPanel.style.display = DisplayStyle.Flex;
            buildButton.SetEnabled(game.Phase == TDGamePhase.Build && game.State.gold >= game.TowerBuildCost);
            if (arcaneBuildOption != null) arcaneBuildOption.text = "✦ ARCANE WATCHTOWER · " + game.TowerBuildCost + " COINS";
            if (archerBuildOption != null) archerBuildOption.text = "⌁ ARCHER WATCHTOWER · " + game.TowerBuildCost + " COINS";
            UpdateAttackCard(TDAttackType.Light, hudAttackValue, lightCooldownRing, lightChargePips);
            UpdateAttackCard(TDAttackType.Heavy, hudHeavyValue, heavyCooldownRing, heavyChargePips);
            UpdateAttackCard(TDAttackType.Mega, hudMegaValue, megaCooldownRing, megaChargePips);
            attackButton.SetEnabled(game.Phase == TDGamePhase.Wave && !paused && game.AttackCooldownRemaining(TDAttackType.Light) <= 0f);
            heavyAttackButton.SetEnabled(game.Phase == TDGamePhase.Wave && !paused && game.AttackCooldownRemaining(TDAttackType.Heavy) <= 0f);
            megaAttackButton.SetEnabled(game.Phase == TDGamePhase.Wave && !paused && game.AttackCooldownRemaining(TDAttackType.Mega) <= 0f);
            if (game.Phase != TDGamePhase.Build || currentScreen != TDMenuScreen.Gameplay) CloseBuildWheel();
            statusValue.text = game.IsPlacementMode
                ? (game.IsPlacingArcher ? "PLACE ARCHER WATCHTOWER" : "PLACE ARCANE WATCHTOWER")
                : game.Phase == TDGamePhase.Build ? "PREPARE THE DEFENSES" : "HOLD THE LINE";
        }

        private void UpdateAttackCard(TDAttackType type, Label value, TDHudCooldownRing ring, VisualElement[] pips)
        {
            float remaining = game.AttackCooldownRemaining(type);
            float duration = Mathf.Max(0.01f, game.AttackCooldownDuration(type));
            bool ready = remaining <= 0f;
            float progress = game.Phase == TDGamePhase.Wave ? 1f - Mathf.Clamp01(remaining / duration)
                : type == TDAttackType.Light ? 1f : 0f;
            value.text = game.Phase != TDGamePhase.Wave ? "" : paused ? "PAUSED" : ready ? "" : remaining.ToString("0.0") + "s";
            if (ring != null) ring.SetProgress(progress);
            if (pips == null) return;
            int lit = Mathf.FloorToInt(progress * pips.Length + .001f);
            for (int i = 0; i < pips.Length; i++)
            {
                if (i < lit) pips[i].AddToClassList("is-charged");
                else pips[i].RemoveFromClassList("is-charged");
            }
        }
    }
}

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace TDAnnihilation
{
    public sealed class TDVerticalSliceHUD : MonoBehaviour
    {
        private TDVerticalSliceBootstrap game;
        private UIDocument document;
        private VisualElement root;
        private readonly List<VisualElement> screens = new List<VisualElement>();
        private readonly TDSkillTreeState skillState = new TDSkillTreeState();
        private IReadOnlyList<TDSkillNodeDefinition> skillNodes;
        private VisualElement mainMenuScreen;
        private VisualElement stageSelectScreen;
        private VisualElement skillTreeScreen;
        private VisualElement settingsScreen;
        private VisualElement gameplayScreen;
        private VisualElement resultScreen;
        private VisualElement buildPanel;
        private VisualElement skillGraph;
        private Label stageValue;
        private Label goldValue;
        private Label livesValue;
        private Label waveValue;
        private Label defeatedValue;
        private Label phaseValue;
        private Label statusValue;
        private Label resultTitle;
        private Label resultCopy;
        private Label skillDetailsTitle;
        private Label skillDetailsCopy;
        private TDMenuScreen currentScreen;
        private string selectedSkillId;

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
            StyleSheet style = Resources.Load<StyleSheet>("TDAnnihilation/UI/Greenward");
            if (style != null && !root.styleSheets.Contains(style)) root.styleSheets.Add(style);
            skillNodes = TDSkillTreeCatalog.CreateDefault();
            CacheElements();
            RegisterCallbacks();
            BuildSkillGraph();
            ShowMainMenu();
        }

        private void Update()
        {
            if (game == null || game.State == null || root == null) return;
            RefreshGameplayValues();
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
        }

        public void ShowStageSelect()
        {
            SetScreen(stageSelectScreen, TDMenuScreen.StageSelect);
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
            resultCopy.text = game.Phase == TDGamePhase.Victory
                ? "The valley holds. Enemies defeated: " + game.State.defeated
                : "The gate has fallen. Return to the stage and try again.";
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
            skillGraph = root.Q<VisualElement>("skill-graph");
            stageValue = root.Q<Label>("stage-value");
            goldValue = root.Q<Label>("gold-value");
            livesValue = root.Q<Label>("lives-value");
            waveValue = root.Q<Label>("wave-value");
            defeatedValue = root.Q<Label>("defeated-value");
            phaseValue = root.Q<Label>("phase-value");
            statusValue = root.Q<Label>("status-value");
            resultTitle = root.Q<Label>("result-title");
            resultCopy = root.Q<Label>("result-copy");
            skillDetailsTitle = root.Q<Label>("skill-details-title");
            skillDetailsCopy = root.Q<Label>("skill-details-copy");
            screens.Add(mainMenuScreen);
            screens.Add(stageSelectScreen);
            screens.Add(skillTreeScreen);
            screens.Add(settingsScreen);
            screens.Add(gameplayScreen);
            screens.Add(resultScreen);
        }

        private void RegisterCallbacks()
        {
            root.Q<Button>("play-button").clicked += ShowStageSelect;
            root.Q<Button>("skill-tree-button").clicked += ShowSkillTree;
            root.Q<Button>("settings-button").clicked += ShowSettings;
            root.Q<Button>("quit-button").clicked += QuitGame;
            root.Q<Button>("stage-greenward-button").clicked += StartGreenward;
            root.Q<Button>("stage-two-button").SetEnabled(false);
            root.Q<Button>("stage-three-button").SetEnabled(false);
            root.Q<Button>("stage-select-back-button").clicked += ShowMainMenu;
            root.Q<Button>("skill-tree-back-button").clicked += ShowMainMenu;
            root.Q<Button>("settings-back-button").clicked += ShowMainMenu;
            root.Q<Button>("placement-button").clicked += TogglePlacement;
            root.Q<Button>("wave-button").clicked += StartNextWave;
            root.Q<Button>("result-replay-button").clicked += Replay;
            root.Q<Button>("result-stage-select-button").clicked += OpenStageSelectFromResult;
            root.Q<Button>("result-menu-button").clicked += OpenMainMenuFromResult;
        }

        private void BuildSkillGraph()
        {
            if (skillGraph == null || skillNodes == null) return;
            skillGraph.Clear();
            foreach (TDSkillBranch branch in new[] { TDSkillBranch.Warden, TDSkillBranch.Arcana, TDSkillBranch.Bastion })
            {
                VisualElement column = new VisualElement();
                column.AddToClassList("skill-branch");
                Label branchTitle = new Label(branch.ToString().ToUpperInvariant());
                branchTitle.AddToClassList("skill-branch-title");
                column.Add(branchTitle);
                for (int i = 0; i < skillNodes.Count; i++)
                {
                    TDSkillNodeDefinition node = skillNodes[i];
                    if (node.Branch != branch) continue;
                    Button button = new Button(() => SelectSkill(node)) { text = node.DisplayName };
                    button.AddToClassList("skill-node");
                    if (skillState.IsUnlocked(node.Id)) button.AddToClassList("is-unlocked");
                    else if (skillState.CanUnlock(node.Id, skillNodes)) button.AddToClassList("is-available");
                    if (node.Id == selectedSkillId) button.AddToClassList("is-selected");
                    column.Add(button);
                }
                skillGraph.Add(column);
            }
        }

        private void SelectSkill(TDSkillNodeDefinition node)
        {
            selectedSkillId = node.Id;
            skillState.TryUnlock(node.Id, skillNodes);
            skillDetailsTitle.text = node.DisplayName;
            skillDetailsCopy.text = node.Description + (string.IsNullOrEmpty(node.PrerequisiteId)
                ? ""
                : " Requires " + node.PrerequisiteId + ".");
            BuildSkillGraph();
        }

        private void StartGreenward()
        {
            game.StartSoloStages();
            ShowGameplay();
        }

        private void TogglePlacement()
        {
            game.TogglePlacementMode();
            RefreshGameplayValues();
        }

        private void StartNextWave()
        {
            game.StartNextWave();
            RefreshGameplayValues();
        }

        private void Replay()
        {
            game.ReturnToMenu();
            game.StartSoloStages();
            ShowGameplay();
        }

        private void OpenStageSelectFromResult()
        {
            game.ReturnToMenu();
            ShowStageSelect();
        }

        private void OpenMainMenuFromResult()
        {
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
            currentScreen = screen;
        }

        private void RefreshGameplayValues()
        {
            if (game.State == null || goldValue == null) return;
            stageValue.text = game.CurrentStageName;
            goldValue.text = "GOLD  " + game.State.gold;
            livesValue.text = "LIVES  " + game.State.lives;
            waveValue.text = "WAVE  " + game.CurrentWave;
            defeatedValue.text = "DEFEATED  " + game.State.defeated;
            phaseValue.text = game.Phase.ToString().ToUpperInvariant();
            buildPanel.style.display = game.Phase == TDGamePhase.Build ? DisplayStyle.Flex : DisplayStyle.None;
            statusValue.text = game.IsPlacementMode
                ? "CHOOSE A VALID BUILD SITE"
                : game.Phase == TDGamePhase.Build ? "PREPARE THE DEFENSES" : "HOLD THE LINE";
        }
    }
}

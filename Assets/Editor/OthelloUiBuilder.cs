using Othello.Services;
using Othello.Unity;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace Othello.EditorTools
{
    /// <summary>
    /// One-shot Editor tool that finishes wiring the screen panels already
    /// present in the scene — MainMenu, PlayMode, Game, Shop, Customization,
    /// Settings — by creating the buttons, labels and sub-panels each
    /// screen script still expects and assigning them into its serialized
    /// fields.
    /// <para>
    /// Also fixes the two structural leftovers from before the
    /// screen-navigation refactor: the board being a direct child of the
    /// Canvas instead of the Game screen (so it would show on top of every
    /// screen, not just the match), and a "ShopButton" wired to a method
    /// (<c>ShopView.Show</c>) that no longer exists.
    /// </para>
    /// <para>
    /// Every button's click behavior is wired in code, in each screen's own
    /// <c>Awake</c> — this tool only creates GameObjects and assigns them
    /// to the right field, it never touches OnClick. Save or commit the
    /// scene before running, since the created objects are not undoable.
    /// Safe to run again afterward: each screen is skipped once its first
    /// expected child already exists, so a second run will not duplicate
    /// what the first one already built.
    /// </para>
    /// </summary>
    public static class OthelloUiBuilder
    {
        [MenuItem("Othello/Build Screen UI")]
        public static void BuildScreenUi()
        {
            var canvas = Object.FindFirstObjectByType<Canvas>();
            if (canvas == null)
            {
                Debug.LogError("Nenhum Canvas encontrado na cena.");
                return;
            }

            Transform canvasRoot = canvas.transform;

            GameObject systemsGO = GameObject.Find("GameController");
            if (systemsGO == null)
            {
                Debug.LogError("GameObject 'GameController' não encontrado na cena.");
                return;
            }

            var gameController = systemsGO.GetComponent<GameController>();
            var boardView = systemsGO.GetComponent<BoardView>();

            var turnTimer = systemsGO.GetComponent<TurnTimerController>();
            if (turnTimer == null)
            {
                turnTimer = systemsGO.AddComponent<TurnTimerController>();
            }
            SetField(gameController, "_turnTimer", turnTimer);

            // May still be null on a scene where MonetizationController
            // hasn't been added to this GameObject by hand yet — SetField
            // tolerates a null value, same as every other not-yet-wired
            // reference in this tool, so BuildGame is safe to call either way.
            var monetization = systemsGO.GetComponent<MonetizationController>();

            RemoveStrayShopButton(canvasRoot);
            ReparentBoardGrid(canvasRoot);

            BuildMainMenu(canvasRoot);
            BuildPlayMode(canvasRoot);
            BuildGame(canvasRoot, gameController, boardView, turnTimer, monetization);
            BuildShop(canvasRoot);
            BuildCustomization(canvasRoot);
            BuildSettings(canvasRoot);

            EditorSceneManager.MarkSceneDirty(canvas.gameObject.scene);
            Debug.Log("Othello UI: telas preenchidas com sucesso.");
        }

        /// <summary>
        /// Second pass, meant to run after <see cref="BuildScreenUi"/>: adds
        /// a <see cref="LocalizationController"/> next to the other system
        /// components, wires it into every screen that needs it, attaches a
        /// <see cref="LocalizedText"/> to every static button label so it
        /// tracks the chosen language, and builds the language picker on
        /// the Settings screen (new UI, not built by the first pass).
        /// Safe to run again — every step is skipped where it was already done.
        /// </summary>
        [MenuItem("Othello/Build Localization")]
        public static void BuildLocalization()
        {
            GameObject systemsGO = GameObject.Find("GameController");
            if (systemsGO == null)
            {
                Debug.LogError("GameObject 'GameController' não encontrado na cena.");
                return;
            }

            var localization = systemsGO.GetComponent<LocalizationController>();
            if (localization == null)
            {
                localization = systemsGO.AddComponent<LocalizationController>();
            }

            var canvas = Object.FindFirstObjectByType<Canvas>();
            if (canvas == null)
            {
                Debug.LogError("Nenhum Canvas encontrado na cena.");
                return;
            }
            Transform canvasRoot = canvas.transform;

            WireLocalizationField(systemsGO.GetComponent<PlayerSessionController>(), localization);

            Transform mainMenu = canvasRoot.Find("MainMenu");
            if (mainMenu != null)
            {
                WireLocalizationField(mainMenu.GetComponent<MainMenuScreen>(), localization);
                Localize(mainMenu, "PlayButton/Label", localization, LocalizationKey.MenuPlay);
                Localize(mainMenu, "ShopButton/Label", localization, LocalizationKey.MenuShop);
                Localize(mainMenu, "CustomizeButton/Label", localization, LocalizationKey.MenuCustomize);
                Localize(mainMenu, "RankingButton/Label", localization, LocalizationKey.MenuRanking);
                Localize(mainMenu, "SettingsButton/Label", localization, LocalizationKey.MenuSettings);
                Localize(mainMenu, "QuitButton/Label", localization, LocalizationKey.MenuQuit);
            }

            Transform playMode = canvasRoot.Find("PlayMode");
            if (playMode != null)
            {
                Localize(playMode, "ModePanel/LocalPvpButton/Label", localization, LocalizationKey.PlayModeLocalPvp);
                Localize(playMode, "ModePanel/OnlinePvpButton/Label", localization, LocalizationKey.PlayModeOnlinePvp);
                Localize(playMode, "ModePanel/VsAiButton/Label", localization, LocalizationKey.PlayModeVsAi);
                Localize(playMode, "ModePanel/BackButton/Label", localization, LocalizationKey.ButtonBack);
                Localize(playMode, "DifficultyPanel/EasyButton/Label", localization, LocalizationKey.DifficultyEasy);
                Localize(playMode, "DifficultyPanel/MediumButton/Label", localization, LocalizationKey.DifficultyMedium);
                Localize(playMode, "DifficultyPanel/HardButton/Label", localization, LocalizationKey.DifficultyHard);
                Localize(playMode, "DifficultyPanel/DifficultyBackButton/Label", localization, LocalizationKey.ButtonBack);
            }

            Transform game = canvasRoot.Find("Game");
            if (game != null)
            {
                WireLocalizationField(game.GetComponent<GameScreen>(), localization);
                // The "back to menu" button itself now lives inside
                // MatchResultPanel (see BuildMatchResultUi, a later pass)
                // and is localized there instead — nothing to do for it here.
            }

            Transform shop = canvasRoot.Find("Shop");
            if (shop != null)
            {
                WireLocalizationField(shop.GetComponent<ShopView>(), localization);
                Localize(shop, "CloseButton/Label", localization, LocalizationKey.ShopClose);
                Localize(shop, "PieceTabButton/Label", localization, LocalizationKey.CategoryPieceSkin);
                Localize(shop, "BoardTabButton/Label", localization, LocalizationKey.CategoryBoardSkin);
                Localize(shop, "AvatarTabButton/Label", localization, LocalizationKey.CategoryAvatar);
            }

            Transform customization = canvasRoot.Find("Customization");
            if (customization != null)
            {
                WireLocalizationField(customization.GetComponent<CustomizationScreen>(), localization);
                Localize(customization, "BackButton/Label", localization, LocalizationKey.ButtonBack);
                Localize(customization, "GoToShopButton/Label", localization, LocalizationKey.CustomizationGoToShop);
                Localize(customization, "PieceTabButton/Label", localization, LocalizationKey.CategoryPieceSkin);
                Localize(customization, "BoardTabButton/Label", localization, LocalizationKey.CategoryBoardSkin);
                Localize(customization, "AvatarTabButton/Label", localization, LocalizationKey.CategoryAvatar);
            }

            Transform settings = canvasRoot.Find("Settings");
            if (settings != null)
            {
                var settingsScript = settings.GetComponent<SettingsScreen>();
                WireLocalizationField(settingsScript, localization);
                Localize(settings, "BackButton/Label", localization, LocalizationKey.ButtonBack);
                BuildLanguagePicker(settings, settingsScript);
            }

            EditorSceneManager.MarkSceneDirty(canvas.gameObject.scene);
            Debug.Log("Othello UI: localização aplicada com sucesso.");
        }

        /// <summary>
        /// Third pass, meant to run after <see cref="BuildLocalization"/>:
        /// adds <see cref="OnlineMatchController"/> and
        /// <see cref="LeaderboardController"/> next to the other system
        /// components, builds the brand-new Ranking screen from scratch and
        /// registers it with <see cref="ScreenManager"/>, and adds the
        /// matchmaking "searching" sub-panel to Play Mode and the
        /// opponent/rating labels to the in-match HUD. Safe to run again —
        /// every step is skipped where it was already done.
        /// </summary>
        [MenuItem("Othello/Build Online and Ranking")]
        public static void BuildOnlineAndRanking()
        {
            var canvas = Object.FindFirstObjectByType<Canvas>();
            if (canvas == null)
            {
                Debug.LogError("Nenhum Canvas encontrado na cena.");
                return;
            }
            Transform canvasRoot = canvas.transform;

            GameObject systemsGO = GameObject.Find("GameController");
            if (systemsGO == null)
            {
                Debug.LogError("GameObject 'GameController' não encontrado na cena.");
                return;
            }

            var localization = systemsGO.GetComponent<LocalizationController>();
            if (localization == null)
            {
                Debug.LogError("LocalizationController não encontrado. Rode 'Othello/Build Localization' primeiro.");
                return;
            }

            var screenManager = Object.FindFirstObjectByType<ScreenManager>();
            if (screenManager == null)
            {
                Debug.LogError("Nenhum ScreenManager encontrado na cena.");
                return;
            }

            var playerSession = systemsGO.GetComponent<PlayerSessionController>();

            var onlineMatch = systemsGO.GetComponent<OnlineMatchController>();
            if (onlineMatch == null)
            {
                onlineMatch = systemsGO.AddComponent<OnlineMatchController>();
            }

            var leaderboard = systemsGO.GetComponent<LeaderboardController>();
            if (leaderboard == null)
            {
                leaderboard = systemsGO.AddComponent<LeaderboardController>();
            }

            BuildRankingScreen(canvasRoot, screenManager, playerSession, leaderboard, localization);
            BuildOnlineSearchUi(canvasRoot, onlineMatch, playerSession, localization);
            BuildOnlineMatchHud(canvasRoot, playerSession);

            // MainMenu's own rank badge (built earlier by BuildScreenUi,
            // before LeaderboardController existed) can only be wired to it
            // here, now that the component is guaranteed to exist.
            Transform mainMenu = canvasRoot.Find("MainMenu");
            if (mainMenu != null)
            {
                SetField(mainMenu.GetComponent<MainMenuScreen>(), "_leaderboard", leaderboard);
            }

            // Same deferred-wiring reason as MainMenu above: GameScreen's
            // player panels need a rank lookup, but LeaderboardController
            // doesn't exist yet when BuildScreenUi (which builds GameScreen)
            // runs.
            Transform game = canvasRoot.Find("Game");
            if (game != null)
            {
                SetField(game.GetComponent<GameScreen>(), "_leaderboard", leaderboard);
            }

            EditorSceneManager.MarkSceneDirty(canvas.gameObject.scene);
            Debug.Log("Othello UI: multiplayer online e ranking configurados com sucesso.");
        }

        /// <summary>
        /// Fourth pass, meant to run after <see cref="BuildOnlineAndRanking"/>:
        /// adds an "Achievements" button to the main menu, and builds the
        /// brand-new Achievements screen from scratch and registers it with
        /// <see cref="ScreenManager"/>. Safe to run again — every step is
        /// skipped where it was already done.
        /// </summary>
        [MenuItem("Othello/Build Achievements")]
        public static void BuildAchievements()
        {
            var canvas = Object.FindFirstObjectByType<Canvas>();
            if (canvas == null)
            {
                Debug.LogError("Nenhum Canvas encontrado na cena.");
                return;
            }
            Transform canvasRoot = canvas.transform;

            GameObject systemsGO = GameObject.Find("GameController");
            if (systemsGO == null)
            {
                Debug.LogError("GameObject 'GameController' não encontrado na cena.");
                return;
            }

            var localization = systemsGO.GetComponent<LocalizationController>();
            if (localization == null)
            {
                Debug.LogError("LocalizationController não encontrado. Rode 'Othello/Build Localization' primeiro.");
                return;
            }

            var screenManager = Object.FindFirstObjectByType<ScreenManager>();
            if (screenManager == null)
            {
                Debug.LogError("Nenhum ScreenManager encontrado na cena.");
                return;
            }

            var playerSession = systemsGO.GetComponent<PlayerSessionController>();

            BuildAchievementsMenuButton(canvasRoot, localization);
            BuildAchievementsScreen(canvasRoot, screenManager, playerSession, localization);

            EditorSceneManager.MarkSceneDirty(canvas.gameObject.scene);
            Debug.Log("Othello UI: tela de conquistas configurada com sucesso.");
        }

        private static void BuildAchievementsMenuButton(Transform canvasRoot, LocalizationController localization)
        {
            Transform mainMenu = canvasRoot.Find("MainMenu");
            if (mainMenu == null || mainMenu.Find("AchievementsButton") != null)
            {
                return;
            }

            var script = mainMenu.GetComponent<MainMenuScreen>();

            // Appended below the existing buttons instead of inserted between
            // Ranking and Settings, so this never has to move a button
            // Victor may already have repositioned by hand in the Editor.
            var achievementsButton = CreateButton(mainMenu, "AchievementsButton", "Conquistas", -700);

            SetField(script, "_achievementsButton", achievementsButton);
            Localize(mainMenu, "AchievementsButton/Label", localization, LocalizationKey.MenuAchievements);
        }

        private static void BuildAchievementsScreen(
            Transform canvasRoot, ScreenManager screenManager, PlayerSessionController playerSession, LocalizationController localization)
        {
            Transform existing = canvasRoot.Find("Achievements");
            GameObject achievementsGO;
            AchievementsScreen script;

            if (existing == null)
            {
                achievementsGO = new GameObject("Achievements", typeof(RectTransform), typeof(Image), typeof(AchievementsScreen));
                achievementsGO.transform.SetParent(canvasRoot, false);

                var rect = (RectTransform)achievementsGO.transform;
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
                achievementsGO.GetComponent<Image>().color = new Color(0.08f, 0.1f, 0.09f, 0.97f);

                script = achievementsGO.GetComponent<AchievementsScreen>();
                SetField(script, "_root", achievementsGO);

                CreateLabel(achievementsGO.transform, "TitleLabel", "Achievements", -40);
                Localize(achievementsGO.transform, "TitleLabel", localization, LocalizationKey.AchievementsTitle);
            }
            else
            {
                achievementsGO = existing.gameObject;
                script = achievementsGO.GetComponent<AchievementsScreen>();
            }

            AddToScreenManager(screenManager, script);

            TMP_Text statsLabel = FindOrCreateLabel(achievementsGO.transform, "StatsLabel", "Matches: 0 · Wins: 0 · Losses: 0 · Draws: 0 · Streak: 0 (best 0)", -90);

            Transform containerTransform = achievementsGO.transform.Find("Container");
            RectTransform container;
            if (containerTransform == null)
            {
                container = CreatePanel(achievementsGO.transform, "Container");
                container.anchorMin = new Vector2(0.1f, 0.15f);
                container.anchorMax = new Vector2(0.9f, 0.8f);
                container.offsetMin = Vector2.zero;
                container.offsetMax = Vector2.zero;
                container.gameObject.AddComponent<VerticalLayoutGroup>();
            }
            else
            {
                container = (RectTransform)containerTransform;
            }

            Transform backTransform = achievementsGO.transform.Find("BackButton");
            Button backButton;
            if (backTransform == null)
            {
                backButton = CreateButton(achievementsGO.transform, "BackButton", "Voltar", 0);
                var backRect = (RectTransform)backButton.transform;
                backRect.anchorMin = new Vector2(0f, 0f);
                backRect.anchorMax = new Vector2(0f, 0f);
                backRect.pivot = new Vector2(0f, 0f);
                backRect.anchoredPosition = new Vector2(50f, 50f);
                Localize(achievementsGO.transform, "BackButton/Label", localization, LocalizationKey.ButtonBack);
            }
            else
            {
                backButton = backTransform.GetComponent<Button>();
            }

            SetField(script, "_playerSession", playerSession);
            SetField(script, "_localization", localization);
            SetField(script, "_statsLabel", statsLabel);
            SetField(script, "_listContainer", container);
            SetField(script, "_backButton", backButton);
        }

        /// <summary>
        /// Fifth pass, meant to run after <see cref="BuildAchievements"/>:
        /// adds a display-name input field and a Save button to the
        /// Settings screen, so the player can rename themselves without any
        /// external service. Safe to run again — every step is skipped
        /// where it was already done.
        /// </summary>
        [MenuItem("Othello/Build Player Rename")]
        public static void BuildPlayerRename()
        {
            var canvas = Object.FindFirstObjectByType<Canvas>();
            if (canvas == null)
            {
                Debug.LogError("Nenhum Canvas encontrado na cena.");
                return;
            }
            Transform canvasRoot = canvas.transform;

            GameObject systemsGO = GameObject.Find("GameController");
            if (systemsGO == null)
            {
                Debug.LogError("GameObject 'GameController' não encontrado na cena.");
                return;
            }

            var localization = systemsGO.GetComponent<LocalizationController>();
            if (localization == null)
            {
                Debug.LogError("LocalizationController não encontrado. Rode 'Othello/Build Localization' primeiro.");
                return;
            }

            var playerSession = systemsGO.GetComponent<PlayerSessionController>();

            Transform settings = canvasRoot.Find("Settings");
            if (settings == null)
            {
                Debug.LogError("Tela 'Settings' não encontrada na cena.");
                return;
            }

            var script = settings.GetComponent<SettingsScreen>();
            SetField(script, "_playerSession", playerSession);

            if (settings.Find("DisplayNameInput") == null)
            {
                var inputField = CreateInputField(settings, "DisplayNameInput", -400);
                var saveButton = CreateButton(settings, "SaveNameButton", "Salvar Nome", -470);

                SetField(script, "_displayNameInput", inputField);
                SetField(script, "_saveNameButton", saveButton);
                Localize(settings, "SaveNameButton/Label", localization, LocalizationKey.SettingsSaveName);
            }

            EditorSceneManager.MarkSceneDirty(canvas.gameObject.scene);
            Debug.Log("Othello UI: campo de renomear jogador configurado com sucesso.");
        }

        /// <summary>
        /// Sixth pass, meant to run after <see cref="BuildPlayerRename"/> and
        /// after the placeholder art under Assets/Resources/Art has been
        /// added to the project: reskins every button already built by the
        /// earlier passes with the shared button sprite (new buttons built
        /// from now on pick it up automatically, via <see cref="CreateButton"/>),
        /// adds the avatar and currency icons to the main menu, and applies
        /// the panel background to the Ranking and Achievements screens.
        /// Safe to run again — every step just reapplies the same sprites.
        /// </summary>
        [MenuItem("Othello/Build Art Assets")]
        public static void BuildArtAssets()
        {
            var canvas = Object.FindFirstObjectByType<Canvas>();
            if (canvas == null)
            {
                Debug.LogError("Nenhum Canvas encontrado na cena.");
                return;
            }
            Transform canvasRoot = canvas.transform;

            Sprite buttonSprite = LoadArtSprite("UI/button");
            if (buttonSprite == null)
            {
                Debug.LogWarning(
                    "Othello UI: 'Assets/Resources/Art/UI/button.png' não encontrado — rode este passo de novo depois de importar os assets.");
            }
            else
            {
                ReskinExistingButtons(canvasRoot, buttonSprite);
            }

            Sprite panelBackground = LoadArtSprite("UI/panel_background");
            ApplyPanelBackground(canvasRoot.Find("Ranking"), panelBackground);
            ApplyPanelBackground(canvasRoot.Find("Achievements"), panelBackground);
            ApplyPanelBackground(canvasRoot.Find("DailyReward"), panelBackground);

            WireMainMenuArt(canvasRoot.Find("MainMenu"));

            EditorSceneManager.MarkSceneDirty(canvas.gameObject.scene);
            Debug.Log("Othello UI: assets de arte aplicados com sucesso.");
        }

        /// <summary>
        /// Seventh pass, meant to run after <see cref="BuildArtAssets"/>:
        /// adds a "Daily Reward" button to the main menu, and builds the
        /// brand-new Daily Reward screen from scratch and registers it with
        /// <see cref="ScreenManager"/>. Run "Build Art Assets" again
        /// afterward to reskin this screen's new buttons and background —
        /// this pass only creates them, matching how every earlier
        /// screen-building pass leaves art to the dedicated pass. Safe to
        /// run again — every step is skipped where it was already done.
        /// </summary>
        [MenuItem("Othello/Build Daily Reward")]
        public static void BuildDailyReward()
        {
            var canvas = Object.FindFirstObjectByType<Canvas>();
            if (canvas == null)
            {
                Debug.LogError("Nenhum Canvas encontrado na cena.");
                return;
            }
            Transform canvasRoot = canvas.transform;

            GameObject systemsGO = GameObject.Find("GameController");
            if (systemsGO == null)
            {
                Debug.LogError("GameObject 'GameController' não encontrado na cena.");
                return;
            }

            var localization = systemsGO.GetComponent<LocalizationController>();
            if (localization == null)
            {
                Debug.LogError("LocalizationController não encontrado. Rode 'Othello/Build Localization' primeiro.");
                return;
            }

            var screenManager = Object.FindFirstObjectByType<ScreenManager>();
            if (screenManager == null)
            {
                Debug.LogError("Nenhum ScreenManager encontrado na cena.");
                return;
            }

            var playerSession = systemsGO.GetComponent<PlayerSessionController>();

            BuildDailyRewardMenuButton(canvasRoot, localization);
            BuildDailyRewardScreen(canvasRoot, screenManager, playerSession, localization);

            EditorSceneManager.MarkSceneDirty(canvas.gameObject.scene);
            Debug.Log("Othello UI: tela de recompensa diária configurada com sucesso.");
        }

        private static void BuildDailyRewardMenuButton(Transform canvasRoot, LocalizationController localization)
        {
            Transform mainMenu = canvasRoot.Find("MainMenu");
            if (mainMenu == null || mainMenu.Find("DailyRewardButton") != null)
            {
                return;
            }

            var script = mainMenu.GetComponent<MainMenuScreen>();

            // Appended below the Achievements button, for the same reason
            // that one was appended below Ranking: never move a button
            // Victor may already have repositioned by hand in the Editor.
            var dailyRewardButton = CreateButton(mainMenu, "DailyRewardButton", "Recompensa Diária", -770);

            SetField(script, "_dailyRewardButton", dailyRewardButton);
            Localize(mainMenu, "DailyRewardButton/Label", localization, LocalizationKey.DailyRewardTitle);
        }

        private static void BuildDailyRewardScreen(
            Transform canvasRoot, ScreenManager screenManager, PlayerSessionController playerSession, LocalizationController localization)
        {
            Transform existing = canvasRoot.Find("DailyReward");
            GameObject dailyRewardGO;
            DailyRewardScreen script;

            if (existing == null)
            {
                dailyRewardGO = new GameObject("DailyReward", typeof(RectTransform), typeof(Image), typeof(DailyRewardScreen));
                dailyRewardGO.transform.SetParent(canvasRoot, false);

                var rect = (RectTransform)dailyRewardGO.transform;
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
                dailyRewardGO.GetComponent<Image>().color = new Color(0.08f, 0.1f, 0.09f, 0.97f);

                script = dailyRewardGO.GetComponent<DailyRewardScreen>();
                SetField(script, "_root", dailyRewardGO);

                CreateLabel(dailyRewardGO.transform, "TitleLabel", "Daily Reward", -40);
                Localize(dailyRewardGO.transform, "TitleLabel", localization, LocalizationKey.DailyRewardTitle);
            }
            else
            {
                dailyRewardGO = existing.gameObject;
                script = dailyRewardGO.GetComponent<DailyRewardScreen>();
            }

            AddToScreenManager(screenManager, script);

            TMP_Text streakLabel = FindOrCreateLabel(dailyRewardGO.transform, "StreakLabel", "Day 1 streak", -140);
            TMP_Text previewLabel = FindOrCreateLabel(dailyRewardGO.transform, "PreviewLabel", "+20 gems", -190);

            Transform claimTransform = dailyRewardGO.transform.Find("ClaimButton");
            Button claimButton;
            if (claimTransform == null)
            {
                claimButton = CreateButton(dailyRewardGO.transform, "ClaimButton", "Claim", -280);
            }
            else
            {
                claimButton = claimTransform.GetComponent<Button>();
            }

            Transform backTransform = dailyRewardGO.transform.Find("BackButton");
            Button backButton;
            if (backTransform == null)
            {
                backButton = CreateButton(dailyRewardGO.transform, "BackButton", "Voltar", 0);
                var backRect = (RectTransform)backButton.transform;
                backRect.anchorMin = new Vector2(0f, 0f);
                backRect.anchorMax = new Vector2(0f, 0f);
                backRect.pivot = new Vector2(0f, 0f);
                backRect.anchoredPosition = new Vector2(50f, 50f);
                Localize(dailyRewardGO.transform, "BackButton/Label", localization, LocalizationKey.ButtonBack);
            }
            else
            {
                backButton = backTransform.GetComponent<Button>();
            }

            SetField(script, "_playerSession", playerSession);
            SetField(script, "_localization", localization);
            SetField(script, "_streakLabel", streakLabel);
            SetField(script, "_previewLabel", previewLabel);
            SetField(script, "_claimButton", claimButton);
            SetField(script, "_backButton", backButton);
        }

        private static void ReskinExistingButtons(Transform canvasRoot, Sprite buttonSprite)
        {
            foreach (var (screenName, path) in ButtonPaths)
            {
                Transform screen = canvasRoot.Find(screenName);
                Transform button = screen != null ? screen.Find(path) : null;
                Image image = button != null ? button.GetComponent<Image>() : null;
                if (image == null)
                {
                    continue;
                }

                image.sprite = buttonSprite;
                image.type = Image.Type.Sliced;
            }
        }

        private static void ApplyPanelBackground(Transform screenRoot, Sprite backgroundSprite)
        {
            if (screenRoot == null || backgroundSprite == null)
            {
                return;
            }

            var image = screenRoot.GetComponent<Image>();
            if (image == null)
            {
                return;
            }

            image.sprite = backgroundSprite;
            image.color = Color.white;
            image.type = Image.Type.Simple;
        }

        private static void WireMainMenuArt(Transform mainMenu)
        {
            if (mainMenu == null)
            {
                return;
            }

            var script = mainMenu.GetComponent<MainMenuScreen>();

            if (mainMenu.Find("AvatarImage") == null)
            {
                var avatarImage = CreateStaticIcon(
                    mainMenu, "AvatarImage", LoadArtSprite("Avatars/avatar_fox"),
                    new Vector2(0.5f, 1f), new Vector2(0f, -20f), new Vector2(90f, 90f));
                SetField(script, "_avatarImage", avatarImage);
            }

            if (mainMenu.Find("SoftCurrencyIcon") == null)
            {
                var softIcon = CreateStaticIcon(
                    mainMenu, "SoftCurrencyIcon", LoadArtSprite("UI/icon_soft_currency"),
                    new Vector2(0f, 1f), new Vector2(60f, -110f), new Vector2(40f, 40f));
                SetField(script, "_softCurrencyIcon", softIcon);
            }

            if (mainMenu.Find("HardCurrencyIcon") == null)
            {
                var hardIcon = CreateStaticIcon(
                    mainMenu, "HardCurrencyIcon", LoadArtSprite("UI/icon_hard_currency"),
                    new Vector2(0f, 1f), new Vector2(60f, -150f), new Vector2(40f, 40f));
                SetField(script, "_hardCurrencyIcon", hardIcon);
            }
        }

        private static Image CreateStaticIcon(
            Transform parent, string name, Sprite sprite, Vector2 anchor, Vector2 anchoredPosition, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);

            var rect = (RectTransform)go.transform;
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;

            var image = go.GetComponent<Image>();
            image.sprite = sprite;
            image.raycastTarget = false;
            return image;
        }

        private static Sprite LoadArtSprite(string relativePath) =>
            AssetDatabase.LoadAssetAtPath<Sprite>($"Assets/Resources/Art/{relativePath}.png");

        /// <summary>
        /// (screen root name, path from that root) for every button built by
        /// the earlier passes — the fixed list <see cref="ReskinExistingButtons"/>
        /// walks to apply the button sprite to buttons that already existed
        /// before this pass was added and so never got it from <see cref="CreateButton"/>.
        /// </summary>
        private static readonly (string screen, string path)[] ButtonPaths =
        {
            ("MainMenu", "PlayButton"),
            ("MainMenu", "ShopButton"),
            ("MainMenu", "CustomizeButton"),
            ("MainMenu", "RankingButton"),
            ("MainMenu", "AchievementsButton"),
            ("MainMenu", "DailyRewardButton"),
            ("MainMenu", "SettingsButton"),
            ("MainMenu", "QuitButton"),
            ("PlayMode", "ModePanel/LocalPvpButton"),
            ("PlayMode", "ModePanel/OnlinePvpButton"),
            ("PlayMode", "ModePanel/VsAiButton"),
            ("PlayMode", "ModePanel/BackButton"),
            ("PlayMode", "DifficultyPanel/EasyButton"),
            ("PlayMode", "DifficultyPanel/MediumButton"),
            ("PlayMode", "DifficultyPanel/HardButton"),
            ("PlayMode", "DifficultyPanel/DifficultyBackButton"),
            ("PlayMode", "SearchingPanel/CancelSearchButton"),
            ("Game", "MatchResultPanel/Card/CloseButton"),
            ("Shop", "CloseButton"),
            ("Customization", "BackButton"),
            ("Customization", "GoToShopButton"),
            ("Settings", "BackButton"),
            ("Settings", "LanguageButton"),
            ("Settings", "SaveNameButton"),
            ("Ranking", "BackButton"),
            ("Achievements", "BackButton"),
            ("DailyReward", "ClaimButton"),
            ("DailyReward", "BackButton"),
        };

        private static void BuildRankingScreen(
            Transform canvasRoot, ScreenManager screenManager, PlayerSessionController playerSession,
            LeaderboardController leaderboard, LocalizationController localization)
        {
            Transform existing = canvasRoot.Find("Ranking");
            GameObject rankingGO;
            RankingScreen script;

            if (existing == null)
            {
                rankingGO = new GameObject("Ranking", typeof(RectTransform), typeof(Image), typeof(RankingScreen));
                rankingGO.transform.SetParent(canvasRoot, false);

                var rect = (RectTransform)rankingGO.transform;
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
                rankingGO.GetComponent<Image>().color = new Color(0.08f, 0.1f, 0.09f, 0.97f);

                script = rankingGO.GetComponent<RankingScreen>();
                SetField(script, "_root", rankingGO);

                CreateLabel(rankingGO.transform, "TitleLabel", "Ranking", -40);
                Localize(rankingGO.transform, "TitleLabel", localization, LocalizationKey.RankingTitle);
            }
            else
            {
                rankingGO = existing.gameObject;
                script = rankingGO.GetComponent<RankingScreen>();
            }

            AddToScreenManager(screenManager, script);

            Transform ratingTransform = rankingGO.transform.Find("YourRatingLabel");
            TMP_Text ratingLabel = ratingTransform != null
                ? ratingTransform.GetComponent<TMP_Text>()
                : CreateLabel(rankingGO.transform, "YourRatingLabel", "Your rating: 1200", -90);

            Transform rankRowTransform = rankingGO.transform.Find("YourRankRow");
            Image rankIcon;
            TMP_Text rankLabel;
            if (rankRowTransform == null)
            {
                CreateIconLabelRow(rankingGO.transform, "YourRankRow", -140, out rankIcon, out rankLabel);
            }
            else
            {
                rankIcon = rankRowTransform.Find("Icon").GetComponent<Image>();
                rankLabel = rankRowTransform.Find("Label").GetComponent<TMP_Text>();
            }

            Transform containerTransform = rankingGO.transform.Find("Container");
            RectTransform container;
            if (containerTransform == null)
            {
                container = CreatePanel(rankingGO.transform, "Container");
                container.anchorMin = new Vector2(0.1f, 0.15f);
                container.anchorMax = new Vector2(0.9f, 0.8f);
                container.offsetMin = Vector2.zero;
                container.offsetMax = Vector2.zero;
            }
            else
            {
                container = (RectTransform)containerTransform;
            }

            // Reasserted every run rather than only at creation — this is
            // the exact same "unconfigured VerticalLayoutGroup" bug that
            // Shop and Customization had, fixed here before Victor ever
            // needs to notice it.
            var containerLayout = container.GetComponent<VerticalLayoutGroup>();
            if (containerLayout == null)
            {
                containerLayout = container.gameObject.AddComponent<VerticalLayoutGroup>();
            }
            containerLayout.childControlWidth = true;
            containerLayout.childControlHeight = true;
            containerLayout.childForceExpandWidth = true;
            containerLayout.childForceExpandHeight = false;
            containerLayout.spacing = 6f;

            Transform backTransform = rankingGO.transform.Find("BackButton");
            Button backButton;
            if (backTransform == null)
            {
                backButton = CreateButton(rankingGO.transform, "BackButton", "Voltar", 0);
                var backRect = (RectTransform)backButton.transform;
                backRect.anchorMin = new Vector2(0f, 0f);
                backRect.anchorMax = new Vector2(0f, 0f);
                backRect.pivot = new Vector2(0f, 0f);
                backRect.anchoredPosition = new Vector2(50f, 50f);
                Localize(rankingGO.transform, "BackButton/Label", localization, LocalizationKey.ButtonBack);
            }
            else
            {
                backButton = backTransform.GetComponent<Button>();
            }

            SetField(script, "_playerSession", playerSession);
            SetField(script, "_leaderboard", leaderboard);
            SetField(script, "_localization", localization);
            SetField(script, "_listContainer", container);
            SetField(script, "_yourRatingLabel", ratingLabel);
            SetField(script, "_yourRankIcon", rankIcon);
            SetField(script, "_yourRankLabel", rankLabel);
            SetField(script, "_backButton", backButton);
        }

        private static void BuildOnlineSearchUi(
            Transform canvasRoot, OnlineMatchController onlineMatch, PlayerSessionController playerSession, LocalizationController localization)
        {
            Transform playMode = canvasRoot.Find("PlayMode");
            if (playMode == null)
            {
                return;
            }

            var script = playMode.GetComponent<PlayModeScreen>();
            SetField(script, "_onlineMatch", onlineMatch);
            SetField(script, "_playerSession", playerSession);
            SetField(script, "_localization", localization);

            // Split into two independently-skippable steps: SearchingTipLabel
            // was added after SearchingPanel already existed in real
            // projects, so a re-run must be able to add just that without
            // requiring the whole panel to be deleted first.
            if (playMode.Find("SearchingPanel") == null)
            {
                RectTransform searchingPanel = CreatePanel(playMode, "SearchingPanel");
                CreateLabel(searchingPanel, "SearchingLabel", "Searching for an opponent...", -140);
                var cancelButton = CreateButton(searchingPanel, "CancelSearchButton", "Cancelar", -260);

                Localize(playMode, "SearchingPanel/SearchingLabel", localization, LocalizationKey.MatchmakingSearching);
                Localize(playMode, "SearchingPanel/CancelSearchButton/Label", localization, LocalizationKey.MatchmakingCancel);

                SetField(script, "_searchingPanel", searchingPanel.gameObject);
                SetField(script, "_cancelSearchButton", cancelButton);
            }

            if (playMode.Find("SearchingPanel/SearchingTipLabel") == null)
            {
                Transform searchingPanelTransform = playMode.Find("SearchingPanel");
                if (searchingPanelTransform != null)
                {
                    var tipLabel = CreateLabel(searchingPanelTransform, "SearchingTipLabel", string.Empty, -340);
                    tipLabel.fontSize = 22f;
                    SetField(script, "_searchingTipLabel", tipLabel);
                }
            }
        }

        private static void BuildOnlineMatchHud(Transform canvasRoot, PlayerSessionController playerSession)
        {
            Transform game = canvasRoot.Find("Game");
            if (game == null)
            {
                return;
            }

            var script = game.GetComponent<GameScreen>();
            SetField(script, "_playerSession", playerSession);

            Transform hud = game.Find("Hud");
            if (hud == null)
            {
                return;
            }

            // Fifth line in the compact Hud text block built by BuildGame —
            // repositioned every run (not just created once) so a layout
            // change here actually moves it even on a scene that already has
            // it from an earlier run. RatingChangeLabel and
            // AchievementToastLabel used to live here too; the former now
            // lives inside MatchResultPanel (see BuildMatchResultUi) since
            // it's only ever relevant right when a match ends, and the
            // latter moved into BuildGame itself as a floating toast — both
            // were cramped into this same 5-line Hud otherwise.
            TMP_Text opponentLabel = FindOrCreateLabel(hud, "OpponentLabel", "vs Opponent (Rating 1200)", -158);
            opponentLabel.fontSize = 20f;
            SetField(script, "_opponentLabel", opponentLabel);
        }

        private static TMP_Text FindOrCreateLabel(Transform parent, string name, string placeholderText, float y)
        {
            Transform existing = parent.Find(name);
            return existing != null ? existing.GetComponent<TMP_Text>() : CreateLabel(parent, name, placeholderText, y);
        }

        /// <summary>Destroys <paramref name="name"/>'s direct child of <paramref name="parent"/>, if any. No-op otherwise.</summary>
        private static void DestroyStaleChild(Transform parent, string name)
        {
            Transform child = parent.Find(name);
            if (child != null)
            {
                Object.DestroyImmediate(child.gameObject);
            }
        }

        /// <summary>
        /// Keeps only the first direct child of <paramref name="parent"/>
        /// named <paramref name="name"/> — the one <see cref="Transform.Find"/>
        /// (and so <see cref="FindOrCreateLabel"/>) would return — and
        /// destroys any later sibling with the same name. Guards against the
        /// exact failure mode that produced the duplicate ModeLabel/TimerLabel
        /// pair in Victor's scene: an earlier run's label left in place while
        /// a later run's RepositionLabelChild call only ever finds and moves
        /// one of the two, leaving the other sitting wherever it was built.
        /// </summary>
        private static void DestroyDuplicateChildren(Transform parent, string name)
        {
            var matches = new System.Collections.Generic.List<Transform>();
            for (int i = 0; i < parent.childCount; i++)
            {
                if (parent.GetChild(i).name == name)
                {
                    matches.Add(parent.GetChild(i));
                }
            }

            for (int i = 1; i < matches.Count; i++)
            {
                Object.DestroyImmediate(matches[i].gameObject);
            }
        }

        /// <summary>
        /// Resets a label built by <see cref="FindOrCreateLabel"/> back to
        /// its standard top-anchored layout and reapplies its Y offset and
        /// font size every run — unlike <see cref="FindOrCreateLabel"/>
        /// alone, which leaves an already-existing label exactly where it
        /// was. Needed wherever a label's parent panel changed size or
        /// position (like <see cref="BuildGame"/>'s Hud band) and the
        /// label's own offset has to move along with it.
        /// </summary>
        private static TMP_Text RepositionLabelChild(Transform parent, string name, string placeholderText, float y, float fontSize)
        {
            TMP_Text label = FindOrCreateLabel(parent, name, placeholderText, y);
            var rect = (RectTransform)label.transform;
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, y);
            label.fontSize = fontSize;
            return label;
        }

        /// <summary>
        /// The row-layout equivalent of <see cref="RepositionLabelChild"/>,
        /// for a row built by <see cref="CreateIconLabelRow"/> (an icon next
        /// to a label, not a bare label) — used to slide
        /// <see cref="PlayerProfilePanel"/>'s rank and piece-color rows to
        /// their compacted offsets in <see cref="RepositionPlayerProfilePanel"/>.
        /// No-ops if <paramref name="name"/> doesn't exist yet — every
        /// caller only repositions rows <see cref="BuildPlayerProfilePanel"/>
        /// already created.
        /// </summary>
        private static void RepositionRowChild(Transform parent, string name, float y)
        {
            Transform row = parent.Find(name);
            if (row == null)
            {
                return;
            }

            var rect = (RectTransform)row;
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, y);
        }

        private static RectTransform FindOrCreatePanel(Transform parent, string name)
        {
            Transform existing = parent.Find(name);
            return existing != null ? (RectTransform)existing : CreatePanel(parent, name);
        }

        private static Button FindOrCreateButton(Transform parent, string name, string label)
        {
            Transform existing = parent.Find(name);
            return existing != null ? existing.GetComponent<Button>() : CreateButton(parent, name, label, 0f);
        }

        private static PlayerProfilePanel FindOrCreatePlayerProfilePanel(Transform parent, string name)
        {
            Transform existing = parent.Find(name);
            if (existing != null)
            {
                return existing.GetComponent<PlayerProfilePanel>();
            }

            // The anchors passed here are throwaway — every caller follows
            // this immediately with RepositionPlayerProfilePanel, which
            // sets the real ones, on both a freshly created panel and one
            // found already existing.
            return BuildPlayerProfilePanel(parent, name, Vector2.zero, Vector2.one);
        }

        /// <summary>
        /// Moves an already-built <see cref="PlayerProfilePanel"/> to a new
        /// anchor rect and compacts its internal children to fit a shorter
        /// panel. <see cref="BuildPlayerProfilePanel"/>'s children are laid
        /// out with fixed pixel Y-offsets from the panel's own top edge, so
        /// shrinking only the outer rect (without also shrinking those
        /// offsets) would leave content overflowing past the new, shorter
        /// rect's bottom edge — this reapplies both together, every run.
        /// </summary>
        private static void RepositionPlayerProfilePanel(PlayerProfilePanel panel, Vector2 anchorMin, Vector2 anchorMax)
        {
            var rect = (RectTransform)panel.transform;
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            Transform avatar = panel.transform.Find("Avatar");
            if (avatar != null)
            {
                var avatarRect = (RectTransform)avatar;
                avatarRect.anchorMin = new Vector2(0.5f, 1f);
                avatarRect.anchorMax = new Vector2(0.5f, 1f);
                avatarRect.pivot = new Vector2(0.5f, 1f);
                avatarRect.anchoredPosition = Vector2.zero;
                avatarRect.sizeDelta = new Vector2(56f, 56f);
            }

            RepositionLabelChild(panel.transform, "NameLabel", "Player", -60f, 22f);
            RepositionRowChild(panel.transform, "RankRow", -104f);
            RepositionRowChild(panel.transform, "PieceColorRow", -148f);
            RepositionLabelChild(panel.transform, "ExtraInfoLabel", string.Empty, -192f, 18f);
        }

        private static void AddToScreenManager(ScreenManager screenManager, UIScreen screen)
        {
            var serialized = new SerializedObject(screenManager);
            SerializedProperty screensProperty = serialized.FindProperty("_screens");
            if (screensProperty == null)
            {
                Debug.LogError("Campo '_screens' não encontrado em ScreenManager.");
                return;
            }

            for (int i = 0; i < screensProperty.arraySize; i++)
            {
                if (screensProperty.GetArrayElementAtIndex(i).objectReferenceValue == screen)
                {
                    return;
                }
            }

            int newIndex = screensProperty.arraySize;
            screensProperty.arraySize++;
            screensProperty.GetArrayElementAtIndex(newIndex).objectReferenceValue = screen;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void BuildLanguagePicker(Transform settingsRoot, SettingsScreen script)
        {
            if (settingsRoot.Find("LanguageButton") != null)
            {
                return;
            }

            var languageButton = CreateButton(settingsRoot, "LanguageButton", "Language: English", -320);
            var valueLabel = languageButton.GetComponentInChildren<TMP_Text>();

            SetField(script, "_languageButton", languageButton);
            SetField(script, "_languageValueLabel", valueLabel);
        }

        private static void Localize(Transform root, string path, LocalizationController localization, LocalizationKey key)
        {
            Transform label = root.Find(path);
            if (label == null)
            {
                Debug.LogWarning($"Localize: '{path}' não encontrado sob '{root.name}'.");
                return;
            }

            var localizedText = label.GetComponent<LocalizedText>();
            if (localizedText == null)
            {
                localizedText = label.gameObject.AddComponent<LocalizedText>();
            }

            SetField(localizedText, "_localization", localization);
            SetEnumField(localizedText, "_key", key);
        }

        private static void WireLocalizationField(Object target, LocalizationController localization)
        {
            if (target != null)
            {
                SetField(target, "_localization", localization);
            }
        }

        private static void RemoveStrayShopButton(Transform canvasRoot)
        {
            Transform stray = canvasRoot.Find("ShopButton");
            if (stray != null)
            {
                Object.DestroyImmediate(stray.gameObject);
            }
        }

        /// <summary>
        /// Anchors the board to its play area — below the top profile row
        /// and compact Hud band built by <see cref="BuildGame"/>, above the
        /// bottom button zone — and frames it with a <c>BoardBorder</c> so
        /// it never sits flush against the device's screen edges. The
        /// reparent itself only needs to happen once, but the anchors and
        /// border are reapplied every run so a layout change here actually
        /// moves an already-built scene's board instead of silently no-oping.
        /// </summary>
        private static void ReparentBoardGrid(Transform canvasRoot)
        {
            Transform game = canvasRoot.Find("Game");

            // Transform.Find(name) only searches direct children, not the
            // whole hierarchy — once a previous run has already reparented
            // BoardGrid under Game, canvasRoot.Find("BoardGrid") stops
            // finding it, so every run after the first used to return here
            // immediately and skip the anchor/fitter setup below entirely,
            // no matter how that setup changed.
            Transform boardGrid = canvasRoot.Find("BoardGrid");
            if (boardGrid == null && game != null)
            {
                boardGrid = game.Find("BoardGrid");
            }

            if (boardGrid == null || game == null)
            {
                return;
            }

            if (boardGrid.parent != game)
            {
                boardGrid.SetParent(game, false);
            }

            var rect = (RectTransform)boardGrid;
            rect.anchorMin = new Vector2(0.07f, 0.16f);
            rect.anchorMax = new Vector2(0.93f, 0.68f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = Vector2.zero;

            // The grid's cell size (set by hand in the Inspector on
            // whichever screen BoardGrid was last tuned for) does not shrink
            // on its own when the RectTransform above is resized — a fixed
            // GridLayoutGroup cell size just overflows its new, smaller
            // bounds, which is what made the board paint over the Hud and
            // player panels above it. ResponsiveGridLayout (an existing,
            // previously-unused component) fixes that by recomputing the
            // cell size from BoardGrid's own rect whenever it changes.
            // Deliberately NOT paired with an AspectRatioFitter here, even
            // though that's the component's own suggested pairing: in
            // FitInParent mode, AspectRatioFitter forces its RectTransform's
            // anchors to fully stretch its parent and centers the square in
            // THAT full area — which would silently undo the anchorMin/Max
            // set just above and put the board back to covering the whole
            // Game screen. Centering within the smaller rect is done the
            // GridLayoutGroup's own way instead, below.
            var gridLayoutGroup = boardGrid.GetComponent<GridLayoutGroup>();
            if (gridLayoutGroup != null)
            {
                gridLayoutGroup.childAlignment = TextAnchor.MiddleCenter;
            }

            if (boardGrid.GetComponent<ResponsiveGridLayout>() == null)
            {
                boardGrid.gameObject.AddComponent<ResponsiveGridLayout>();
            }

            // Removes the AspectRatioFitter an earlier version of this
            // method added here — see the comment above for why it fights
            // the anchors instead of respecting them.
            var strayFitter = boardGrid.GetComponent<AspectRatioFitter>();
            if (strayFitter != null)
            {
                Object.DestroyImmediate(strayFitter);
            }

            Transform border = game.Find("BoardBorder");
            RectTransform borderRect;
            if (border == null)
            {
                var borderGO = new GameObject("BoardBorder", typeof(RectTransform), typeof(Image));
                borderGO.transform.SetParent(game, false);
                borderRect = (RectTransform)borderGO.transform;
                // Sibling index 0 within Game so it renders behind the
                // board grid (and behind everything else built into Game),
                // never intercepting board taps.
                borderRect.SetAsFirstSibling();
                var borderImage = borderGO.GetComponent<Image>();
                borderImage.color = new Color(0.08f, 0.08f, 0.1f, 1f);
                borderImage.raycastTarget = false;
            }
            else
            {
                borderRect = (RectTransform)border;
            }

            borderRect.anchorMin = rect.anchorMin;
            borderRect.anchorMax = rect.anchorMax;
            borderRect.offsetMin = new Vector2(-20f, -20f);
            borderRect.offsetMax = new Vector2(20f, 20f);
        }

        private static void BuildMainMenu(Transform canvasRoot)
        {
            Transform root = canvasRoot.Find("MainMenu");
            if (root == null)
            {
                return;
            }

            var script = root.GetComponent<MainMenuScreen>();

            if (root.Find("PlayerNameLabel") == null)
            {
                var nameLabel = CreateLabel(root, "PlayerNameLabel", "Jogador", -60);
                var softLabel = CreateLabel(root, "SoftBalanceLabel", "Gemas: 0", -110);
                var hardLabel = CreateLabel(root, "HardBalanceLabel", "Moeda premium: 0", -150);

                var playButton = CreateButton(root, "PlayButton", "Jogar", -220);
                var shopButton = CreateButton(root, "ShopButton", "Loja", -300);
                var customizeButton = CreateButton(root, "CustomizeButton", "Personalizar", -380);
                var rankingButton = CreateButton(root, "RankingButton", "Ranking (em breve)", -460);
                var settingsButton = CreateButton(root, "SettingsButton", "Configurações", -540);
                var quitButton = CreateButton(root, "QuitButton", "Sair", -620);

                SetField(script, "_playerNameLabel", nameLabel);
                SetField(script, "_softBalanceLabel", softLabel);
                SetField(script, "_hardBalanceLabel", hardLabel);
                SetField(script, "_playButton", playButton);
                SetField(script, "_shopButton", shopButton);
                SetField(script, "_customizeButton", customizeButton);
                SetField(script, "_rankingButton", rankingButton);
                SetField(script, "_settingsButton", settingsButton);
                SetField(script, "_quitButton", quitButton);
            }

            // Separate, independently-skippable step — added after the rest
            // of MainMenu already existed in real projects, same retrofit
            // approach as Shop's category tabs.
            if (root.Find("RankRow") == null)
            {
                CreateIconLabelRow(root, "RankRow", -190, out Image rankIcon, out TMP_Text rankLabel);
                SetField(script, "_rankIcon", rankIcon);
                SetField(script, "_rankLabel", rankLabel);
            }
        }

        private static void BuildPlayMode(Transform canvasRoot)
        {
            Transform root = canvasRoot.Find("PlayMode");
            if (root == null || root.Find("ModePanel") != null)
            {
                return;
            }

            var script = root.GetComponent<PlayModeScreen>();

            RectTransform modePanel = CreatePanel(root, "ModePanel");
            var localPvpButton = CreateButton(modePanel, "LocalPvpButton", "PvP Local", -100);
            var onlinePvpButton = CreateButton(modePanel, "OnlinePvpButton", "PvP Online (em breve)", -180);
            var vsAiButton = CreateButton(modePanel, "VsAiButton", "Contra a IA", -260);
            var backButton = CreateButton(modePanel, "BackButton", "Voltar", -340);

            RectTransform difficultyPanel = CreatePanel(root, "DifficultyPanel");
            var easyButton = CreateButton(difficultyPanel, "EasyButton", "Fácil", -100);
            var mediumButton = CreateButton(difficultyPanel, "MediumButton", "Médio", -180);
            var hardButton = CreateButton(difficultyPanel, "HardButton", "Difícil", -260);
            var difficultyBackButton = CreateButton(difficultyPanel, "DifficultyBackButton", "Voltar", -340);

            Transform gameScreenTransform = canvasRoot.Find("Game");
            var gameScreen = gameScreenTransform != null ? gameScreenTransform.GetComponent<GameScreen>() : null;

            SetField(script, "_modePanel", modePanel.gameObject);
            SetField(script, "_difficultyPanel", difficultyPanel.gameObject);
            SetField(script, "_localPvpButton", localPvpButton);
            SetField(script, "_onlinePvpButton", onlinePvpButton);
            SetField(script, "_vsAiButton", vsAiButton);
            SetField(script, "_backButton", backButton);
            SetField(script, "_easyButton", easyButton);
            SetField(script, "_mediumButton", mediumButton);
            SetField(script, "_hardButton", hardButton);
            SetField(script, "_difficultyBackButton", difficultyBackButton);
            SetField(script, "_gameScreen", gameScreen);
        }

        /// <summary>
        /// Lays out the Game screen: a compact profile row for both players
        /// across the top, a 5-line Hud band just below it, the board in
        /// the middle (anchored and bordered by <see cref="ReparentBoardGrid"/>),
        /// a single Resign button at the bottom, and a floating achievement
        /// toast just above that button. The profile panels used to sit
        /// beside the board instead of above it, which is what made them
        /// overlap the play area — moving them off to the side of the
        /// board's own footprint wasn't enough on its own, since the two
        /// used to occupy the same horizontal band.
        /// <para>
        /// Every element here is repositioned unconditionally, not just
        /// created — this needs to actually move things on Victor's
        /// already-built scene, not silently no-op because the object
        /// already exists from an earlier layout.
        /// </para>
        /// </summary>
        private static void BuildGame(
            Transform canvasRoot, GameController gameController, BoardView boardView,
            TurnTimerController turnTimer, MonetizationController monetization)
        {
            Transform root = canvasRoot.Find("Game");
            if (root == null)
            {
                return;
            }

            var script = root.GetComponent<GameScreen>();
            SetField(script, "_gameController", gameController);
            SetField(script, "_boardView", boardView);
            SetField(script, "_turnTimer", turnTimer);
            SetField(script, "_monetization", monetization);

            // Superseded by MatchResultPanel/Card/CloseButton (see
            // BuildMatchResultUi) — an already-built scene still has this
            // stray root-level object from before that move, and nothing
            // references it anymore, so left alone it would sit visible
            // and inert at the bottom of the screen forever.
            Transform strayBackButton = root.Find("BackToMenuButton");
            if (strayBackButton != null)
            {
                Object.DestroyImmediate(strayBackButton.gameObject);
            }

            RectTransform hud = FindOrCreatePanel(root, "Hud");
            hud.anchorMin = new Vector2(0f, 0.70f);
            hud.anchorMax = new Vector2(1f, 0.83f);
            hud.offsetMin = Vector2.zero;
            hud.offsetMax = Vector2.zero;

            // RatingChangeLabel and AchievementToastLabel used to live
            // directly under Hud before they moved to MatchResultPanel and
            // to a floating toast under Game (see the comment on
            // BuildOnlineMatchHud) — an already-built scene keeps the old
            // copies around forever since nothing looks them up by this
            // parent/name combination anymore. Left in place they sit well
            // below Hud's own band with no clipping, painting over the top
            // rows of the board and blocking clicks there. Destroying them
            // here, every run, means a stale scene self-heals instead of
            // accumulating more of these on each rebuild.
            DestroyStaleChild(hud, "RatingChangeLabel");
            DestroyStaleChild(hud, "AchievementToastLabel");
            DestroyDuplicateChildren(hud, "TurnLabel");
            DestroyDuplicateChildren(hud, "ScoreLabel");
            DestroyDuplicateChildren(hud, "ModeLabel");
            DestroyDuplicateChildren(hud, "TimerLabel");

            TMP_Text turnLabel = RepositionLabelChild(hud, "TurnLabel", "Vez de: Preto", -6, 22f);
            TMP_Text scoreLabel = RepositionLabelChild(hud, "ScoreLabel", "Preto 2 - 2 Branco", -42, 22f);
            TMP_Text modeLabel = RepositionLabelChild(hud, "ModeLabel", "Modo: PvP Local", -78, 20f);
            TMP_Text timerLabel = RepositionLabelChild(hud, "TimerLabel", "Tempo: 30s", -114, 20f);

            SetField(script, "_turnLabel", turnLabel);
            SetField(script, "_scoreLabel", scoreLabel);
            SetField(script, "_modeLabel", modeLabel);
            SetField(script, "_timerLabel", timerLabel);
            // OpponentLabel, the Hud's fifth line, is built by
            // BuildOnlineMatchHud instead — it needs the PlayerSessionController
            // that pass already receives, which this one doesn't.

            Button resignButton = FindOrCreateButton(root, "ResignButton", "Desistir");
            var resignRect = (RectTransform)resignButton.transform;
            resignRect.anchorMin = new Vector2(0.5f, 0f);
            resignRect.anchorMax = new Vector2(0.5f, 0f);
            resignRect.pivot = new Vector2(0.5f, 0f);
            resignRect.anchoredPosition = new Vector2(0f, 70f);
            resignRect.sizeDelta = new Vector2(340f, 70f);
            SetField(script, "_resignButton", resignButton);

            // A thin floating band just above the button zone — used to sit
            // inside the old Hud, but that band is compact now and has no
            // room for a variable-length toast on top of the five fixed lines.
            TMP_Text achievementToastLabel = FindOrCreateLabel(root, "AchievementToastLabel", string.Empty, 0f);
            var toastRect = (RectTransform)achievementToastLabel.transform;
            toastRect.anchorMin = new Vector2(0.05f, 0.165f);
            toastRect.anchorMax = new Vector2(0.95f, 0.20f);
            toastRect.pivot = new Vector2(0.5f, 0.5f);
            toastRect.anchoredPosition = Vector2.zero;
            toastRect.sizeDelta = Vector2.zero;
            achievementToastLabel.fontSize = 20f;
            achievementToastLabel.gameObject.SetActive(false);
            SetField(script, "_achievementToastLabel", achievementToastLabel);

            PlayerProfilePanel playerPanel = FindOrCreatePlayerProfilePanel(root, "PlayerPanel");
            RepositionPlayerProfilePanel(playerPanel, new Vector2(0.02f, 0.84f), new Vector2(0.49f, 0.98f));
            SetField(script, "_playerPanel", playerPanel);

            PlayerProfilePanel opponentPanel = FindOrCreatePlayerProfilePanel(root, "OpponentPanel");
            RepositionPlayerProfilePanel(opponentPanel, new Vector2(0.51f, 0.84f), new Vector2(0.98f, 0.98f));
            SetField(script, "_opponentPanel", opponentPanel);
        }

        /// <summary>
        /// Builds one <see cref="PlayerProfilePanel"/> hierarchy — avatar,
        /// name, rank row, piece-color row, extra-info line — anchored to a
        /// rectangle of <paramref name="parent"/>. Used twice by
        /// <see cref="BuildGame"/>: once anchored to the left third of the
        /// screen for the session player, once to the right third for the
        /// opponent.
        /// </summary>
        private static PlayerProfilePanel BuildPlayerProfilePanel(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);

            var rect = (RectTransform)go.transform;
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var avatarGO = new GameObject("Avatar", typeof(RectTransform), typeof(Image));
            avatarGO.transform.SetParent(go.transform, false);
            var avatarRect = (RectTransform)avatarGO.transform;
            avatarRect.anchorMin = new Vector2(0.5f, 1f);
            avatarRect.anchorMax = new Vector2(0.5f, 1f);
            avatarRect.pivot = new Vector2(0.5f, 1f);
            avatarRect.anchoredPosition = Vector2.zero;
            avatarRect.sizeDelta = new Vector2(80f, 80f);
            var avatarImage = avatarGO.GetComponent<Image>();
            avatarImage.enabled = false; // hidden until a real avatar sprite is assigned at runtime

            var nameLabel = CreateLabel(go.transform, "NameLabel", "Player", -90);
            nameLabel.fontSize = 24f;

            CreateIconLabelRow(go.transform, "RankRow", -140, out Image rankIcon, out TMP_Text rankLabel);
            CreateIconLabelRow(go.transform, "PieceColorRow", -190, out Image pieceColorIcon, out TMP_Text pieceColorLabel);

            var extraInfoLabel = CreateLabel(go.transform, "ExtraInfoLabel", string.Empty, -240);
            extraInfoLabel.fontSize = 22f;
            extraInfoLabel.gameObject.SetActive(false);

            var panel = go.AddComponent<PlayerProfilePanel>();
            SetField(panel, "_avatarImage", avatarImage);
            SetField(panel, "_nameLabel", nameLabel);
            SetField(panel, "_rankIcon", rankIcon);
            SetField(panel, "_rankLabel", rankLabel);
            SetField(panel, "_pieceColorIcon", pieceColorIcon);
            SetField(panel, "_pieceColorLabel", pieceColorLabel);
            SetField(panel, "_extraInfoLabel", extraInfoLabel);

            return panel;
        }

        private static void BuildShop(Transform canvasRoot)
        {
            Transform root = canvasRoot.Find("Shop");
            if (root == null)
            {
                return;
            }

            var script = root.GetComponent<ShopView>();

            // Split into two independently-skippable steps — unlike
            // Customization's Container, Shop's already existed in the
            // scene before this tool did, so an already-built Shop (has
            // CloseButton) still needs a chance to pick up the tabs added
            // later, without requiring anything to be deleted first.
            if (root.Find("CloseButton") == null)
            {
                Transform container = root.Find("Container");
                if (container != null)
                {
                    var rect = (RectTransform)container;
                    rect.anchorMin = new Vector2(0.05f, 0.1f);
                    rect.anchorMax = new Vector2(0.95f, 0.85f);
                    rect.anchoredPosition = Vector2.zero;
                    rect.sizeDelta = Vector2.zero;
                }

                var closeButton = CreateButton(root, "CloseButton", "Fechar", 0);
                var closeRect = (RectTransform)closeButton.transform;
                closeRect.anchorMin = new Vector2(1f, 1f);
                closeRect.anchorMax = new Vector2(1f, 1f);
                closeRect.pivot = new Vector2(1f, 1f);
                closeRect.anchoredPosition = new Vector2(-30f, -30f);
                closeRect.sizeDelta = new Vector2(160f, 60f);

                SetField(script, "_closeButton", closeButton);
            }

            if (root.Find("PieceTabButton") == null)
            {
                Transform container = root.Find("Container");
                if (container != null)
                {
                    // Shrinks the list to leave a strip for the tabs above
                    // it, same layout as Customization's Container.
                    var rect = (RectTransform)container;
                    rect.anchorMax = new Vector2(rect.anchorMax.x, 0.78f);

                    var containerLayout = container.GetComponent<VerticalLayoutGroup>();
                    if (containerLayout == null)
                    {
                        containerLayout = container.gameObject.AddComponent<VerticalLayoutGroup>();
                    }
                    // Manually-set-up scenes may already have one, possibly
                    // still misconfigured from before the Shop layout fix —
                    // reasserting these here means the checkboxes never
                    // need to be touched by hand again.
                    containerLayout.childControlWidth = true;
                    containerLayout.childControlHeight = true;
                    containerLayout.childForceExpandWidth = true;
                    containerLayout.childForceExpandHeight = false;
                    containerLayout.spacing = 8f;
                }

                var pieceTab = CreateTabButton(root, "PieceTabButton", "Peças", new Vector2(0.05f, 0.79f), new Vector2(0.37f, 0.85f));
                var boardTab = CreateTabButton(root, "BoardTabButton", "Tabuleiros", new Vector2(0.38f, 0.79f), new Vector2(0.70f, 0.85f));
                var avatarTab = CreateTabButton(root, "AvatarTabButton", "Avatares", new Vector2(0.71f, 0.79f), new Vector2(1.0f, 0.85f));

                SetField(script, "_pieceTabButton", pieceTab);
                SetField(script, "_boardTabButton", boardTab);
                SetField(script, "_avatarTabButton", avatarTab);
            }
        }

        private static void BuildCustomization(Transform canvasRoot)
        {
            Transform root = canvasRoot.Find("Customization");
            if (root == null || root.Find("Container") != null)
            {
                return;
            }

            var script = root.GetComponent<CustomizationScreen>();

            // Leaves a strip above the list for the category tabs — the
            // list itself used to run all the way up to 0.85.
            RectTransform container = CreatePanel(root, "Container");
            container.anchorMin = new Vector2(0.05f, 0.1f);
            container.anchorMax = new Vector2(0.95f, 0.78f);
            container.offsetMin = Vector2.zero;
            container.offsetMax = Vector2.zero;

            var containerLayout = container.gameObject.AddComponent<VerticalLayoutGroup>();
            // Without these, rows are left at their own uncontrolled
            // RectTransform size instead of being sized by the layout group
            // — the same bug the Shop list had (see ShopView.BuildRow).
            containerLayout.childControlWidth = true;
            containerLayout.childControlHeight = true;
            containerLayout.childForceExpandWidth = true;
            containerLayout.childForceExpandHeight = false;
            containerLayout.spacing = 8f;

            var pieceTab = CreateTabButton(root, "PieceTabButton", "Peças", new Vector2(0.05f, 0.79f), new Vector2(0.37f, 0.85f));
            var boardTab = CreateTabButton(root, "BoardTabButton", "Tabuleiros", new Vector2(0.38f, 0.79f), new Vector2(0.70f, 0.85f));
            var avatarTab = CreateTabButton(root, "AvatarTabButton", "Avatares", new Vector2(0.71f, 0.79f), new Vector2(1.0f, 0.85f));

            var backButton = CreateButton(root, "BackButton", "Voltar", 0);
            var backRect = (RectTransform)backButton.transform;
            backRect.anchorMin = new Vector2(0f, 0f);
            backRect.anchorMax = new Vector2(0f, 0f);
            backRect.pivot = new Vector2(0f, 0f);
            backRect.anchoredPosition = new Vector2(50f, 50f);

            var shopButton = CreateButton(root, "GoToShopButton", "Ir para a Loja", 0);
            var shopRect = (RectTransform)shopButton.transform;
            shopRect.anchorMin = new Vector2(1f, 0f);
            shopRect.anchorMax = new Vector2(1f, 0f);
            shopRect.pivot = new Vector2(1f, 0f);
            shopRect.anchoredPosition = new Vector2(-50f, 50f);

            SetField(script, "_listContainer", container);
            SetField(script, "_pieceTabButton", pieceTab);
            SetField(script, "_boardTabButton", boardTab);
            SetField(script, "_avatarTabButton", avatarTab);
            SetField(script, "_backButton", backButton);
            SetField(script, "_goToShopButton", shopButton);
        }

        private static void BuildSettings(Transform canvasRoot)
        {
            Transform root = canvasRoot.Find("Settings");
            if (root == null || root.Find("VolumeSlider") != null)
            {
                return;
            }

            var script = root.GetComponent<SettingsScreen>();

            Slider slider = CreateSlider(root, "VolumeSlider", -150);
            var backButton = CreateButton(root, "BackButton", "Voltar", -260);

            SetField(script, "_volumeSlider", slider);
            SetField(script, "_backButton", backButton);
        }

        /// <summary>
        /// Eighth pass, meant to run after <see cref="BuildDailyReward"/>:
        /// adds the resign-confirmation popup to the Game screen, the
        /// guest/account-linking section to Settings, and the friendly-online
        /// sub-menu (quick match / create / join by room code) to Play Mode.
        /// Safe to run again — every step is skipped where it was already done.
        /// </summary>
        [MenuItem("Othello/Build Match Rewards And Accounts")]
        public static void BuildMatchRewardsAndAccounts()
        {
            var canvas = Object.FindFirstObjectByType<Canvas>();
            if (canvas == null)
            {
                Debug.LogError("Nenhum Canvas encontrado na cena.");
                return;
            }
            Transform canvasRoot = canvas.transform;

            GameObject systemsGO = GameObject.Find("GameController");
            if (systemsGO == null)
            {
                Debug.LogError("GameObject 'GameController' não encontrado na cena.");
                return;
            }

            var localization = systemsGO.GetComponent<LocalizationController>();
            if (localization == null)
            {
                Debug.LogError("LocalizationController não encontrado. Rode 'Othello/Build Localization' primeiro.");
                return;
            }

            BuildResignConfirmUi(canvasRoot, localization);
            BuildAccountLinkUi(canvasRoot, localization);
            BuildDeleteAccountUi(canvasRoot, localization);
            BuildFriendlyOnlineUi(canvasRoot, localization);
            BuildMatchResultUi(canvasRoot, localization);

            EditorSceneManager.MarkSceneDirty(canvas.gameObject.scene);
            Debug.Log("Othello UI: confirmação de desistência, contas, exclusão de conta e partidas amistosas configuradas com sucesso.");
        }

        /// <summary>
        /// Adds the Yes/No confirmation popup <see cref="GameScreen"/> shows
        /// before actually resigning — a modal panel over the whole Game
        /// screen with a dimmed background, a message, and two buttons.
        /// Starts hidden; <see cref="GameScreen"/> itself toggles it.
        /// </summary>
        private static void BuildResignConfirmUi(Transform canvasRoot, LocalizationController localization)
        {
            Transform game = canvasRoot.Find("Game");
            if (game == null || game.Find("ResignConfirmPanel") != null)
            {
                return;
            }

            var script = game.GetComponent<GameScreen>();

            RectTransform panel = CreatePanel(game, "ResignConfirmPanel");
            var backgroundImage = panel.gameObject.AddComponent<Image>();
            backgroundImage.color = new Color(0f, 0f, 0f, 0.65f);

            CreateLabel(panel, "MessageLabel", "Desistir desta partida?", -40);
            var yesButton = CreateButton(panel, "YesButton", "Sim", -140);
            var noButton = CreateButton(panel, "NoButton", "Não", -220);

            SetField(script, "_resignConfirmPanel", panel.gameObject);
            SetField(script, "_resignConfirmYesButton", yesButton);
            SetField(script, "_resignConfirmNoButton", noButton);

            Localize(game, "ResignConfirmPanel/MessageLabel", localization, LocalizationKey.GameResignConfirmMessage);
            Localize(game, "ResignConfirmPanel/YesButton/Label", localization, LocalizationKey.GameResignConfirmYes);
            Localize(game, "ResignConfirmPanel/NoButton/Label", localization, LocalizationKey.GameResignConfirmNo);

            panel.gameObject.SetActive(false);
        }

        /// <summary>
        /// Adds the guest/account section to the Settings screen: a status
        /// label, and — only shown while the session is still a guest — an
        /// email/password pair and a "Create Account" button that link a
        /// permanent identity via <see cref="PlayerSession.LinkEmailPasswordAccount"/>.
        /// </summary>
        private static void BuildAccountLinkUi(Transform canvasRoot, LocalizationController localization)
        {
            Transform settings = canvasRoot.Find("Settings");
            if (settings == null || settings.Find("AccountStatusLabel") != null)
            {
                return;
            }

            var script = settings.GetComponent<SettingsScreen>();

            var statusLabel = CreateLabel(settings, "AccountStatusLabel", "Convidado", -540);

            RectTransform linkPanel = CreatePanel(settings, "AccountLinkPanel");
            var emailInput = CreateInputField(linkPanel, "EmailInput", -580);
            var passwordInput = CreateInputField(linkPanel, "PasswordInput", -660);
            passwordInput.contentType = TMP_InputField.ContentType.Password;
            var createAccountButton = CreateButton(linkPanel, "CreateAccountButton", "Criar Conta", -740);

            var feedbackLabel = CreateLabel(settings, "AccountFeedbackLabel", string.Empty, -820);

            SetField(script, "_accountStatusLabel", statusLabel);
            SetField(script, "_accountLinkPanel", linkPanel.gameObject);
            SetField(script, "_accountEmailInput", emailInput);
            SetField(script, "_accountPasswordInput", passwordInput);
            SetField(script, "_createAccountButton", createAccountButton);
            SetField(script, "_accountFeedbackLabel", feedbackLabel);

            Localize(settings, "AccountLinkPanel/CreateAccountButton/Label", localization, LocalizationKey.SettingsAccountCreateButton);

            feedbackLabel.gameObject.SetActive(false);
        }

        /// <summary>
        /// Adds the "Delete Account" button to the Settings screen and its
        /// Yes/No confirmation popup — required by both storefronts' account
        /// deletion policies since the game creates accounts. Shown
        /// regardless of <see cref="PlayerSession.HasAccount"/>: even a
        /// guest already has an anonymous Firebase identity and a saved
        /// cloud profile (see <see cref="PlayerSessionController.Start"/>)
        /// worth being able to erase. Mirrors <see cref="BuildResignConfirmUi"/>'s
        /// modal-popup pattern.
        /// </summary>
        private static void BuildDeleteAccountUi(Transform canvasRoot, LocalizationController localization)
        {
            Transform settings = canvasRoot.Find("Settings");
            if (settings == null || settings.Find("DeleteAccountButton") != null)
            {
                return;
            }

            var script = settings.GetComponent<SettingsScreen>();

            var deleteButton = CreateButton(settings, "DeleteAccountButton", "Excluir Conta", -900);

            RectTransform confirmPanel = CreatePanel(settings, "DeleteAccountConfirmPanel");
            var backgroundImage = confirmPanel.gameObject.AddComponent<Image>();
            backgroundImage.color = new Color(0f, 0f, 0f, 0.65f);

            CreateLabel(confirmPanel, "MessageLabel", "Excluir sua conta? Isso apaga permanentemente seu progresso, classificação e compras. Não pode ser desfeito.", -40);
            var yesButton = CreateButton(confirmPanel, "YesButton", "Excluir", -220);
            var noButton = CreateButton(confirmPanel, "NoButton", "Cancelar", -300);

            SetField(script, "_deleteAccountButton", deleteButton);
            SetField(script, "_deleteAccountConfirmPanel", confirmPanel.gameObject);
            SetField(script, "_deleteAccountConfirmYesButton", yesButton);
            SetField(script, "_deleteAccountConfirmNoButton", noButton);

            Localize(settings, "DeleteAccountButton/Label", localization, LocalizationKey.SettingsDeleteAccountButton);
            Localize(settings, "DeleteAccountConfirmPanel/MessageLabel", localization, LocalizationKey.SettingsDeleteAccountConfirmMessage);
            Localize(settings, "DeleteAccountConfirmPanel/YesButton/Label", localization, LocalizationKey.SettingsDeleteAccountConfirmYes);
            Localize(settings, "DeleteAccountConfirmPanel/NoButton/Label", localization, LocalizationKey.SettingsDeleteAccountConfirmNo);

            confirmPanel.gameObject.SetActive(false);
        }

        /// <summary>
        /// Adds the friendly-online sub-menu to Play Mode: picking "PvP
        /// Online" now leads here instead of straight into ranked
        /// matchmaking, offering a quick (ranked) match, creating a friendly
        /// room, or joining one by its code — plus the join-code entry panel
        /// and the room-code label shown on the searching panel once a
        /// friendly room has been created. See <see cref="PlayModeScreen"/>.
        /// </summary>
        private static void BuildFriendlyOnlineUi(Transform canvasRoot, LocalizationController localization)
        {
            Transform playMode = canvasRoot.Find("PlayMode");
            if (playMode == null || playMode.Find("OnlineModePanel") != null)
            {
                return;
            }

            var script = playMode.GetComponent<PlayModeScreen>();

            RectTransform onlineModePanel = CreatePanel(playMode, "OnlineModePanel");
            var quickMatchButton = CreateButton(onlineModePanel, "QuickMatchButton", "Partida Rápida", -100);
            var createFriendlyButton = CreateButton(onlineModePanel, "CreateFriendlyButton", "Criar Partida Amistosa", -180);
            var joinFriendlyButton = CreateButton(onlineModePanel, "JoinFriendlyButton", "Entrar em Partida Amistosa", -260);
            var onlineModeBackButton = CreateButton(onlineModePanel, "OnlineModeBackButton", "Voltar", -340);

            RectTransform joinCodePanel = CreatePanel(playMode, "JoinCodePanel");
            CreateLabel(joinCodePanel, "JoinCodeTitleLabel", "Digite o Código da Sala", -60);
            var joinCodeInput = CreateInputField(joinCodePanel, "JoinCodeInput", -140);
            var joinCodeConfirmButton = CreateButton(joinCodePanel, "JoinCodeConfirmButton", "Entrar", -220);
            var joinCodeBackButton = CreateButton(joinCodePanel, "JoinCodeBackButton", "Voltar", -300);

            SetField(script, "_onlineModePanel", onlineModePanel.gameObject);
            SetField(script, "_quickMatchButton", quickMatchButton);
            SetField(script, "_createFriendlyButton", createFriendlyButton);
            SetField(script, "_joinFriendlyButton", joinFriendlyButton);
            SetField(script, "_onlineModeBackButton", onlineModeBackButton);
            SetField(script, "_joinCodePanel", joinCodePanel.gameObject);
            SetField(script, "_joinCodeInput", joinCodeInput);
            SetField(script, "_joinCodeConfirmButton", joinCodeConfirmButton);
            SetField(script, "_joinCodeBackButton", joinCodeBackButton);

            Localize(playMode, "OnlineModePanel/QuickMatchButton/Label", localization, LocalizationKey.PlayModeQuickMatch);
            Localize(playMode, "OnlineModePanel/CreateFriendlyButton/Label", localization, LocalizationKey.PlayModeCreateFriendly);
            Localize(playMode, "OnlineModePanel/JoinFriendlyButton/Label", localization, LocalizationKey.PlayModeJoinFriendly);
            Localize(playMode, "OnlineModePanel/OnlineModeBackButton/Label", localization, LocalizationKey.ButtonBack);
            Localize(playMode, "JoinCodePanel/JoinCodeTitleLabel", localization, LocalizationKey.PlayModeJoinCodeTitle);
            Localize(playMode, "JoinCodePanel/JoinCodeConfirmButton/Label", localization, LocalizationKey.PlayModeJoinConfirm);
            Localize(playMode, "JoinCodePanel/JoinCodeBackButton/Label", localization, LocalizationKey.ButtonBack);

            onlineModePanel.gameObject.SetActive(false);
            joinCodePanel.gameObject.SetActive(false);

            // The room code label lives on the searching panel, alongside
            // the flavor-tip label — only meaningful once that panel already
            // exists (built by BuildOnlineSearchUi, an earlier pass).
            Transform searchingPanel = playMode.Find("SearchingPanel");
            if (searchingPanel != null && searchingPanel.Find("RoomCodeLabel") == null)
            {
                var roomCodeLabel = CreateLabel(searchingPanel, "RoomCodeLabel", string.Empty, -420);
                SetField(script, "_roomCodeLabel", roomCodeLabel);
                roomCodeLabel.gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// Adds the big post-match feedback overlay to the Game screen: a
        /// dimmed full-screen background behind a centered card with the
        /// result title, an optional subtitle, the currency reward, the
        /// rating change, and the button that used to be a plain
        /// "BackToMenuButton" living directly under Game (see
        /// <see cref="BuildGame"/>, which now removes that stray leftover).
        /// Deliberately large — per Victor's own note, this is the one
        /// piece of feedback that should occupy real screen space instead
        /// of sitting in a corner. Starts hidden;
        /// <see cref="GameScreen.RefreshMatchResultPanel"/> fills it in and
        /// shows it once a match ends.
        /// </summary>
        private static void BuildMatchResultUi(Transform canvasRoot, LocalizationController localization)
        {
            Transform game = canvasRoot.Find("Game");
            if (game == null || game.Find("MatchResultPanel") != null)
            {
                return;
            }

            var script = game.GetComponent<GameScreen>();

            RectTransform panel = CreatePanel(game, "MatchResultPanel");
            var dimImage = panel.gameObject.AddComponent<Image>();
            dimImage.color = new Color(0f, 0f, 0f, 0.72f);

            RectTransform card = CreatePanel(panel, "Card");
            card.anchorMin = new Vector2(0.08f, 0.28f);
            card.anchorMax = new Vector2(0.92f, 0.72f);
            card.offsetMin = Vector2.zero;
            card.offsetMax = Vector2.zero;
            var cardImage = card.gameObject.AddComponent<Image>();
            cardImage.color = new Color(0.97f, 0.97f, 0.97f, 1f);

            var titleLabel = CreateLabel(card, "TitleLabel", "Vitória!", -60);
            titleLabel.fontSize = 52f;
            titleLabel.fontStyle = FontStyles.Bold;
            ((RectTransform)titleLabel.transform).sizeDelta = new Vector2(-60f, 100f);

            var subtitleLabel = CreateLabel(card, "SubtitleLabel", string.Empty, -170);
            subtitleLabel.fontSize = 28f;

            var rewardLabel = CreateLabel(card, "RewardLabel", string.Empty, -230);
            rewardLabel.fontSize = 26f;
            rewardLabel.color = new Color(0.15f, 0.5f, 0.2f);

            var ratingLabel = CreateLabel(card, "RatingLabel", string.Empty, -280);
            ratingLabel.fontSize = 24f;

            var closeButton = CreateButton(card, "CloseButton", "Voltar ao Menu", -360);
            var closeRect = (RectTransform)closeButton.transform;
            closeRect.sizeDelta = new Vector2(420f, 80f);

            SetField(script, "_matchResultPanel", panel.gameObject);
            SetField(script, "_matchResultTitleLabel", titleLabel);
            SetField(script, "_matchResultSubtitleLabel", subtitleLabel);
            SetField(script, "_matchResultRewardLabel", rewardLabel);
            SetField(script, "_ratingChangeLabel", ratingLabel);
            SetField(script, "_backToMenuButton", closeButton);

            Localize(game, "MatchResultPanel/Card/CloseButton/Label", localization, LocalizationKey.GameBackToMenu);

            panel.gameObject.SetActive(false);
        }

        /// <summary>
        /// Builds the Login screen and sets it as <see cref="ScreenManager"/>'s
        /// <c>_initialScreen</c> — see <see cref="LoginScreen"/>'s own doc
        /// comment for why it belongs first. Run after 'Othello/Build Screen
        /// UI' and 'Othello/Build Localization', since it depends on
        /// <see cref="ScreenManager"/>, <see cref="LocalizationController"/>
        /// and <see cref="MainMenuScreen"/> already existing in the scene.
        /// Safe to run again: the whole screen is skipped once already
        /// built, but <c>_initialScreen</c> is reassigned every time in case
        /// Victor pointed it elsewhere by hand.
        /// </summary>
        [MenuItem("Othello/Build Login Screen")]
        public static void BuildLoginScreenCommand()
        {
            var canvas = Object.FindFirstObjectByType<Canvas>();
            if (canvas == null)
            {
                Debug.LogError("Nenhum Canvas encontrado na cena.");
                return;
            }
            Transform canvasRoot = canvas.transform;

            GameObject systemsGO = GameObject.Find("GameController");
            if (systemsGO == null)
            {
                Debug.LogError("GameObject 'GameController' não encontrado na cena.");
                return;
            }

            var localization = systemsGO.GetComponent<LocalizationController>();
            if (localization == null)
            {
                Debug.LogError("LocalizationController não encontrado. Rode 'Othello/Build Localization' primeiro.");
                return;
            }

            var screenManager = Object.FindFirstObjectByType<ScreenManager>();
            if (screenManager == null)
            {
                Debug.LogError("Nenhum ScreenManager encontrado na cena.");
                return;
            }

            var playerSession = systemsGO.GetComponent<PlayerSessionController>();
            if (playerSession == null)
            {
                Debug.LogError("PlayerSessionController não encontrado em 'GameController'.");
                return;
            }

            LoginScreen script = BuildLoginScreen(canvasRoot, screenManager, playerSession, localization);
            SetField(screenManager, "_initialScreen", script);

            EditorSceneManager.MarkSceneDirty(canvas.gameObject.scene);
            Debug.Log("Othello UI: tela de login configurada e definida como tela inicial.");
        }

        private static LoginScreen BuildLoginScreen(
            Transform canvasRoot, ScreenManager screenManager, PlayerSessionController playerSession, LocalizationController localization)
        {
            Transform existing = canvasRoot.Find("Login");
            if (existing != null)
            {
                var existingScript = existing.GetComponent<LoginScreen>();
                AddToScreenManager(screenManager, existingScript);
                return existingScript;
            }

            var loginGO = new GameObject("Login", typeof(RectTransform), typeof(Image), typeof(LoginScreen));
            loginGO.transform.SetParent(canvasRoot, false);

            var rect = (RectTransform)loginGO.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            loginGO.GetComponent<Image>().color = new Color(0.08f, 0.1f, 0.09f, 0.97f);

            var script = loginGO.GetComponent<LoginScreen>();
            SetField(script, "_root", loginGO);

            CreateLabel(loginGO.transform, "TitleLabel", "Bem-vindo", -50);
            Localize(loginGO.transform, "TitleLabel", localization, LocalizationKey.LoginTitle);

            var subtitleLabel = CreateLabel(loginGO.transform, "SubtitleLabel", string.Empty, -110);
            subtitleLabel.fontSize = 24f;
            ((RectTransform)subtitleLabel.transform).sizeDelta = new Vector2(-140f, 80f);
            Localize(loginGO.transform, "SubtitleLabel", localization, LocalizationKey.LoginSubtitle);

            var googleButton = CreateButton(loginGO.transform, "GoogleButton", "Continuar com Google", -230);
            Localize(loginGO.transform, "GoogleButton/Label", localization, LocalizationKey.LoginGoogleButton);

            var appleButton = CreateButton(loginGO.transform, "AppleButton", "Continuar com Apple", -310);
            Localize(loginGO.transform, "AppleButton/Label", localization, LocalizationKey.LoginAppleButton);

            var emailButton = CreateButton(loginGO.transform, "EmailButton", "Continuar com e-mail", -390);
            Localize(loginGO.transform, "EmailButton/Label", localization, LocalizationKey.LoginEmailButton);

            RectTransform emailPanel = CreatePanel(loginGO.transform, "EmailPanel");
            var emailInput = CreateInputField(emailPanel, "EmailInput", -470);
            var passwordInput = CreateInputField(emailPanel, "PasswordInput", -560);
            passwordInput.contentType = TMP_InputField.ContentType.Password;
            var emailConfirmButton = CreateButton(emailPanel, "EmailConfirmButton", "Confirmar", -650);
            Localize(loginGO.transform, "EmailPanel/EmailConfirmButton/Label", localization, LocalizationKey.LoginEmailConfirmButton);
            emailPanel.gameObject.SetActive(false);

            var rememberToggle = CreateToggle(loginGO.transform, "RememberToggle", "Salvar meu login", -730);
            Localize(loginGO.transform, "RememberToggle/Label", localization, LocalizationKey.LoginRememberUser);

            var autoConnectToggle = CreateToggle(loginGO.transform, "AutoConnectToggle", "Conectar automaticamente", -780);
            Localize(loginGO.transform, "AutoConnectToggle/Label", localization, LocalizationKey.LoginAutoConnect);

            var guestButton = CreateButton(loginGO.transform, "GuestButton", "Continuar como convidado", -860);
            Localize(loginGO.transform, "GuestButton/Label", localization, LocalizationKey.LoginGuestButton);

            var feedbackLabel = CreateLabel(loginGO.transform, "FeedbackLabel", string.Empty, -940);
            feedbackLabel.gameObject.SetActive(false);

            SetField(script, "_localization", localization);
            SetField(script, "_playerSession", playerSession);
            SetField(script, "_rememberToggle", rememberToggle);
            SetField(script, "_autoConnectToggle", autoConnectToggle);
            SetField(script, "_googleButton", googleButton);
            SetField(script, "_appleButton", appleButton);
            SetField(script, "_emailButton", emailButton);
            SetField(script, "_emailPanel", emailPanel.gameObject);
            SetField(script, "_emailInput", emailInput);
            SetField(script, "_passwordInput", passwordInput);
            SetField(script, "_emailConfirmButton", emailConfirmButton);
            SetField(script, "_guestButton", guestButton);
            SetField(script, "_feedbackLabel", feedbackLabel);

            AddToScreenManager(screenManager, script);

            return script;
        }

        private static RectTransform CreatePanel(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);

            var rect = (RectTransform)go.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            return rect;
        }

        private static TMP_Text CreateLabel(Transform parent, string name, string text, float y)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);

            var rect = (RectTransform)go.transform;
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, y);
            rect.sizeDelta = new Vector2(-100f, 40f);

            var label = go.AddComponent<TextMeshProUGUI>();
            label.text = text;
            label.alignment = TextAlignmentOptions.Center;
            label.fontSize = 28f;
            label.color = Color.black;
            return label;
        }

        private static Button CreateButton(Transform parent, string name, string label, float y)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);

            var rect = (RectTransform)go.transform;
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, y);
            rect.sizeDelta = new Vector2(500f, 70f);

            return DecorateButton(go, label);
        }

        /// <summary>
        /// A button stretched to fill an anchor rect instead of pinned by a
        /// fixed size and Y offset — used for the Customization screen's
        /// side-by-side category tabs, where <see cref="CreateButton"/>'s
        /// always-centered vertical stacking doesn't apply.
        /// </summary>
        private static Button CreateTabButton(Transform parent, string name, string label, Vector2 anchorMin, Vector2 anchorMax)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);

            var rect = (RectTransform)go.transform;
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = new Vector2(4f, 2f);
            rect.offsetMax = new Vector2(-4f, -2f);

            return DecorateButton(go, label);
        }

        /// <summary>
        /// Applies the shared button look (background sprite or flat
        /// fallback fill, centered label) to <paramref name="go"/>, whose
        /// RectTransform has already been positioned by the caller. Pulled
        /// out of <see cref="CreateButton"/> so <see cref="CreateTabButton"/>
        /// doesn't have to duplicate it.
        /// </summary>
        private static Button DecorateButton(GameObject go, string label)
        {
            var image = go.GetComponent<Image>();
            Sprite buttonSprite = LoadArtSprite("UI/button");
            if (buttonSprite != null)
            {
                image.sprite = buttonSprite;
                image.type = Image.Type.Sliced;
            }
            else
            {
                // Falls back to the original flat fill until the button art
                // has been imported into the project.
                image.color = new Color(0.85f, 0.85f, 0.85f, 1f);
            }

            var labelGO = new GameObject("Label", typeof(RectTransform));
            labelGO.transform.SetParent(go.transform, false);
            var labelRect = (RectTransform)labelGO.transform;
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;

            var tmp = labelGO.AddComponent<TextMeshProUGUI>();
            tmp.text = label;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.fontSize = 28f;
            tmp.color = Color.black;

            var button = go.GetComponent<Button>();
            button.targetGraphic = image;
            return button;
        }

        /// <summary>
        /// A small icon next to a centered text label, both sized by a
        /// <see cref="HorizontalLayoutGroup"/> instead of hand-placed —
        /// used for the "your rank" badge on both
        /// <see cref="RankingScreen"/> and <see cref="MainMenuScreen"/>.
        /// </summary>
        private static void CreateIconLabelRow(Transform parent, string name, float y, out Image icon, out TMP_Text label)
        {
            var row = new GameObject(name, typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
            row.transform.SetParent(parent, false);

            var rect = (RectTransform)row.transform;
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, y);
            rect.sizeDelta = new Vector2(-100f, 40f);

            var layout = row.GetComponent<HorizontalLayoutGroup>();
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.spacing = 8f;

            var rowElement = row.GetComponent<LayoutElement>();
            rowElement.preferredHeight = 40f;

            var iconGO = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            iconGO.transform.SetParent(row.transform, false);
            icon = iconGO.GetComponent<Image>();
            icon.raycastTarget = false;
            icon.enabled = false; // stays hidden until a real rank icon sprite is assigned at runtime
            var iconElement = iconGO.AddComponent<LayoutElement>();
            iconElement.preferredWidth = 32f;
            iconElement.preferredHeight = 32f;

            var labelGO = new GameObject("Label", typeof(RectTransform));
            labelGO.transform.SetParent(row.transform, false);
            label = labelGO.AddComponent<TextMeshProUGUI>();
            label.alignment = TextAlignmentOptions.Center;
            label.fontSize = 26f;
            label.color = Color.black;
            var labelElement = labelGO.AddComponent<LayoutElement>();
            labelElement.preferredWidth = 260f;
            labelElement.flexibleWidth = 0f;
        }

        private static Slider CreateSlider(Transform parent, string name, float y)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Slider));
            go.transform.SetParent(parent, false);

            var rect = (RectTransform)go.transform;
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, y);
            rect.sizeDelta = new Vector2(500f, 40f);

            var background = new GameObject("Background", typeof(RectTransform), typeof(Image));
            background.transform.SetParent(go.transform, false);
            Stretch((RectTransform)background.transform);
            background.GetComponent<Image>().color = new Color(0.7f, 0.7f, 0.7f, 1f);

            var fillArea = new GameObject("Fill Area", typeof(RectTransform));
            fillArea.transform.SetParent(go.transform, false);
            Stretch((RectTransform)fillArea.transform);

            var fill = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            fill.transform.SetParent(fillArea.transform, false);
            Stretch((RectTransform)fill.transform);
            fill.GetComponent<Image>().color = new Color(0.2f, 0.6f, 0.9f, 1f);

            var handleArea = new GameObject("Handle Slide Area", typeof(RectTransform));
            handleArea.transform.SetParent(go.transform, false);
            Stretch((RectTransform)handleArea.transform);

            var handle = new GameObject("Handle", typeof(RectTransform), typeof(Image));
            handle.transform.SetParent(handleArea.transform, false);
            var handleRect = (RectTransform)handle.transform;
            handleRect.sizeDelta = new Vector2(24f, 0f);
            var handleImage = handle.GetComponent<Image>();
            handleImage.color = Color.white;

            var slider = go.GetComponent<Slider>();
            slider.fillRect = (RectTransform)fill.transform;
            slider.handleRect = handleRect;
            slider.targetGraphic = handleImage;
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = 1f;

            return slider;
        }

        private static TMP_InputField CreateInputField(Transform parent, string name, float y)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(TMP_InputField));
            go.transform.SetParent(parent, false);

            var rect = (RectTransform)go.transform;
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, y);
            rect.sizeDelta = new Vector2(500f, 70f);

            var image = go.GetComponent<Image>();
            image.color = new Color(0.95f, 0.95f, 0.95f, 1f);

            var textGO = new GameObject("Text", typeof(RectTransform));
            textGO.transform.SetParent(go.transform, false);
            Stretch((RectTransform)textGO.transform);

            var textComponent = textGO.AddComponent<TextMeshProUGUI>();
            textComponent.alignment = TextAlignmentOptions.MidlineLeft;
            textComponent.fontSize = 28f;
            textComponent.color = Color.black;

            var inputField = go.GetComponent<TMP_InputField>();
            inputField.textComponent = textComponent;
            inputField.targetGraphic = image;

            return inputField;
        }

        /// <summary>
        /// A labeled checkbox: a small square background with a fill image
        /// as its checkmark (<see cref="Toggle.graphic"/>, which Unity's
        /// own <see cref="Toggle"/> shows/hides for you) and a label to its
        /// right. Positioned the same way <see cref="CreateButton"/> is —
        /// pinned to the top-center, offset down by <paramref name="y"/>.
        /// </summary>
        private static Toggle CreateToggle(Transform parent, string name, string label, float y)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Toggle));
            go.transform.SetParent(parent, false);

            var rect = (RectTransform)go.transform;
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, y);
            rect.sizeDelta = new Vector2(500f, 50f);

            var backgroundGO = new GameObject("Background", typeof(RectTransform), typeof(Image));
            backgroundGO.transform.SetParent(go.transform, false);
            var backgroundRect = (RectTransform)backgroundGO.transform;
            backgroundRect.anchorMin = new Vector2(0f, 0.5f);
            backgroundRect.anchorMax = new Vector2(0f, 0.5f);
            backgroundRect.pivot = new Vector2(0f, 0.5f);
            backgroundRect.anchoredPosition = Vector2.zero;
            backgroundRect.sizeDelta = new Vector2(40f, 40f);
            var backgroundImage = backgroundGO.GetComponent<Image>();
            backgroundImage.color = new Color(0.95f, 0.95f, 0.95f, 1f);

            var checkGO = new GameObject("Checkmark", typeof(RectTransform), typeof(Image));
            checkGO.transform.SetParent(backgroundGO.transform, false);
            var checkRect = (RectTransform)checkGO.transform;
            checkRect.anchorMin = new Vector2(0.15f, 0.15f);
            checkRect.anchorMax = new Vector2(0.85f, 0.85f);
            checkRect.offsetMin = Vector2.zero;
            checkRect.offsetMax = Vector2.zero;
            var checkImage = checkGO.GetComponent<Image>();
            checkImage.color = new Color(0.15f, 0.5f, 0.2f, 1f);

            var toggle = go.GetComponent<Toggle>();
            toggle.targetGraphic = backgroundImage;
            toggle.graphic = checkImage;

            var labelComponent = CreateLabel(go.transform, "Label", label, 0f);
            labelComponent.alignment = TextAlignmentOptions.MidlineLeft;
            var labelRect = (RectTransform)labelComponent.transform;
            labelRect.anchorMin = new Vector2(0f, 0.5f);
            labelRect.anchorMax = new Vector2(1f, 0.5f);
            labelRect.pivot = new Vector2(0f, 0.5f);
            labelRect.anchoredPosition = new Vector2(52f, 0f);
            labelRect.sizeDelta = new Vector2(-52f, 40f);

            return toggle;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static void SetField(Object target, string fieldName, Object value)
        {
            if (target == null)
            {
                Debug.LogError($"Não foi possível atribuir '{fieldName}': script alvo é nulo.");
                return;
            }

            var serialized = new SerializedObject(target);
            SerializedProperty property = serialized.FindProperty(fieldName);
            if (property == null)
            {
                Debug.LogError($"Campo '{fieldName}' não encontrado em {target.GetType().Name}.");
                return;
            }

            property.objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetEnumField(Object target, string fieldName, System.Enum value)
        {
            if (target == null)
            {
                Debug.LogError($"Não foi possível atribuir '{fieldName}': script alvo é nulo.");
                return;
            }

            var serialized = new SerializedObject(target);
            SerializedProperty property = serialized.FindProperty(fieldName);
            if (property == null)
            {
                Debug.LogError($"Campo '{fieldName}' não encontrado em {target.GetType().Name}.");
                return;
            }

            property.enumValueIndex = System.Convert.ToInt32(value);
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>
        /// Dev-only shortcut for testing the Shop without playing through
        /// matches/achievements to earn currency for real. Only works in
        /// Play Mode, since <see cref="MonetizationController.State"/> is
        /// only ever created there. Not part of the game itself — nothing
        /// in <c>Othello.Unity</c> calls this.
        /// </summary>
        [MenuItem("Othello/Debug/Grant Test Currency")]
        public static void GrantTestCurrency()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("Só funciona em Play Mode.");
                return;
            }

            GameObject systemsGO = GameObject.Find("GameController");
            var monetization = systemsGO != null ? systemsGO.GetComponent<MonetizationController>() : null;
            if (monetization == null || monetization.State == null)
            {
                Debug.LogWarning("MonetizationController ainda não está pronto.");
                return;
            }

            monetization.State.Wallet.Add(CurrencyType.Soft, 1000);
            monetization.State.Wallet.Add(CurrencyType.Hard, 500);
            Debug.Log($"Saldo de teste concedido. Soft={monetization.State.Wallet.SoftBalance}, Hard={monetization.State.Wallet.HardBalance}");
        }
    }
}

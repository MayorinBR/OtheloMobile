using Othello.AI;
using Othello.Core;
using Othello.Services;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Othello.Unity
{
    /// <summary>
    /// Lets the player choose how the next match is played: local
    /// pass-and-play, online (matched via <see cref="OnlineMatchController"/>),
    /// or against the AI. Picking "Contra a IA" swaps the mode buttons for a
    /// difficulty sub-panel instead of navigating to a whole new screen,
    /// since "Fácil / Médio / Difícil" is still the same "how do I want to
    /// play" decision the player is in the middle of; picking "PvP Online"
    /// swaps them for a searching panel the same way.
    /// </summary>
    public sealed class PlayModeScreen : UIScreen
    {
        [SerializeField]
        private OnlineMatchController _onlineMatch;

        [SerializeField]
        private PlayerSessionController _playerSession;

        [SerializeField]
        private LocalizationController _localization;

        [SerializeField]
        private GameObject _modePanel;

        [SerializeField]
        private GameObject _difficultyPanel;

        [SerializeField]
        private GameObject _searchingPanel;

        [SerializeField]
        private GameObject _onlineModePanel;

        [SerializeField]
        private GameObject _joinCodePanel;

        [SerializeField]
        private Button _localPvpButton;

        [SerializeField]
        private Button _onlinePvpButton;

        [SerializeField]
        private Button _vsAiButton;

        [SerializeField]
        private Button _backButton;

        [SerializeField]
        private Button _quickMatchButton;

        [SerializeField]
        private Button _createFriendlyButton;

        [SerializeField]
        private Button _joinFriendlyButton;

        [SerializeField]
        private Button _onlineModeBackButton;

        [SerializeField]
        private TMP_InputField _joinCodeInput;

        [SerializeField]
        private Button _joinCodeConfirmButton;

        [SerializeField]
        private Button _joinCodeBackButton;

        [SerializeField]
        private TMP_Text _roomCodeLabel;

        [SerializeField]
        private Button _easyButton;

        [SerializeField]
        private Button _mediumButton;

        [SerializeField]
        private Button _hardButton;

        [SerializeField]
        private Button _difficultyBackButton;

        [SerializeField]
        private Button _cancelSearchButton;

        [SerializeField]
        private TMP_Text _searchingTipLabel;

        [SerializeField]
        private GameScreen _gameScreen;

        /// <summary>Rotating flavor tips shown while searching for an opponent, cycled every <see cref="TipRotationSeconds"/>.</summary>
        private static readonly LocalizationKey[] SearchingTips =
        {
            LocalizationKey.MatchmakingTip1,
            LocalizationKey.MatchmakingTip2,
            LocalizationKey.MatchmakingTip3
        };

        private const float TipRotationSeconds = 4f;

        private float _searchElapsedSeconds;
        private float _tipTimer;
        private int _tipIndex;

        private void Awake()
        {
            _localPvpButton?.onClick.AddListener(() => BeginMatch(GameMode.LocalPvP, Difficulty.Medium));
            _vsAiButton?.onClick.AddListener(ShowDifficultyPanel);
            _onlinePvpButton?.onClick.AddListener(ShowOnlineModePanel);
            _backButton?.onClick.AddListener(() => Manager.GoBack());

            _easyButton?.onClick.AddListener(() => BeginMatch(GameMode.VsAI, Difficulty.Easy));
            _mediumButton?.onClick.AddListener(() => BeginMatch(GameMode.VsAI, Difficulty.Medium));
            _hardButton?.onClick.AddListener(() => BeginMatch(GameMode.VsAI, Difficulty.Hard));
            _difficultyBackButton?.onClick.AddListener(ShowModePanel);

            _cancelSearchButton?.onClick.AddListener(CancelOnlineSearch);

            _quickMatchButton?.onClick.AddListener(BeginOnlineSearch);
            _createFriendlyButton?.onClick.AddListener(BeginFriendlyCreate);
            _joinFriendlyButton?.onClick.AddListener(ShowJoinCodePanel);
            _onlineModeBackButton?.onClick.AddListener(ShowModePanel);

            _joinCodeConfirmButton?.onClick.AddListener(() => BeginFriendlyJoin(_joinCodeInput != null ? _joinCodeInput.text : string.Empty));
            _joinCodeBackButton?.onClick.AddListener(ShowOnlineModePanel);
        }

        protected override void OnShown()
        {
            if (_joinCodeInput != null)
            {
                _joinCodeInput.text = string.Empty;
            }

            SetRoomCodeLabel(null);
            ShowModePanel();
        }

        protected override void OnHidden() => CancelOnlineSearch();

        private void ShowModePanel() => ShowOnly(_modePanel);

        private void ShowDifficultyPanel() => ShowOnly(_difficultyPanel);

        private void ShowOnlineModePanel() => ShowOnly(_onlineModePanel);

        private void ShowJoinCodePanel() => ShowOnly(_joinCodePanel);

        private void ShowSearchingPanel() => ShowOnly(_searchingPanel);

        private void ShowOnly(GameObject panel)
        {
            _modePanel?.SetActive(panel == _modePanel);
            _difficultyPanel?.SetActive(panel == _difficultyPanel);
            _onlineModePanel?.SetActive(panel == _onlineModePanel);
            _joinCodePanel?.SetActive(panel == _joinCodePanel);
            _searchingPanel?.SetActive(panel == _searchingPanel);
        }

        private void BeginMatch(GameMode mode, Difficulty difficulty)
        {
            _gameScreen.BeginMatch(mode, difficulty, Player.Black);
            Manager.Show<GameScreen>();
        }

        private void BeginOnlineSearch()
        {
            if (_onlineMatch == null || _playerSession == null || _playerSession.Session == null)
            {
                return;
            }

            PlayerSession session = _playerSession.Session;
            StartSearchingUi(roomCode: null);
            _onlineMatch.Service.FindMatch(session.DisplayName, session.Rating);
        }

        /// <summary>
        /// Creates a friendly room and immediately starts waiting in it — see
        /// <see cref="IOnlineMatchService.CreateFriendlyMatch"/>. Unlike a
        /// ranked search, the searching panel here also shows the room code
        /// so the player can share it with whoever they want to play.
        /// </summary>
        private void BeginFriendlyCreate()
        {
            if (_onlineMatch == null || _playerSession == null || _playerSession.Session == null)
            {
                return;
            }

            PlayerSession session = _playerSession.Session;
            string code = _onlineMatch.Service.CreateFriendlyMatch(session.DisplayName);
            StartSearchingUi(code);
        }

        /// <summary>Joins a friendly room by the code the creator shared — see <see cref="IOnlineMatchService.JoinFriendlyMatch"/>.</summary>
        private void BeginFriendlyJoin(string roomCode)
        {
            if (_onlineMatch == null || _playerSession == null || _playerSession.Session == null || string.IsNullOrWhiteSpace(roomCode))
            {
                return;
            }

            PlayerSession session = _playerSession.Session;
            StartSearchingUi(roomCode: null);
            _onlineMatch.Service.JoinFriendlyMatch(session.DisplayName, roomCode);
        }

        /// <summary>
        /// Common setup shared by every way of entering the searching panel
        /// — a ranked quick match, creating a friendly room, or joining one.
        /// <paramref name="roomCode"/> is only passed when this player just
        /// created the room, since that's the only case with a code worth
        /// showing to share; joining or a ranked search leave it null and the
        /// room code label just stays hidden.
        /// </summary>
        private void StartSearchingUi(string roomCode)
        {
            ShowSearchingPanel();
            _searchElapsedSeconds = 0f;
            _tipTimer = 0f;
            _tipIndex = 0;
            UpdateSearchingTipLabel();
            SetRoomCodeLabel(roomCode);

            _onlineMatch.Service.MatchFound += OnOnlineMatchFound;
        }

        private void SetRoomCodeLabel(string code)
        {
            if (_roomCodeLabel == null)
            {
                return;
            }

            bool hasCode = !string.IsNullOrEmpty(code);
            _roomCodeLabel.gameObject.SetActive(hasCode);
            if (hasCode && _localization != null)
            {
                _roomCodeLabel.text = string.Format(_localization.Service.Get(LocalizationKey.PlayModeRoomCodeFormat), code);
            }
        }

        /// <summary>
        /// Advances the elapsed search time and, every <see cref="TipRotationSeconds"/>,
        /// rotates to the next flavor tip — only does anything while the
        /// searching panel is actually visible.
        /// </summary>
        private void Update()
        {
            if (_searchingPanel == null || !_searchingPanel.activeSelf)
            {
                return;
            }

            _searchElapsedSeconds += Time.deltaTime;
            _tipTimer += Time.deltaTime;
            if (_tipTimer >= TipRotationSeconds)
            {
                _tipTimer = 0f;
                _tipIndex = (_tipIndex + 1) % SearchingTips.Length;
            }

            UpdateSearchingTipLabel();
        }

        private void UpdateSearchingTipLabel()
        {
            if (_searchingTipLabel == null || _localization == null)
            {
                return;
            }

            string tip = _localization.Service.Get(SearchingTips[_tipIndex]);
            string elapsed = string.Format(
                _localization.Service.Get(LocalizationKey.MatchmakingElapsedFormat), Mathf.FloorToInt(_searchElapsedSeconds));
            _searchingTipLabel.text = $"{tip}\n{elapsed}";
        }

        private void CancelOnlineSearch()
        {
            if (_onlineMatch == null)
            {
                return;
            }

            _onlineMatch.Service.MatchFound -= OnOnlineMatchFound;
            _onlineMatch.Service.CancelMatchmaking();
        }

        private void OnOnlineMatchFound(OnlineOpponent opponent)
        {
            _onlineMatch.Service.MatchFound -= OnOnlineMatchFound;
            _onlineMatch.Service.LeaveMatch();

            _gameScreen.BeginOnlineMatch(opponent, Player.Black, _onlineMatch.Relay);
            Manager.Show<GameScreen>();
        }
    }
}

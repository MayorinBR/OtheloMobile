using Othello.AI;
using Othello.Core;
using Othello.Services;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Othello.Unity
{
    /// <summary>
    /// Hosts one Othello match: binds <see cref="BoardView"/> to
    /// <see cref="GameController"/> and keeps a small turn/score HUD in
    /// sync with it.
    /// <para>
    /// <see cref="BeginMatch"/> must be called (by <see cref="PlayModeScreen"/>)
    /// before this screen is shown — <see cref="ScreenManager.Show{TScreen}"/>
    /// has no way to pass parameters into a screen, so starting the match is
    /// the caller's job and this screen only ever reflects whatever match is
    /// currently running.
    /// </para>
    /// </summary>
    public sealed class GameScreen : UIScreen
    {
        [SerializeField]
        private GameController _gameController;

        [SerializeField]
        private BoardView _boardView;

        [SerializeField]
        private LocalizationController _localization;

        [SerializeField]
        private PlayerSessionController _playerSession;

        [SerializeField]
        private MonetizationController _monetization;

        [SerializeField]
        private TMP_Text _turnLabel;

        [SerializeField]
        private TMP_Text _scoreLabel;

        [SerializeField]
        private TMP_Text _opponentLabel;

        [SerializeField]
        private TMP_Text _ratingChangeLabel;

        [SerializeField]
        private TMP_Text _achievementToastLabel;

        [SerializeField]
        private Button _backToMenuButton;

        [SerializeField]
        private LeaderboardController _leaderboard;

        [SerializeField]
        private TurnTimerController _turnTimer;

        [SerializeField]
        private PlayerProfilePanel _playerPanel;

        [SerializeField]
        private PlayerProfilePanel _opponentPanel;

        [SerializeField]
        private TMP_Text _modeLabel;

        [SerializeField]
        private TMP_Text _timerLabel;

        [SerializeField]
        private Button _resignButton;

        [SerializeField]
        private GameObject _resignConfirmPanel;

        [SerializeField]
        private Button _resignConfirmYesButton;

        [SerializeField]
        private Button _resignConfirmNoButton;

        [SerializeField]
        private GameObject _matchResultPanel;

        [SerializeField]
        private TMP_Text _matchResultTitleLabel;

        [SerializeField]
        private TMP_Text _matchResultSubtitleLabel;

        [SerializeField]
        private TMP_Text _matchResultRewardLabel;

        private OthelloGame _boundGame;
        private bool _subscribedToAchievements;

        private void Awake()
        {
            _backToMenuButton?.onClick.AddListener(() => Manager.ShowRoot<MainMenuScreen>());
            _resignButton?.onClick.AddListener(ShowResignConfirm);
            _resignConfirmYesButton?.onClick.AddListener(ConfirmResign);
            _resignConfirmNoButton?.onClick.AddListener(HideResignConfirm);

            ApplyCornerLayout();
        }

        /// <summary>
        /// Forces the big centered square board with corner-anchored HUD,
        /// resign button and player panels at runtime, every time this
        /// screen loads. The scene file and the Editor build tool
        /// (<c>OthelloUiBuilder</c>) are meant to set these same anchors at
        /// edit time, but that kept failing to stick (stale compiles,
        /// scene saves overwriting edits) — doing it here too guarantees
        /// the layout is correct the moment the game actually runs.
        /// </summary>
        private void ApplyCornerLayout()
        {
            Transform boardGrid = transform.Find("BoardGrid");
            if (boardGrid != null)
            {
                SetAnchors(boardGrid, new Vector2(0.03f, 0.04f), new Vector2(0.97f, 0.95f));

                var gridLayoutGroup = boardGrid.GetComponent<GridLayoutGroup>();
                if (gridLayoutGroup != null)
                {
                    gridLayoutGroup.childAlignment = TextAnchor.MiddleCenter;
                }

                if (boardGrid.GetComponent<ResponsiveGridLayout>() == null)
                {
                    boardGrid.gameObject.AddComponent<ResponsiveGridLayout>();
                }
            }

            SetAnchors(transform.Find("BoardBorder"), new Vector2(0.03f, 0.04f), new Vector2(0.97f, 0.95f));
            SetAnchors(transform.Find("Hud"), new Vector2(0.01f, 0.01f), new Vector2(0.26f, 0.23f));
            SetAnchors(_playerPanel != null ? _playerPanel.transform : null, new Vector2(0.01f, 0.74f), new Vector2(0.23f, 0.99f));
            SetAnchors(_opponentPanel != null ? _opponentPanel.transform : null, new Vector2(0.77f, 0.74f), new Vector2(0.99f, 0.99f));
            SetAnchors(_achievementToastLabel != null ? _achievementToastLabel.transform : null, new Vector2(0.01f, 0.24f), new Vector2(0.30f, 0.31f));

            if (_resignButton != null)
            {
                var resignRect = (RectTransform)_resignButton.transform;
                resignRect.anchorMin = new Vector2(1f, 0f);
                resignRect.anchorMax = new Vector2(1f, 0f);
                resignRect.pivot = new Vector2(1f, 0f);
                resignRect.anchoredPosition = new Vector2(-24f, 24f);
                resignRect.sizeDelta = new Vector2(260f, 70f);
            }
        }

        private static void SetAnchors(Transform target, Vector2 anchorMin, Vector2 anchorMax)
        {
            if (target == null)
            {
                return;
            }

            var rect = (RectTransform)target;
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private void ShowResignConfirm() => _resignConfirmPanel?.SetActive(true);

        private void HideResignConfirm() => _resignConfirmPanel?.SetActive(false);

        /// <summary>
        /// Actually resigns after the player confirms — see
        /// <see cref="GameController.Resign"/> for what happens to the match
        /// reward. Also shows an interstitial ad right away: a resignation
        /// is the one match ending that skips the natural post-match beats
        /// (result screen lingering, a real opponent's reaction), so it gets
        /// its own guaranteed ad instead of waiting on
        /// <see cref="MonetizationController"/>'s periodic timer, which may
        /// not be due yet. Skipped entirely for a "no ads" purchaser.
        /// </summary>
        private void ConfirmResign()
        {
            HideResignConfirm();
            _gameController.Resign();

            if (_monetization != null && !_monetization.State.IsNoAdsPurchased)
            {
                _monetization.Services.Resolve<IAdsService>().ShowInterstitial();
            }
        }

        private void Start()
        {
            // Safe here (not in Awake): LocalizationController creates its
            // Service in its own Awake, and every Awake runs before any Start.
            if (_localization != null)
            {
                _localization.Service.Changed += RefreshHud;
            }

            if (_turnTimer != null)
            {
                _turnTimer.TimeRemainingChanged += OnTimerTick;
            }
        }

        /// <summary>Starts a new local or Vs-AI match and points this screen's board and HUD at it.</summary>
        public void BeginMatch(GameMode mode, Difficulty aiDifficulty, Player humanPlayer)
        {
            _gameController.StartNewMatch(mode, aiDifficulty, humanPlayer);
            _boardView.Bind(_gameController);
        }

        /// <summary>
        /// Starts a new online match against <paramref name="opponent"/> and
        /// points this screen's board and HUD at it. <paramref name="relay"/>
        /// is the real move relay to that opponent (Firestore, via
        /// <see cref="FirebaseOnlineMatchService"/>, once wired in) — see
        /// <see cref="GameController.StartOnlineMatch"/>.
        /// </summary>
        public void BeginOnlineMatch(OnlineOpponent opponent, Player humanPlayer, IOnlineMoveRelay relay = null)
        {
            _gameController.StartOnlineMatch(opponent, humanPlayer, relay);
            _boardView.Bind(_gameController);
        }

        protected override void OnShown()
        {
            RebindGameEvents();
            RefreshHud();
            RefreshOpponentLabel();
            RefreshPlayerPanels();
            RefreshModeLabel();
            SubscribeToAchievements();

            if (_ratingChangeLabel != null)
            {
                _ratingChangeLabel.gameObject.SetActive(false);
            }

            if (_achievementToastLabel != null)
            {
                _achievementToastLabel.gameObject.SetActive(false);
            }

            if (_matchResultPanel != null)
            {
                _matchResultPanel.SetActive(false);
            }

            if (_timerLabel != null)
            {
                _timerLabel.gameObject.SetActive(_gameController.Mode != GameMode.VsAI);
            }

            HideResignConfirm();
        }

        protected override void OnHidden()
        {
            if (_boundGame != null)
            {
                _boundGame.MoveApplied -= OnMoveApplied;
                _boundGame.GameEnded -= OnGameEnded;
            }
            _boundGame = null;
        }

        private void RebindGameEvents()
        {
            if (_boundGame != null)
            {
                _boundGame.MoveApplied -= OnMoveApplied;
                _boundGame.GameEnded -= OnGameEnded;
            }

            _boundGame = _gameController.Game;
            if (_boundGame != null)
            {
                _boundGame.MoveApplied += OnMoveApplied;
                _boundGame.GameEnded += OnGameEnded;
            }
        }

        private void OnMoveApplied(int square, Player mover) => RefreshHud();

        private void OnGameEnded()
        {
            HideResignConfirm();
            RefreshHud();
            RefreshMatchResultPanel();
            RecordMatchResultIfApplicable();
            GrantMatchCurrencyReward();
        }

        private void RefreshOpponentLabel()
        {
            if (_opponentLabel == null)
            {
                return;
            }

            OnlineOpponent opponent = _gameController.CurrentOpponent;
            bool isOnlineMatch = _gameController.Mode == GameMode.OnlinePvP && opponent != null;
            _opponentLabel.gameObject.SetActive(isOnlineMatch);
            if (isOnlineMatch)
            {
                _opponentLabel.text = string.Format(
                    _localization.Service.Get(LocalizationKey.MatchmakingOpponentFormat), opponent.DisplayName, opponent.Rating);
            }
        }

        /// <summary>
        /// Populates both side panels for whatever match is currently
        /// running. The session player always goes on the left; what goes
        /// on the right depends on <see cref="GameController.Mode"/> — the
        /// matched opponent's real profile for <see cref="GameMode.OnlinePvP"/>,
        /// a fixed AI persona plus the difficulty for <see cref="GameMode.VsAI"/>,
        /// or a generic second-player placeholder for
        /// <see cref="GameMode.LocalPvP"/> (which has no real second
        /// identity to show).
        /// </summary>
        private void RefreshPlayerPanels()
        {
            if (_playerPanel == null || _opponentPanel == null || _localization == null)
            {
                return;
            }

            PlayerSession session = _playerSession?.Session;
            string localName = session?.DisplayName ?? PlayerName(_gameController.HumanPlayer);
            Sprite localAvatar = session != null ? CosmeticArt.LoadAvatar(session.AvatarId) : null;
            _playerPanel.SetIdentity(localName, localAvatar);
            SetPieceColorOnPanel(_playerPanel, _gameController.HumanPlayer);
            _playerPanel.SetExtraInfo(null);

            // A guest (no linked account, see PlayerSession.HasAccount) never
            // enters the ranking system, so there's no meaningful position
            // to show them here — the rank row just stays hidden instead of
            // showing a number that will never actually change.
            if (session != null && session.HasAccount && _leaderboard != null)
            {
                _leaderboard.Service.GetRanked(session.DisplayName, session.Rating, windowSize: 1, entries =>
                {
                    if (this == null || !gameObject.activeInHierarchy || entries.Count == 0)
                    {
                        return;
                    }
                    ApplyRankToPanel(_playerPanel, entries[0]);
                });
            }
            else
            {
                _playerPanel.HideRank();
            }

            Player opponentColor = _gameController.HumanPlayer == Player.Black ? Player.White : Player.Black;

            switch (_gameController.Mode)
            {
                case GameMode.LocalPvP:
                    _opponentPanel.SetIdentity(_localization.Service.Get(LocalizationKey.ProfileLocalPlayerTwo), null);
                    SetPieceColorOnPanel(_opponentPanel, opponentColor);
                    _opponentPanel.HideRank();
                    _opponentPanel.SetExtraInfo(null);
                    break;

                case GameMode.VsAI:
                    _opponentPanel.SetIdentity(_gameController.OpponentDisplayName, CosmeticArt.LoadAvatar(_gameController.OpponentAvatarId));
                    SetPieceColorOnPanel(_opponentPanel, opponentColor);
                    _opponentPanel.HideRank();
                    LocalizationKey difficultyKey = _gameController.AiDifficulty switch
                    {
                        Difficulty.Easy => LocalizationKey.DifficultyEasy,
                        Difficulty.Medium => LocalizationKey.DifficultyMedium,
                        _ => LocalizationKey.DifficultyHard
                    };
                    _opponentPanel.SetExtraInfo(string.Format(
                        _localization.Service.Get(LocalizationKey.ProfileDifficultyFormat), _localization.Service.Get(difficultyKey)));
                    break;

                case GameMode.OnlinePvP:
                    OnlineOpponent opponent = _gameController.CurrentOpponent;
                    _opponentPanel.SetIdentity(opponent?.DisplayName, opponent != null ? CosmeticArt.LoadAvatar(opponent.AvatarId) : null);
                    SetPieceColorOnPanel(_opponentPanel, opponentColor);
                    _opponentPanel.SetExtraInfo(null);

                    if (opponent != null && _leaderboard != null)
                    {
                        _leaderboard.Service.GetRanked(opponent.DisplayName, opponent.Rating, windowSize: 1, entries =>
                        {
                            if (this == null || !gameObject.activeInHierarchy || entries.Count == 0)
                            {
                                return;
                            }
                            ApplyRankToPanel(_opponentPanel, entries[0]);
                        });
                    }
                    else
                    {
                        _opponentPanel.HideRank();
                    }
                    break;
            }
        }

        private void ApplyRankToPanel(PlayerProfilePanel panel, LeaderboardEntry entry)
        {
            Sprite icon = CosmeticArt.LoadRankIcon(entry.Tier);
            string tierName = _localization.Service.Get(RankTierCatalog.GetNameKey(entry.Tier));
            string rankText = string.Format(_localization.Service.Get(LocalizationKey.RankTierPositionFormat), tierName, entry.Rank);
            panel.SetRank(icon, rankText);
        }

        /// <summary>
        /// Shows a plain circle tinted per <paramref name="player"/> — the
        /// same fallback disc look <see cref="BoardView"/> uses, reused here
        /// since a profile panel has no equipped-skin sprite of its own to show.
        /// </summary>
        private void SetPieceColorOnPanel(PlayerProfilePanel panel, Player player)
        {
            Sprite circle = CircleSpriteFactory.GetCircleSprite();
            Color color = player == Player.Black ? Color.black : Color.white;
            string text = string.Format(_localization.Service.Get(LocalizationKey.ProfilePieceColorFormat), PlayerName(player));
            panel.SetPieceColor(circle, color, text);
        }

        private void RefreshModeLabel()
        {
            if (_modeLabel == null || _localization == null)
            {
                return;
            }

            bool isFriendlyOnline = _gameController.Mode == GameMode.OnlinePvP && (_gameController.CurrentOpponent?.IsFriendly ?? false);
            LocalizationKey modeKey = _gameController.Mode switch
            {
                GameMode.LocalPvP => LocalizationKey.PlayModeLocalPvp,
                GameMode.OnlinePvP => isFriendlyOnline ? LocalizationKey.PlayModeOnlineFriendly : LocalizationKey.PlayModeOnlinePvp,
                _ => LocalizationKey.PlayModeVsAi
            };
            _modeLabel.text = string.Format(
                _localization.Service.Get(LocalizationKey.GameModeLabelFormat), _localization.Service.Get(modeKey));
        }

        private void OnTimerTick(float secondsRemaining)
        {
            if (_timerLabel == null || _localization == null)
            {
                return;
            }

            _timerLabel.text = string.Format(
                _localization.Service.Get(LocalizationKey.GameTimerFormat), Mathf.CeilToInt(secondsRemaining));
        }

        /// <summary>
        /// Records the finished match's result — play stats, Elo rating for
        /// ranked online matches, and any newly-unlocked achievements — for
        /// every mode except local pass-and-play, which has no single "the
        /// player" perspective to record a result for.
        /// <para>
        /// "Ranked" excludes a friendly online match (<see cref="OnlineOpponent.IsFriendly"/>)
        /// — those never touch Elo or the online win count, exactly like a
        /// Vs-AI match — and, separately, excludes a guest with no linked
        /// account (<see cref="PlayerSession.HasAccount"/>), enforced inside
        /// <see cref="PlayerSession.RecordMatchResult"/> itself.
        /// </para>
        /// </summary>
        private void RecordMatchResultIfApplicable()
        {
            if (_gameController.Mode == GameMode.LocalPvP || _playerSession == null || _playerSession.Session == null)
            {
                return;
            }

            PlayerSession session = _playerSession.Session;
            bool isFriendly = _gameController.CurrentOpponent?.IsFriendly ?? false;
            bool isOnline = _gameController.Mode == GameMode.OnlinePvP && !isFriendly;
            int opponentRating = _gameController.CurrentOpponent?.Rating ?? 0;
            MatchOutcome outcome = DetermineOutcome(_gameController.Game.GetWinner());

            int previousRating = session.Rating;
            int delta = session.RecordMatchResult(outcome, isOnline, opponentRating);

            if (isOnline && session.HasAccount && _ratingChangeLabel != null)
            {
                string sign = delta >= 0 ? "+" : "";
                _ratingChangeLabel.text = string.Format(
                    _localization.Service.Get(LocalizationKey.RankingRatingChangeFormat),
                    previousRating, session.Rating, $"{sign}{delta}");
                _ratingChangeLabel.gameObject.SetActive(true);
            }
        }

        /// <summary>
        /// Credits the soft-currency reward for the just-finished match —
        /// unlike <see cref="RecordMatchResultIfApplicable"/>, this runs for
        /// every mode including local pass-and-play, since the reward
        /// always lands in this single session's wallet no matter which
        /// local color technically won. See
        /// <see cref="PlayerSession.GrantMatchReward"/> for the guest cap
        /// and <see cref="MatchRewardCalculator"/> for the reward formula.
        /// </summary>
        private void GrantMatchCurrencyReward()
        {
            if (_playerSession == null || _playerSession.Session == null || _gameController.Game == null)
            {
                return;
            }

            RewardMatchMode rewardMode = _gameController.Mode switch
            {
                GameMode.LocalPvP => RewardMatchMode.LocalPvP,
                GameMode.VsAI => RewardMatchMode.VsAI,
                _ => RewardMatchMode.OnlinePvP
            };

            MatchOutcome outcome = DetermineOutcome(_gameController.Game.GetWinner());

            // A resignation always pays nothing — but who "resigned" from
            // the reward's point of view depends on the mode. In LocalPvP
            // there's one wallet regardless of which local color quit, so
            // any resignation zeroes it; in every other mode only a human
            // ever clicks Resign, so a resignation win (outcome == Win) can
            // only mean the *opponent* quit — a clean win, not a payout of 0.
            bool resigned = _gameController.EndReason == MatchEndReason.Resignation
                && (_gameController.Mode == GameMode.LocalPvP || outcome != MatchOutcome.Win);
            bool forcedEnd = _gameController.EndReason != MatchEndReason.Natural;
            Player opponentColor = _gameController.HumanPlayer == Player.Black ? Player.White : Player.Black;

            int reward = _playerSession.Session.GrantMatchReward(
                rewardMode, outcome, resigned, _gameController.AiDifficulty,
                _gameController.Game.State.DiscCount(_gameController.HumanPlayer),
                _gameController.Game.State.DiscCount(opponentColor),
                forcedEnd);

            if (_matchResultRewardLabel != null)
            {
                _matchResultRewardLabel.gameObject.SetActive(reward > 0);
                if (reward > 0)
                {
                    _matchResultRewardLabel.text = string.Format(
                        _localization.Service.Get(LocalizationKey.DailyRewardPreviewFormat), reward);
                }
            }
        }

        /// <summary>
        /// Fills in and shows the big post-match feedback overlay: who won
        /// (by name) and why — a natural finish, or a resignation, told
        /// apart from the opponent's point of view when there is one. The
        /// currency and rating lines are filled in separately, by
        /// <see cref="GrantMatchCurrencyReward"/> and
        /// <see cref="RecordMatchResultIfApplicable"/>, since both already
        /// compute those numbers for their own purposes.
        /// </summary>
        private void RefreshMatchResultPanel()
        {
            if (_matchResultPanel == null || _localization == null || _gameController.Game == null)
            {
                return;
            }

            Player? winner = _gameController.Game.GetWinner();
            bool isResignation = _gameController.EndReason == MatchEndReason.Resignation;
            string title;
            string subtitle;

            if (_gameController.Mode == GameMode.LocalPvP)
            {
                // No single "the player" here — both sides are local, so the
                // feedback narrates by piece color instead of win/loss.
                if (winner.HasValue)
                {
                    title = string.Format(_localization.Service.Get(LocalizationKey.GameResultWinnerSubtitleFormat), ResolveDisplayName(winner.Value));
                    Player resignedPlayer = winner.Value == Player.Black ? Player.White : Player.Black;
                    subtitle = isResignation
                        ? string.Format(_localization.Service.Get(LocalizationKey.GameResultLocalResignedFormat), ResolveDisplayName(resignedPlayer))
                        : string.Empty;
                }
                else
                {
                    title = _localization.Service.Get(LocalizationKey.GameResultDrawTitle);
                    subtitle = string.Empty;
                }
            }
            else
            {
                MatchOutcome outcome = DetermineOutcome(winner);

                if (isResignation)
                {
                    // Only the local human ever calls Resign() outside
                    // LocalPvP (the AI never resigns), so a resignation that
                    // still comes out as a Win can only mean the opponent
                    // quit — never the player themselves.
                    bool opponentResigned = outcome == MatchOutcome.Win;
                    title = _localization.Service.Get(opponentResigned ? LocalizationKey.GameResultVictoryTitle : LocalizationKey.GameResultDefeatTitle);
                    subtitle = _localization.Service.Get(opponentResigned ? LocalizationKey.GameResultOpponentResignedSubtitle : LocalizationKey.GameResultYouResignedSubtitle);
                }
                else
                {
                    title = outcome switch
                    {
                        MatchOutcome.Win => _localization.Service.Get(LocalizationKey.GameResultVictoryTitle),
                        MatchOutcome.Loss => _localization.Service.Get(LocalizationKey.GameResultDefeatTitle),
                        _ => _localization.Service.Get(LocalizationKey.GameResultDrawTitle)
                    };
                    subtitle = outcome == MatchOutcome.Draw || !winner.HasValue
                        ? string.Empty
                        : string.Format(_localization.Service.Get(LocalizationKey.GameResultWinnerSubtitleFormat), ResolveDisplayName(winner.Value));
                }
            }

            if (_matchResultTitleLabel != null)
            {
                _matchResultTitleLabel.text = title;
            }

            if (_matchResultSubtitleLabel != null)
            {
                _matchResultSubtitleLabel.text = subtitle;
                _matchResultSubtitleLabel.gameObject.SetActive(!string.IsNullOrEmpty(subtitle));
            }

            if (_matchResultRewardLabel != null)
            {
                _matchResultRewardLabel.gameObject.SetActive(false);
            }

            if (_ratingChangeLabel != null)
            {
                _ratingChangeLabel.gameObject.SetActive(false);
            }

            _matchResultPanel.SetActive(true);
        }

        /// <summary>
        /// The name to show for whichever side of the board <paramref name="player"/>
        /// is — the session's own display name or the matched/AI opponent's
        /// name when there is a real identity for that side, falling back to
        /// the plain "Black"/"White" label (e.g. for LocalPvP, or before an
        /// identity is available).
        /// </summary>
        private string ResolveDisplayName(Player player)
        {
            if (_gameController.Mode != GameMode.LocalPvP)
            {
                if (player == _gameController.HumanPlayer)
                {
                    return _playerSession?.Session?.DisplayName ?? PlayerName(player);
                }

                return _gameController.CurrentOpponent?.DisplayName ?? _gameController.OpponentDisplayName ?? PlayerName(player);
            }

            return PlayerName(player);
        }

        private void SubscribeToAchievements()
        {
            if (_subscribedToAchievements || _playerSession == null || _playerSession.Session == null)
            {
                return;
            }

            _playerSession.Session.AchievementUnlocked += OnAchievementUnlocked;
            _subscribedToAchievements = true;
        }

        private void OnAchievementUnlocked(AchievementDefinition achievement)
        {
            if (_achievementToastLabel == null)
            {
                return;
            }

            string name = _localization.Service.Get(achievement.NameKey);
            string line = string.Format(
                _localization.Service.Get(LocalizationKey.AchievementUnlockedFormat), name, achievement.RewardSoftCurrency);

            _achievementToastLabel.text = _achievementToastLabel.gameObject.activeSelf
                ? $"{_achievementToastLabel.text}\n{line}"
                : line;
            _achievementToastLabel.gameObject.SetActive(true);
        }

        private MatchOutcome DetermineOutcome(Player? winner)
        {
            if (!winner.HasValue)
            {
                return MatchOutcome.Draw;
            }

            return winner.Value == _gameController.HumanPlayer ? MatchOutcome.Win : MatchOutcome.Loss;
        }

        private void RefreshHud()
        {
            OthelloGame game = _gameController.Game;
            if (game == null)
            {
                return;
            }

            if (_scoreLabel != null)
            {
                _scoreLabel.text = string.Format(
                    _localization.Service.Get(LocalizationKey.GameScoreFormat),
                    PlayerName(Player.Black), game.State.DiscCount(Player.Black),
                    game.State.DiscCount(Player.White), PlayerName(Player.White));
            }

            if (_turnLabel != null)
            {
                _turnLabel.text = DescribeTurn(game);
            }

            // Resign and "back to menu" share the same spot on screen (see
            // OthelloUiBuilder.BuildGame) and are never both relevant at
            // once: there's nothing to resign from once the match is over,
            // and there's nowhere to go back to from mid-match.
            if (_resignButton != null)
            {
                _resignButton.gameObject.SetActive(!game.IsGameOver);
            }

            if (_backToMenuButton != null)
            {
                _backToMenuButton.gameObject.SetActive(game.IsGameOver);
            }
        }

        private string DescribeTurn(OthelloGame game)
        {
            if (game.IsGameOver)
            {
                Player? winner = game.GetWinner();
                return winner.HasValue
                    ? string.Format(_localization.Service.Get(LocalizationKey.GameOverWinnerFormat), PlayerName(winner.Value))
                    : _localization.Service.Get(LocalizationKey.GameOverDraw);
            }

            if (_gameController.Mode != GameMode.LocalPvP && game.State.CurrentPlayer != _gameController.HumanPlayer)
            {
                return _localization.Service.Get(LocalizationKey.GameAiThinking);
            }

            return string.Format(_localization.Service.Get(LocalizationKey.GameTurnFormat), PlayerName(game.State.CurrentPlayer));
        }

        private string PlayerName(Player player) => _localization.Service.Get(
            player == Player.Black ? LocalizationKey.PlayerBlack : LocalizationKey.PlayerWhite);
    }
}

using Othello.Services;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Othello.Unity
{
    /// <summary>
    /// The game's hub screen: shows the player's name, avatar-linked
    /// identity and currency balances, and is the entry point to every
    /// other screen — playing a match, the shop, customization, settings,
    /// and the ranking screen.
    /// <para>
    /// Reads <see cref="PlayerSessionController.Session"/> and
    /// <see cref="MonetizationController.State"/> defensively instead of
    /// assuming they already exist: both are only guaranteed to exist after
    /// their own components' <c>Start</c> has run, and Unity does not order
    /// <c>Start</c> across different components — the same problem
    /// <see cref="ShopController"/> solves the same way, by connecting
    /// lazily rather than eagerly.
    /// </para>
    /// </summary>
    public sealed class MainMenuScreen : UIScreen
    {
        [SerializeField]
        private PlayerSessionController _playerSession;

        [SerializeField]
        private MonetizationController _monetization;

        [SerializeField]
        private LocalizationController _localization;

        [SerializeField]
        private LeaderboardController _leaderboard;

        [SerializeField]
        private Image _avatarImage;

        [SerializeField]
        private TMP_Text _playerNameLabel;

        [SerializeField]
        private Image _rankIcon;

        [SerializeField]
        private TMP_Text _rankLabel;

        [SerializeField]
        private Image _softCurrencyIcon;

        [SerializeField]
        private TMP_Text _softBalanceLabel;

        [SerializeField]
        private Image _hardCurrencyIcon;

        [SerializeField]
        private TMP_Text _hardBalanceLabel;

        [SerializeField]
        private Button _playButton;

        [SerializeField]
        private Button _shopButton;

        [SerializeField]
        private Button _customizeButton;

        [SerializeField]
        private Button _rankingButton;

        [SerializeField]
        private Button _achievementsButton;

        [SerializeField]
        private Button _dailyRewardButton;

        [SerializeField]
        private Button _settingsButton;

        [SerializeField]
        private Button _quitButton;

        private bool _connected;

        private void Awake()
        {
            // The currency icons never change, so they're set once here
            // rather than on every RefreshLabels call.
            SetIconIfAvailable(_softCurrencyIcon, CosmeticArt.LoadSoftCurrencyIcon());
            SetIconIfAvailable(_hardCurrencyIcon, CosmeticArt.LoadHardCurrencyIcon());

            _playButton?.onClick.AddListener(() => Manager.Show<PlayModeScreen>());
            _shopButton?.onClick.AddListener(() => Manager.Show<ShopView>());
            _customizeButton?.onClick.AddListener(() => Manager.Show<CustomizationScreen>());
            _rankingButton?.onClick.AddListener(() => Manager.Show<RankingScreen>());
            _achievementsButton?.onClick.AddListener(() => Manager.Show<AchievementsScreen>());
            _dailyRewardButton?.onClick.AddListener(() => Manager.Show<DailyRewardScreen>());
            _settingsButton?.onClick.AddListener(() => Manager.Show<SettingsScreen>());

            if (_quitButton != null)
            {
                // "Sair" só faz sentido em desktop; um app mobile não tem esse conceito.
                bool isDesktop = Application.platform != RuntimePlatform.Android &&
                                  Application.platform != RuntimePlatform.IPhonePlayer;
                _quitButton.gameObject.SetActive(isDesktop);
                _quitButton.onClick.AddListener(Application.Quit);
            }
        }

        private void Update()
        {
            if (!_connected)
            {
                TryConnect();
            }
        }

        protected override void OnShown()
        {
            TryConnect();
            RefreshLabels();
        }

        private void TryConnect()
        {
            if (_connected || _playerSession == null || _playerSession.Session == null ||
                _monetization == null || _localization == null)
            {
                return;
            }

            _playerSession.Session.Changed += RefreshLabels;
            _monetization.State.Wallet.Changed += RefreshLabels;
            _localization.Service.Changed += RefreshLabels;
            _connected = true;
            RefreshLabels();
        }

        private void RefreshLabels()
        {
            var session = _playerSession != null ? _playerSession.Session : null;
            var wallet = _monetization != null ? _monetization.State.Wallet : null;

            if (_playerNameLabel != null)
            {
                _playerNameLabel.text = session != null ? session.DisplayName : "...";
            }

            SetIconIfAvailable(_avatarImage, session != null ? CosmeticArt.LoadAvatar(session.AvatarId) : null);

            if (_softBalanceLabel != null)
            {
                _softBalanceLabel.text = wallet != null
                    ? $"{_localization.Service.Get(LocalizationKey.CurrencySoftLabel)}: {wallet.SoftBalance}"
                    : "-";
            }

            if (_hardBalanceLabel != null)
            {
                _hardBalanceLabel.text = wallet != null
                    ? $"{_localization.Service.Get(LocalizationKey.CurrencyHardLabel)}: {wallet.HardBalance}"
                    : "-";
            }

            RefreshRank(session);
        }

        /// <summary>
        /// Shows the player's own rank tier icon and "#n" position, read
        /// from a 1-entry leaderboard query centered on them. The local mock
        /// answers synchronously, but a real backend would not — this
        /// re-reads <see cref="_playerSession"/> instead of trusting
        /// <paramref name="session"/> is still current by the time the
        /// callback runs.
        /// </summary>
        private void RefreshRank(PlayerSession session)
        {
            if (_leaderboard == null || session == null)
            {
                return;
            }

            // A guest (no linked account, see PlayerSession.HasAccount)
            // never enters the ranking system, so there's no meaningful
            // position to show — hide the row instead of querying for one
            // that will never actually change.
            if (!session.HasAccount)
            {
                if (_rankIcon != null)
                {
                    _rankIcon.enabled = false;
                }

                if (_rankLabel != null)
                {
                    _rankLabel.text = string.Empty;
                }

                return;
            }

            _leaderboard.Service.GetRanked(session.DisplayName, session.Rating, windowSize: 1, entries =>
            {
                if (this == null || !gameObject.activeInHierarchy || entries.Count == 0)
                {
                    return;
                }

                LeaderboardEntry ownEntry = entries[0];

                if (_rankIcon != null)
                {
                    Sprite icon = CosmeticArt.LoadRankIcon(ownEntry.Tier);
                    _rankIcon.sprite = icon;
                    _rankIcon.enabled = icon != null;
                }

                if (_rankLabel != null)
                {
                    string tierName = _localization.Service.Get(RankTierCatalog.GetNameKey(ownEntry.Tier));
                    _rankLabel.text = string.Format(
                        _localization.Service.Get(LocalizationKey.RankTierPositionFormat), tierName, ownEntry.Rank);
                }
            });
        }

        /// <summary>
        /// Shows <paramref name="sprite"/> on <paramref name="image"/> when
        /// both exist, otherwise hides the image instead of leaving a blank
        /// placeholder graphic visible.
        /// </summary>
        private static void SetIconIfAvailable(Image image, Sprite sprite)
        {
            if (image == null)
            {
                return;
            }

            image.sprite = sprite;
            image.enabled = sprite != null;
        }
    }
}

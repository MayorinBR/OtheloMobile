using System;
using Othello.Services;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Othello.Unity
{
    /// <summary>
    /// A simple standalone screen for the daily login bonus: shows the
    /// player's current streak, previews what claiming now would grant, and
    /// either lets them claim it or tells them to come back tomorrow.
    /// Rebuilt every time the screen is shown, the same way
    /// <see cref="RankingScreen"/> and <see cref="AchievementsScreen"/>
    /// refresh their own content on <c>OnShown</c> instead of only once.
    /// </summary>
    public sealed class DailyRewardScreen : UIScreen
    {
        [SerializeField]
        private PlayerSessionController _playerSession;

        [SerializeField]
        private LocalizationController _localization;

        [SerializeField]
        private TMP_Text _streakLabel;

        [SerializeField]
        private TMP_Text _previewLabel;

        [SerializeField]
        private Button _claimButton;

        [SerializeField]
        private Button _backButton;

        private void Awake()
        {
            _backButton?.onClick.AddListener(() => Manager.GoBack());
            _claimButton?.onClick.AddListener(Claim);
        }

        protected override void OnShown() => Refresh();

        private void Claim()
        {
            PlayerSession session = _playerSession != null ? _playerSession.Session : null;
            if (session == null)
            {
                return;
            }

            session.TryClaimDailyReward(DateTime.UtcNow, out _);
            Refresh();
        }

        private void Refresh()
        {
            PlayerSession session = _playerSession != null ? _playerSession.Session : null;
            LocalizationService localization = _localization != null ? _localization.Service : null;
            if (session == null || localization == null)
            {
                return;
            }

            DateTime nowUtc = DateTime.UtcNow;
            bool canClaim = session.CanClaimDailyReward(nowUtc);
            DailyRewardTier previewTier = session.PeekNextDailyReward(nowUtc);

            if (_streakLabel != null)
            {
                _streakLabel.text = string.Format(localization.Get(LocalizationKey.DailyRewardStreakFormat), previewTier.Day);
            }

            if (_previewLabel != null)
            {
                _previewLabel.text = previewTier.HardReward > 0
                    ? string.Format(localization.Get(LocalizationKey.DailyRewardPreviewBonusFormat), previewTier.SoftReward, previewTier.HardReward)
                    : string.Format(localization.Get(LocalizationKey.DailyRewardPreviewFormat), previewTier.SoftReward);
            }

            if (_claimButton != null)
            {
                _claimButton.interactable = canClaim;
                TMP_Text claimLabel = _claimButton.GetComponentInChildren<TMP_Text>();
                if (claimLabel != null)
                {
                    claimLabel.text = localization.Get(canClaim
                        ? LocalizationKey.DailyRewardClaimButton
                        : LocalizationKey.DailyRewardAlreadyClaimed);
                }
            }
        }
    }
}

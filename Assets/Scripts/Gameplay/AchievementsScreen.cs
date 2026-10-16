using System.Collections.Generic;
using System.Linq;
using Othello.Services;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Othello.Unity
{
    /// <summary>
    /// Shows every achievement in <see cref="AchievementCatalog"/>, marking
    /// which ones the player has already unlocked. Rebuilt every time the
    /// screen is shown, the same way <see cref="RankingScreen"/> rebuilds
    /// its own procedural list.
    /// </summary>
    public sealed class AchievementsScreen : UIScreen
    {
        [SerializeField]
        private PlayerSessionController _playerSession;

        [SerializeField]
        private LocalizationController _localization;

        [SerializeField]
        private TMP_Text _statsLabel;

        [SerializeField]
        private RectTransform _listContainer;

        [SerializeField]
        private Button _backButton;

        private readonly List<GameObject> _spawnedRows = new();

        private void Awake()
        {
            _backButton?.onClick.AddListener(() => Manager.GoBack());
        }

        protected override void OnShown() => Rebuild();

        private void Rebuild()
        {
            foreach (var row in _spawnedRows)
            {
                Destroy(row);
            }
            _spawnedRows.Clear();

            if (_playerSession == null || _playerSession.Session == null || _localization == null || _listContainer == null)
            {
                return;
            }

            EnsureListContainerLayout();

            PlayerSession session = _playerSession.Session;
            RefreshStatsLabel(session.Stats);

            foreach (var achievement in AchievementCatalog.All)
            {
                bool isUnlocked = session.UnlockedAchievementIds.Contains(achievement.Id);
                _spawnedRows.Add(BuildRow(achievement, isUnlocked));
            }
        }

        /// <summary>
        /// Forces <see cref="_listContainer"/>'s <see cref="VerticalLayoutGroup"/>
        /// to actually control each row's width and height. The scene has it
        /// saved with both controls off, so every row sat at Unity's raw
        /// ~100px default width regardless of how wide this container
        /// actually is — wrapping the achievement text into a tall,
        /// overlapping column instead of one line per row.
        /// </summary>
        private void EnsureListContainerLayout()
        {
            var layoutGroup = _listContainer.GetComponent<VerticalLayoutGroup>();
            if (layoutGroup == null)
            {
                layoutGroup = _listContainer.gameObject.AddComponent<VerticalLayoutGroup>();
            }

            layoutGroup.childControlWidth = true;
            layoutGroup.childControlHeight = true;
            layoutGroup.childForceExpandWidth = true;
            layoutGroup.childForceExpandHeight = false;
            layoutGroup.spacing = 8f;
        }

        private void RefreshStatsLabel(MatchStats stats)
        {
            if (_statsLabel == null)
            {
                return;
            }

            _statsLabel.text = string.Format(
                _localization.Service.Get(LocalizationKey.AchievementsStatsFormat),
                stats.MatchesPlayed, stats.Wins, stats.Losses, stats.Draws, stats.CurrentWinStreak, stats.BestWinStreak);
        }

        private GameObject BuildRow(AchievementDefinition achievement, bool isUnlocked)
        {
            var row = new GameObject($"Achievement_{achievement.Id}", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
            row.transform.SetParent(_listContainer, false);

            // Same fix as ShopView/CustomizationScreen/RankingScreen's
            // BuildRow: an unconfigured HorizontalLayoutGroup leaves its
            // child (here, just the text label) at Unity's own uncontrolled
            // default size instead of filling the row.
            var rowLayout = row.GetComponent<HorizontalLayoutGroup>();
            rowLayout.childControlWidth = true;
            rowLayout.childControlHeight = true;
            rowLayout.childForceExpandWidth = true;
            rowLayout.childForceExpandHeight = false;

            var rowElement = row.GetComponent<LayoutElement>();
            rowElement.preferredHeight = 56f;
            rowElement.flexibleHeight = 0f;

            string name = _localization.Service.Get(achievement.NameKey);
            string status = _localization.Service.Get(
                isUnlocked ? LocalizationKey.AchievementStatusUnlocked : LocalizationKey.AchievementStatusLocked);
            string reward = string.Format(
                _localization.Service.Get(LocalizationKey.AchievementRewardFormat), achievement.RewardSoftCurrency);

            // Locked achievements still show their name and reward, so the
            // player knows what to aim for — only the status text differs.
            string marker = isUnlocked ? "> " : "";
            TMP_Text label = UIWidgets.CreateLabel(row.transform, $"{marker}{name} — {status} — {reward}");
            label.enableWordWrapping = false;
            label.overflowMode = TextOverflowModes.Ellipsis;

            return row;
        }
    }
}

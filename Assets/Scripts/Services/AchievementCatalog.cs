using System.Collections.Generic;

namespace Othello.Services
{
    /// <summary>One unlockable milestone and the Soft-currency reward it grants.</summary>
    public sealed class AchievementDefinition
    {
        public string Id { get; }
        public LocalizationKey NameKey { get; }
        public int RewardSoftCurrency { get; }

        public AchievementDefinition(string id, LocalizationKey nameKey, int rewardSoftCurrency)
        {
            Id = id;
            NameKey = nameKey;
            RewardSoftCurrency = rewardSoftCurrency;
        }
    }

    /// <summary>
    /// Every achievement the game grants. The condition that unlocks each
    /// one lives in <see cref="AchievementEvaluator"/>, kept separate so
    /// this catalog stays plain data — the same split
    /// <see cref="InventoryCatalog"/> uses between an item's definition and
    /// <see cref="ShopService"/>'s purchase rules.
    /// </summary>
    public static class AchievementCatalog
    {
        public const string FirstWin = "first_win";
        public const string WinStreak3 = "win_streak_3";
        public const string WinStreak5 = "win_streak_5";
        public const string TenMatches = "ten_matches";
        public const string FiftyMatches = "fifty_matches";
        public const string FirstOnlineWin = "first_online_win";
        public const string Rating1400 = "rating_1400";
        public const string FullCollection = "full_collection";
        public const string Dedicated = "dedicated_7_day_streak";

        public static readonly IReadOnlyList<AchievementDefinition> All = new[]
        {
            new AchievementDefinition(FirstWin, LocalizationKey.AchievementFirstWin, 20),
            new AchievementDefinition(WinStreak3, LocalizationKey.AchievementWinStreak3, 30),
            new AchievementDefinition(WinStreak5, LocalizationKey.AchievementWinStreak5, 60),
            new AchievementDefinition(TenMatches, LocalizationKey.AchievementTenMatches, 20),
            new AchievementDefinition(FiftyMatches, LocalizationKey.AchievementFiftyMatches, 100),
            new AchievementDefinition(FirstOnlineWin, LocalizationKey.AchievementFirstOnlineWin, 30),
            new AchievementDefinition(Rating1400, LocalizationKey.AchievementRating1400, 50),
            new AchievementDefinition(FullCollection, LocalizationKey.AchievementFullCollection, 50),
            new AchievementDefinition(Dedicated, LocalizationKey.AchievementDedicated, 40)
        };

        public static bool TryFind(string id, out AchievementDefinition definition)
        {
            foreach (var candidate in All)
            {
                if (candidate.Id == id)
                {
                    definition = candidate;
                    return true;
                }
            }

            definition = default;
            return false;
        }
    }
}

using System.Collections.Generic;

namespace Othello.Services
{
    /// <summary>
    /// One day of the daily-login cycle: how much Soft currency it grants,
    /// and Hard currency for the final day of the cycle, as a small
    /// milestone bonus.
    /// </summary>
    public readonly struct DailyRewardTier
    {
        public int Day { get; }
        public int SoftReward { get; }
        public int HardReward { get; }

        public DailyRewardTier(int day, int softReward, int hardReward)
        {
            Day = day;
            SoftReward = softReward;
            HardReward = hardReward;
        }
    }

    /// <summary>
    /// The fixed 7-day reward cycle: each consecutive day of claiming grants
    /// more than the last, day 7 also grants Hard currency, and the cycle
    /// repeats from day 1 after that — kept as plain data, the same split
    /// <see cref="InventoryCatalog"/> and <see cref="AchievementCatalog"/>
    /// use between "what a thing grants" and the rules that decide when it
    /// applies (<see cref="DailyRewardEvaluator"/>).
    /// </summary>
    public static class DailyRewardCatalog
    {
        public static readonly IReadOnlyList<DailyRewardTier> Tiers = new[]
        {
            new DailyRewardTier(1, 20, 0),
            new DailyRewardTier(2, 25, 0),
            new DailyRewardTier(3, 30, 0),
            new DailyRewardTier(4, 40, 0),
            new DailyRewardTier(5, 50, 0),
            new DailyRewardTier(6, 60, 0),
            new DailyRewardTier(7, 100, 10)
        };

        /// <summary>
        /// The tier for <paramref name="streakDay"/>, wrapping back to day 1
        /// once the streak goes past <see cref="Tiers"/>' length so the
        /// cycle repeats indefinitely instead of running out.
        /// </summary>
        public static DailyRewardTier GetTier(int streakDay)
        {
            int index = (streakDay - 1) % Tiers.Count;
            if (index < 0)
            {
                index += Tiers.Count;
            }

            return Tiers[index];
        }
    }
}

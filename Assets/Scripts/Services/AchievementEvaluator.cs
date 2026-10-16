using System.Collections.Generic;

namespace Othello.Services
{
    /// <summary>
    /// Pure function from a player's current progress to which achievements
    /// that progress satisfies. Kept separate from <see cref="PlayerSession"/>
    /// so every unlock condition can be tested directly, without a session,
    /// a wallet, or any save/load machinery.
    /// </summary>
    public static class AchievementEvaluator
    {
        /// <summary>
        /// Returns every achievement id whose condition <paramref name="stats"/>,
        /// <paramref name="rating"/>, the cosmetic-collection progress, and
        /// the daily-login streak currently satisfy — including ones
        /// already unlocked; the caller is responsible for skipping those.
        /// </summary>
        /// <param name="dailyRewardStreak">
        /// The player's current daily-login streak (see <see cref="DailyRewardEvaluator"/>).
        /// Defaults to 0 so every existing call site that predates the
        /// daily-reward feature keeps compiling and behaving exactly as before.
        /// </param>
        public static IReadOnlyList<string> GetSatisfiedIds(
            MatchStats stats, int rating, int unlockedSkinCount, int totalSkinCount, int dailyRewardStreak = 0)
        {
            var satisfied = new List<string>();

            if (stats.Wins >= 1)
            {
                satisfied.Add(AchievementCatalog.FirstWin);
            }
            if (stats.BestWinStreak >= 3)
            {
                satisfied.Add(AchievementCatalog.WinStreak3);
            }
            if (stats.BestWinStreak >= 5)
            {
                satisfied.Add(AchievementCatalog.WinStreak5);
            }
            if (stats.MatchesPlayed >= 10)
            {
                satisfied.Add(AchievementCatalog.TenMatches);
            }
            if (stats.MatchesPlayed >= 50)
            {
                satisfied.Add(AchievementCatalog.FiftyMatches);
            }
            if (stats.OnlineWins >= 1)
            {
                satisfied.Add(AchievementCatalog.FirstOnlineWin);
            }
            if (rating >= 1400)
            {
                satisfied.Add(AchievementCatalog.Rating1400);
            }
            if (totalSkinCount > 0 && unlockedSkinCount >= totalSkinCount)
            {
                satisfied.Add(AchievementCatalog.FullCollection);
            }
            if (dailyRewardStreak >= 7)
            {
                satisfied.Add(AchievementCatalog.Dedicated);
            }

            return satisfied;
        }
    }
}

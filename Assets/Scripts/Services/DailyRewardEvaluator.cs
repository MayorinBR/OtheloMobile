using System;

namespace Othello.Services
{
    /// <summary>
    /// Pure date arithmetic for the daily-login streak: whether today's
    /// reward is still available, and which streak day claiming it now
    /// would be. Kept separate from <see cref="PlayerSession"/> — which
    /// owns the actual claimed state and wallet crediting — the same way
    /// <see cref="AchievementEvaluator"/> is kept separate from the session
    /// it evaluates, so the date logic can be tested without a session, a
    /// wallet, or a clock to mock.
    /// <para>
    /// All comparisons are by calendar date, not elapsed time, so a claim
    /// at 23:59 and the next at 00:01 still count as two different days —
    /// callers pass a UTC "now" so every player's day boundary is the same
    /// moment worldwide, matching how the eventual cloud save timestamps
    /// would work.
    /// </para>
    /// </summary>
    public static class DailyRewardEvaluator
    {
        /// <summary>
        /// True if no reward has been claimed today yet: either there is no
        /// previous claim at all, or its date is strictly before <paramref name="nowUtc"/>'s.
        /// </summary>
        public static bool CanClaim(DateTime? lastClaimUtc, DateTime nowUtc)
        {
            return lastClaimUtc == null || lastClaimUtc.Value.Date < nowUtc.Date;
        }

        /// <summary>
        /// The streak day a claim made "now" would land on: 1 for a
        /// first-ever claim, <paramref name="currentStreakDay"/> + 1 when
        /// the last claim was exactly the day before (wrapping via
        /// <see cref="DailyRewardCatalog.GetTier"/> once past the cycle
        /// length), or a reset back to 1 when a day was missed.
        /// </summary>
        public static int GetNextStreakDay(DateTime? lastClaimUtc, int currentStreakDay, DateTime nowUtc)
        {
            if (lastClaimUtc == null)
            {
                return 1;
            }

            int daysSinceLastClaim = (nowUtc.Date - lastClaimUtc.Value.Date).Days;
            return daysSinceLastClaim == 1 ? currentStreakDay + 1 : 1;
        }
    }
}

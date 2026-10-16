using Othello.AI;

namespace Othello.Services
{
    /// <summary>
    /// Which of the three match modes a reward is being calculated for —
    /// mirrors <c>Othello.Unity.GameMode</c> without this project (or its
    /// tests) needing to depend on Unity.
    /// </summary>
    public enum RewardMatchMode
    {
        LocalPvP,
        VsAI,
        OnlinePvP
    }

    /// <summary>
    /// Computes the soft-currency ("free" gems) reward for a finished
    /// match. Every mode grants something just for playing a match to a
    /// real conclusion — a win pays more than a loss or draw, and exactly
    /// how much more depends on the mode:
    /// <list type="bullet">
    /// <item>Local pass-and-play always pays <see cref="MinReward"/> — there
    /// is no meaningful "difficulty" or "opponent strength" to scale by
    /// when both sides are the same person on the same device.</item>
    /// <item>Vs-AI scales with <see cref="Difficulty"/>: Easy pays the same
    /// floor amount as local PvP, Hard pays as much as the best online
    /// result, Medium sits linearly in between.</item>
    /// <item>Online PvP pays the most on a win, scaled by how one-sided it
    /// was (final disc count against the opponent's) up to
    /// <see cref="MaxReward"/> — except a forced win (the opponent resigned
    /// or timed out), which has no meaningful final disc count to compare
    /// and always pays the max instead.</item>
    /// </list>
    /// A resignation always pays nothing to whoever resigned, regardless of
    /// mode — a match abandoned partway through isn't "played out" the way
    /// a natural or timed-out ending is. Anything else that isn't a win
    /// (a loss, a draw, or a timeout loss) pays the floor amount,
    /// <see cref="MinReward"/>.
    /// </summary>
    public static class MatchRewardCalculator
    {
        /// <summary>The floor reward for completing a match that wasn't a win — or the win bonus a guest account is capped at, see <see cref="PlayerSession.GrantMatchReward"/>.</summary>
        public const int MinReward = 5;

        /// <summary>The richest a single match reward ever gets — a dominant or forced online win, or a Vs-AI win on Hard.</summary>
        public const int MaxReward = 30;

        /// <summary>
        /// Computes the soft-currency reward for one finished match from
        /// the local player's perspective.
        /// </summary>
        /// <param name="mode">Which mode the match was played in.</param>
        /// <param name="outcome">How the match ended for the local player.</param>
        /// <param name="resigned">True if the local player is the one who resigned — always pays 0 regardless of anything else.</param>
        /// <param name="difficulty">The AI difficulty faced; only used when <paramref name="mode"/> is <see cref="RewardMatchMode.VsAI"/>.</param>
        /// <param name="playerDiscs">The local player's final disc count; only used for an online win.</param>
        /// <param name="opponentDiscs">The opponent's final disc count; only used for an online win.</param>
        /// <param name="forcedEnd">True if the match ended via resignation or a turn timeout rather than running out of legal moves naturally — only relevant for an online win, where it means the disc counts aren't a meaningful margin to scale by.</param>
        public static int CalculateSoftReward(
            RewardMatchMode mode, MatchOutcome outcome, bool resigned, Difficulty difficulty = Difficulty.Easy,
            int playerDiscs = 0, int opponentDiscs = 0, bool forcedEnd = false)
        {
            if (resigned)
            {
                return 0;
            }

            if (outcome != MatchOutcome.Win)
            {
                return MinReward;
            }

            return mode switch
            {
                RewardMatchMode.LocalPvP => MinReward,
                RewardMatchMode.VsAI => RewardForDifficulty(difficulty),
                RewardMatchMode.OnlinePvP => RewardForOnlineWin(playerDiscs, opponentDiscs, forcedEnd),
                _ => MinReward
            };
        }

        /// <summary>Easy pays <see cref="MinReward"/>, Hard pays <see cref="MaxReward"/>, Medium sits linearly in between.</summary>
        private static int RewardForDifficulty(Difficulty difficulty)
        {
            const int highestIndex = (int)Difficulty.Hard;
            float t = highestIndex == 0 ? 1f : (float)(int)difficulty / highestIndex;
            return MinReward + (int)System.Math.Round((MaxReward - MinReward) * t);
        }

        private static int RewardForOnlineWin(int playerDiscs, int opponentDiscs, bool forcedEnd)
        {
            if (forcedEnd)
            {
                return MaxReward;
            }

            int margin = playerDiscs - opponentDiscs;
            float marginFraction = Clamp01(margin / 64f);
            return MinReward + (int)System.Math.Round((MaxReward - MinReward) * marginFraction);
        }

        private static float Clamp01(float value) => value < 0f ? 0f : value > 1f ? 1f : value;
    }
}

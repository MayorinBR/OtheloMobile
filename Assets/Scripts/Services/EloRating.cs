using System;

namespace Othello.Services
{
    /// <summary>How a finished match ended, from one player's point of view.</summary>
    public enum MatchOutcome
    {
        Win,
        Loss,
        Draw
    }

    /// <summary>
    /// Standard chess-style Elo rating math: the same formula
    /// <see cref="ILeaderboardService"/> and <see cref="PlayerSession"/> use
    /// to keep every player's rating comparable regardless of who they
    /// happened to play. Pure and stateless on purpose, so it needs no mock
    /// to unit test and no change at all once real opponents (via
    /// <see cref="Othello.Unity.FirebaseOnlineMatchService"/>) replace
    /// today's locally-simulated ones.
    /// </summary>
    public static class EloRating
    {
        /// <summary>Starting rating for a player with no match history yet.</summary>
        public const int DefaultRating = 1200;

        private const double KFactor = 32.0;

        /// <summary>
        /// Returns the rating <paramref name="rating"/> becomes after a match
        /// against a player rated <paramref name="opponentRating"/>, ending
        /// in <paramref name="outcome"/>.
        /// </summary>
        /// <remarks>
        /// Theory: Elo assumes each player has a "true" strength and models
        /// the chance of winning as a logistic curve of the rating gap. A
        /// result better than that expected chance nudges the rating up; a
        /// result worse nudges it down. The <see cref="KFactor"/> caps how
        /// big one match can move the needle, so a rating converges over
        /// many games instead of swinging wildly from a single upset.
        /// </remarks>
        public static int CalculateNewRating(int rating, int opponentRating, MatchOutcome outcome)
        {
            double expectedScore = 1.0 / (1.0 + Math.Pow(10.0, (opponentRating - rating) / 400.0));
            double actualScore = outcome switch
            {
                MatchOutcome.Win => 1.0,
                MatchOutcome.Draw => 0.5,
                MatchOutcome.Loss => 0.0,
                _ => throw new ArgumentOutOfRangeException(nameof(outcome))
            };

            int delta = (int)Math.Round(KFactor * (actualScore - expectedScore), MidpointRounding.AwayFromZero);
            return rating + delta;
        }
    }
}

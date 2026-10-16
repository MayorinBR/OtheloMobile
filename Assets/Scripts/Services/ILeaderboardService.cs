using System;
using System.Collections.Generic;

namespace Othello.Services
{
    /// <summary>One row of a ranked standings list.</summary>
    public sealed class LeaderboardEntry
    {
        public int Rank { get; }
        public string DisplayName { get; }
        public int Rating { get; }
        public bool IsLocalPlayer { get; }

        /// <summary>The rank tier this entry displays as — see <see cref="RankTierCatalog.GetTier"/>.</summary>
        public RankTier Tier { get; }

        public LeaderboardEntry(int rank, string displayName, int rating, bool isLocalPlayer)
        {
            Rank = rank;
            DisplayName = displayName;
            Rating = rating;
            IsLocalPlayer = isLocalPlayer;
            Tier = RankTierCatalog.GetTier(rating, rank);
        }
    }

    /// <summary>
    /// Facade over the real ranked leaderboard (a query against every
    /// player's stored <see cref="PlayerProfileData.Rating"/> once a real
    /// backend exists). Named around what the Ranking screen needs — the
    /// standings around one player — rather than the query shape a specific
    /// backend would use.
    /// <para>
    /// Callback-based, like every other facade in the project
    /// (<see cref="IAuthService"/>, <see cref="ICloudSaveService"/>, and so
    /// on) — a real backend query (a Firestore range query, for instance)
    /// cannot return its result synchronously the way
    /// <see cref="LocalLeaderboardService"/> does today.
    /// </para>
    /// </summary>
    public interface ILeaderboardService
    {
        /// <summary>
        /// Fetches up to <paramref name="windowSize"/> ranked entries
        /// centered on where <paramref name="localPlayerRating"/> currently
        /// places, and passes them to <paramref name="onCompleted"/>.
        /// </summary>
        void GetRanked(string localPlayerName, int localPlayerRating, int windowSize, Action<IReadOnlyList<LeaderboardEntry>> onCompleted);
    }
}

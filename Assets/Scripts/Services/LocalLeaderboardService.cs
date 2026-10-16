using System;
using System.Collections.Generic;

namespace Othello.Services
{
    /// <summary>
    /// Local stand-in for a real ranked leaderboard query: ranks the player
    /// against a small fixed pool of synthetic opponents instead of every
    /// other real player's stored rating. Swap this for the real
    /// implementation behind <see cref="ILeaderboardService"/> once a backend
    /// tracks every player's <see cref="PlayerProfileData.Rating"/>; the
    /// Ranking screen does not need to change.
    /// </summary>
    public sealed class LocalLeaderboardService : ILeaderboardService
    {
        private static readonly (string Name, int Rating)[] SyntheticPlayers =
        {
            ("Aiko", 1900), ("Marcus", 1850), ("Sofia", 1800), ("Kenji", 1750),
            ("Elena", 1700), ("Diego", 1650), ("Priya", 1600), ("Noah", 1550),
            ("Yuki", 1500), ("Liam", 1450), ("Mei", 1400), ("Lucas", 1350),
            ("Ines", 1300), ("Ravi", 1250), ("Chloe", 1200), ("Omar", 1150),
            ("Anna", 1100), ("Felix", 1050), ("Nora", 1000), ("Theo", 950)
        };

        /// <summary>
        /// Ranks the player against the synthetic pool and invokes
        /// <paramref name="onCompleted"/> immediately with the result —
        /// there is no real query to wait on, but the callback shape still
        /// matches <see cref="ILeaderboardService"/> so a real backend can
        /// replace this without <see cref="RankingScreen"/> changing.
        /// </summary>
        public void GetRanked(string localPlayerName, int localPlayerRating, int windowSize, Action<IReadOnlyList<LeaderboardEntry>> onCompleted)
        {
            var players = new List<(string Name, int Rating, bool IsLocal)>(SyntheticPlayers.Length + 1);
            foreach (var synthetic in SyntheticPlayers)
            {
                players.Add((synthetic.Name, synthetic.Rating, false));
            }
            players.Add((string.IsNullOrEmpty(localPlayerName) ? "You" : localPlayerName, localPlayerRating, true));

            players.Sort((a, b) => b.Rating != a.Rating
                ? b.Rating.CompareTo(a.Rating)
                : string.CompareOrdinal(a.Name, b.Name));

            var ranked = new List<LeaderboardEntry>(players.Count);
            int localRank = 0;
            for (int i = 0; i < players.Count; i++)
            {
                ranked.Add(new LeaderboardEntry(i + 1, players[i].Name, players[i].Rating, players[i].IsLocal));
                if (players[i].IsLocal)
                {
                    localRank = i;
                }
            }

            onCompleted?.Invoke(Window(ranked, localRank, windowSize));
        }

        private static IReadOnlyList<LeaderboardEntry> Window(List<LeaderboardEntry> ranked, int centerIndex, int windowSize)
        {
            if (windowSize >= ranked.Count)
            {
                return ranked;
            }

            int half = windowSize / 2;
            int start = Math.Clamp(centerIndex - half, 0, ranked.Count - windowSize);
            return ranked.GetRange(start, windowSize);
        }
    }
}

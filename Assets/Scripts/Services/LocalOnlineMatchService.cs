using System;

namespace Othello.Services
{
    /// <summary>
    /// Local stand-in for the real online matchmaking backend: after a short
    /// simulated search, "finds" a synthetic opponent picked to be close in
    /// rating to the searching player, from a small fixed pool. Enough to
    /// exercise the full matchmaking UI flow — searching, cancelling,
    /// finding an opponent, playing the match, updating the rating — without
    /// any real network or a second device. Swap this for the real
    /// implementation behind <see cref="IOnlineMatchService"/> — see
    /// <see cref="Othello.Unity.FirebaseOnlineMatchService"/> — once it's
    /// wired in; nothing that calls this interface needs to change.
    /// </summary>
    public sealed class LocalOnlineMatchService : IOnlineMatchService
    {
        private static readonly (string Name, int Rating, string AvatarId)[] OpponentPool =
        {
            ("Aiko", 1900, InventoryCatalog.AvatarFox), ("Marcus", 1700, InventoryCatalog.AvatarOwl),
            ("Sofia", 1500, InventoryCatalog.AvatarCat), ("Kenji", 1300, InventoryCatalog.AvatarPanda),
            ("Elena", 1150, InventoryCatalog.AvatarFox), ("Diego", 1000, InventoryCatalog.AvatarOwl),
            ("Priya", 1600, InventoryCatalog.AvatarCat), ("Noah", 1400, InventoryCatalog.AvatarPanda),
            ("Mei", 1250, InventoryCatalog.AvatarFox), ("Lucas", 1050, InventoryCatalog.AvatarOwl)
        };

        private const float SearchDurationSeconds = 1.5f;

        private float _elapsedSeconds;
        private int _nextPoolIndex;
        private int _searchingRating;
        private bool _isFriendlySearch;

        public MatchmakingStatus Status { get; private set; } = MatchmakingStatus.Idle;

        public event Action<OnlineOpponent> MatchFound;

        public void FindMatch(string displayName, int rating)
        {
            _isFriendlySearch = false;
            _searchingRating = rating;
            _elapsedSeconds = 0f;
            Status = MatchmakingStatus.Searching;
        }

        /// <summary>
        /// This local stand-in has no real second player to wait for, so it
        /// simulates a friendly match the same way it simulates a ranked one
        /// — a short search that "finds" a pool opponent, just flagged as
        /// friendly (see <see cref="OnlineOpponent.IsFriendly"/>) and anchored
        /// at the default rating, since a friendly match isn't looking for a
        /// close rating match.
        /// </summary>
        public string CreateFriendlyMatch(string displayName)
        {
            _isFriendlySearch = true;
            _searchingRating = EloRating.DefaultRating;
            _elapsedSeconds = 0f;
            Status = MatchmakingStatus.Searching;
            return RoomCodeGenerator.Generate();
        }

        /// <summary>See <see cref="CreateFriendlyMatch"/> — this local stand-in doesn't validate the code against a real room, it just starts the same simulated friendly search.</summary>
        public void JoinFriendlyMatch(string displayName, string roomCode)
        {
            _isFriendlySearch = true;
            _searchingRating = EloRating.DefaultRating;
            _elapsedSeconds = 0f;
            Status = MatchmakingStatus.Searching;
        }

        public void CancelMatchmaking()
        {
            if (Status == MatchmakingStatus.Searching)
            {
                Status = MatchmakingStatus.Idle;
            }
        }

        public void LeaveMatch()
        {
            Status = MatchmakingStatus.Idle;
        }

        public void Tick(float deltaTimeSeconds)
        {
            if (Status != MatchmakingStatus.Searching)
            {
                return;
            }

            _elapsedSeconds += deltaTimeSeconds;
            if (_elapsedSeconds < SearchDurationSeconds)
            {
                return;
            }

            OnlineOpponent opponent = PickOpponent(_searchingRating, _isFriendlySearch);
            Status = MatchmakingStatus.InMatch;
            MatchFound?.Invoke(opponent);
        }

        /// <summary>
        /// Picks the pool entry closest in rating to <paramref name="aroundRating"/>,
        /// rotating the search start each call so repeated matches don't
        /// always return the same opponent when several are equally close.
        /// </summary>
        private OnlineOpponent PickOpponent(int aroundRating, bool isFriendly)
        {
            (string Name, int Rating, string AvatarId) best = OpponentPool[0];
            int bestDiff = int.MaxValue;

            for (int offset = 0; offset < OpponentPool.Length; offset++)
            {
                var candidate = OpponentPool[(_nextPoolIndex + offset) % OpponentPool.Length];
                int diff = Math.Abs(candidate.Rating - aroundRating);
                if (diff < bestDiff)
                {
                    bestDiff = diff;
                    best = candidate;
                }
            }

            _nextPoolIndex = (_nextPoolIndex + 1) % OpponentPool.Length;
            return new OnlineOpponent(best.Name, best.Rating, best.AvatarId, isFriendly);
        }
    }
}

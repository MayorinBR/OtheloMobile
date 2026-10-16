namespace Othello.Services
{
    /// <summary>
    /// Identity of a matched online opponent — just enough to show who the
    /// player is facing and to pick a fair AI stand-in strength until real
    /// remote players (via <see cref="Othello.Unity.FirebaseOnlineMatchService"/>)
    /// replace today's local simulation.
    /// </summary>
    public sealed class OnlineOpponent
    {
        public string DisplayName { get; }
        public int Rating { get; }

        /// <summary>
        /// One of the <see cref="InventoryCatalog"/> avatar item ids.
        /// Defaults to <see cref="InventoryCatalog.AvatarDefault"/> —
        /// today's matchmaking implementations don't exchange a real
        /// avatar choice yet, so this is a placeholder until they do.
        /// </summary>
        public string AvatarId { get; }

        /// <summary>
        /// True for a "friendly" match created or joined via a room code
        /// (see <see cref="IOnlineMatchService.CreateFriendlyMatch"/>) —
        /// still a real online match, but one that never touches either
        /// player's Elo rating or the ranking system.
        /// </summary>
        public bool IsFriendly { get; }

        public OnlineOpponent(string displayName, int rating, string avatarId = InventoryCatalog.AvatarDefault, bool isFriendly = false)
        {
            DisplayName = displayName;
            Rating = rating;
            AvatarId = avatarId;
            IsFriendly = isFriendly;
        }
    }
}

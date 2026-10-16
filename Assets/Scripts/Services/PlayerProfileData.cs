using System;
using System.Collections.Generic;

namespace Othello.Services
{
    /// <summary>
    /// Plain, serializable shape of what gets saved to and loaded from the
    /// cloud — mirrors the <c>users/{userId}</c> document the project's
    /// Firestore design calls for. Deliberately has no behavior: the rules
    /// live in <see cref="MonetizationState"/> and <see cref="PlayerSession"/>,
    /// this type only carries data across that boundary.
    /// </summary>
    public sealed class PlayerProfileData
    {
        public string UserId { get; set; }
        public string DisplayName { get; set; } = "Player";
        public string AvatarId { get; set; } = "default";
        public List<string> UnlockedSkinIds { get; set; } = new();
        public int SoftBalance { get; set; }
        public int HardBalance { get; set; }
        public bool IsNoAdsPurchased { get; set; }
        public string EquippedPieceSkinId { get; set; }
        public string EquippedBoardSkinId { get; set; }
        public Language Language { get; set; } = Language.English;
        public int Rating { get; set; } = EloRating.DefaultRating;
        public int MatchesPlayed { get; set; }
        public int Wins { get; set; }
        public int Losses { get; set; }
        public int Draws { get; set; }
        public int OnlineWins { get; set; }
        public int CurrentWinStreak { get; set; }
        public int BestWinStreak { get; set; }
        public List<string> UnlockedAchievementIds { get; set; } = new();
        public int DailyRewardStreak { get; set; }
        public DateTime? LastDailyRewardClaimUtc { get; set; }
    }
}

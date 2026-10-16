using System;

namespace Othello.Services
{
    /// <summary>Where matchmaking currently stands.</summary>
    public enum MatchmakingStatus
    {
        /// <summary>No search in progress; <see cref="IOnlineMatchService.FindMatch"/> can be called.</summary>
        Idle,

        /// <summary>Looking for an opponent; <see cref="IOnlineMatchService.CancelMatchmaking"/> can stop it.</summary>
        Searching,

        /// <summary>An opponent was found and handed off via <see cref="IOnlineMatchService.MatchFound"/>.</summary>
        InMatch
    }

    /// <summary>
    /// Facade over the real matchmaking backend (a Cloud Firestore queue and
    /// match documents — see <see cref="Othello.Unity.FirebaseOnlineMatchService"/>),
    /// scoped deliberately narrow: it only finds an opponent and reports who
    /// they are. Once found, the match itself plays out through
    /// <see cref="GameController"/> like any other match — this interface
    /// has no move-relay API of its own, since
    /// <see cref="LocalOnlineMatchService"/> stands in for a real remote
    /// opponent by generating that opponent's moves locally today. When the
    /// real backend is wired in, that responsibility moves onto networked
    /// move relay instead, but this contract — find an opponent, report
    /// them, allow cancelling — does not need to change.
    /// </summary>
    public interface IOnlineMatchService
    {
        MatchmakingStatus Status { get; }

        /// <summary>Starts searching for an opponent near <paramref name="rating"/>.</summary>
        void FindMatch(string displayName, int rating);

        /// <summary>
        /// Creates a private "friendly" match and starts waiting for a second
        /// player to join it — no rating-based matchmaking, and the result
        /// never touches Elo (see <see cref="OnlineOpponent.IsFriendly"/>).
        /// </summary>
        /// <returns>A short room code to share with whoever should join via <see cref="JoinFriendlyMatch"/>.</returns>
        string CreateFriendlyMatch(string displayName);

        /// <summary>Joins a friendly match previously created with <see cref="CreateFriendlyMatch"/>, by its room code.</summary>
        void JoinFriendlyMatch(string displayName, string roomCode);

        /// <summary>Stops an in-progress search. No-op if not currently searching.</summary>
        void CancelMatchmaking();

        /// <summary>Returns matchmaking to <see cref="MatchmakingStatus.Idle"/> once the found match has been handed off and started.</summary>
        void LeaveMatch();

        /// <summary>Advances matchmaking by <paramref name="deltaTimeSeconds"/>; call this once per frame.</summary>
        void Tick(float deltaTimeSeconds);

        /// <summary>Raised once an opponent is found for the current search.</summary>
        event Action<OnlineOpponent> MatchFound;
    }
}

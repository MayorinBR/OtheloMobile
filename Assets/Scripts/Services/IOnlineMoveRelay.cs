using System;

namespace Othello.Services
{
    /// <summary>
    /// Relays moves between the two real players of an online match once
    /// they've been matched — the piece <see cref="IOnlineMatchService"/>'s
    /// own documentation calls out as deliberately outside its scope: that
    /// interface only finds an opponent and reports who they are, this one
    /// carries the moves once the match is actually being played.
    /// <para>
    /// <see cref="GameController"/> only depends on this interface, never on
    /// Firestore directly, so the engine and every screen stay testable
    /// without a network; passing no relay for an online match keeps
    /// today's behavior of an AI standing in for the remote opponent
    /// (<see cref="LocalOnlineMatchService"/>'s simulation), while passing a
    /// real one — <see cref="Othello.Unity.FirebaseMoveRelay"/> — replaces
    /// that stand-in with actual remote moves without
    /// <see cref="GameController"/> changing further.
    /// </para>
    /// </summary>
    public interface IOnlineMoveRelay
    {
        /// <summary>
        /// Sends the local player's move to the remote opponent.
        /// <see cref="GameController"/> also uses this to relay a
        /// resignation or a turn-timer timeout, with a reserved sentinel
        /// square value (-1, not a real board square) meaning "I forfeit" —
        /// this interface's contract otherwise only ever carries real board
        /// squares (0-63).
        /// </summary>
        void SendMove(int square);

        /// <summary>
        /// Raised when the remote opponent's move — or their forfeit
        /// sentinel, see <see cref="SendMove"/> — arrives.
        /// </summary>
        event Action<int> RemoteMoveReceived;
    }
}

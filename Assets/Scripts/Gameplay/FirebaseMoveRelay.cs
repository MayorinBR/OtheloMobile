using System;
using System.Collections.Generic;
using Firebase.Firestore;
using Othello.Services;

namespace Othello.Unity
{
    /// <summary>
    /// The networked half of a real online match: relays a played square
    /// from whichever player sent it to the other, through a
    /// <c>moves</c> subcollection under the match's own
    /// <c>onlineMatches/{roomCode}</c> document. Implements
    /// <see cref="IOnlineMoveRelay"/> so <see cref="GameController"/> only
    /// ever depends on that plain interface, never on Firestore directly.
    /// <para>
    /// Deliberately carries only a square index, nothing else — the engine
    /// (<see cref="Othello.Core.OthelloGame"/>) is already fully
    /// deterministic and runs identically on both peers, so relaying the
    /// move is enough; there is no board state to keep in sync over the
    /// network on top of that.
    /// </para>
    /// <para>
    /// A plain C# class, not a <see cref="MonoBehaviour"/> — unlike a
    /// Photon <c>NetworkObject</c>, a Firestore listener needs no scene
    /// presence, so <see cref="FirebaseOnlineMatchService"/> can construct
    /// and dispose of one as a normal object per match.
    /// </para>
    /// </summary>
    public sealed class FirebaseMoveRelay : IOnlineMoveRelay
    {
        private const string SquareField = "square";
        private const string SenderIdField = "senderId";
        private const string SentAtField = "sentAt";

        private readonly CollectionReference _movesCollection;
        private readonly string _localPlayerId;
        private ListenerRegistration _listenerRegistration;

        public event Action<int> RemoteMoveReceived;

        public FirebaseMoveRelay(CollectionReference movesCollection, string localPlayerId)
        {
            _movesCollection = movesCollection;
            _localPlayerId = localPlayerId;
            _listenerRegistration = _movesCollection.OrderBy(SentAtField).Listen(OnMovesSnapshot);
        }

        /// <summary>
        /// Sends the local player's move to the remote opponent.
        /// <see cref="GameController"/> also uses this to relay a
        /// resignation or a turn-timer timeout, with a reserved sentinel
        /// square value (-1, not a real board square) meaning "I forfeit" —
        /// this method otherwise only ever carries real board squares (0-63).
        /// </summary>
        public void SendMove(int square)
        {
            var move = new Dictionary<string, object>
            {
                [SquareField] = square,
                [SenderIdField] = _localPlayerId,
                [SentAtField] = FieldValue.ServerTimestamp
            };

            _movesCollection.AddAsync(move);
        }

        /// <summary>
        /// Stops listening for new moves — call once the match is over or
        /// abandoned, so an old match's listener doesn't keep consuming
        /// Firestore reads for a game nobody is looking at anymore.
        /// </summary>
        public void StopListening()
        {
            _listenerRegistration?.Stop();
            _listenerRegistration = null;
        }

        /// <summary>
        /// Every square played so far arrives as an "added" change on the
        /// first snapshot after the listener attaches, then one more
        /// "added" change per new move after that — the sender's own moves
        /// are skipped since <see cref="GameController"/> already applied
        /// them locally before calling <see cref="SendMove"/>.
        /// </summary>
        private void OnMovesSnapshot(QuerySnapshot snapshot)
        {
            foreach (DocumentChange change in snapshot.GetChanges())
            {
                if (change.ChangeType != DocumentChange.Type.Added)
                {
                    continue;
                }

                DocumentSnapshot document = change.Document;
                string senderId = document.GetValue<string>(SenderIdField);
                if (senderId == _localPlayerId)
                {
                    continue;
                }

                RemoteMoveReceived?.Invoke(document.GetValue<int>(SquareField));
            }
        }
    }
}

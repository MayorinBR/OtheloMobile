using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Firebase.Extensions;
using Firebase.Firestore;
using Othello.Services;
using UnityEngine;

namespace Othello.Unity
{
    /// <summary>
    /// Real <see cref="IOnlineMatchService"/> backed by Cloud Firestore —
    /// the same backend already used for accounts, cloud save and the
    /// leaderboard, instead of a dedicated networking SDK. Drop-in
    /// replacement for <see cref="LocalOnlineMatchService"/> — register this
    /// instead, in <see cref="OnlineMatchController.Awake"/>. Nothing else
    /// in the game changes, since <see cref="GameController"/> only ever
    /// depends on <see cref="IOnlineMatchService"/> and
    /// <see cref="IOnlineMoveRelay"/>.
    /// <para>
    /// Two collections carry the whole feature: <c>matchmakingQueue</c>
    /// holds one document per player currently searching for a ranked
    /// opponent, and <c>onlineMatches/{roomCode}</c> holds one document per
    /// match — friendly or ranked — with a <c>moves</c> subcollection
    /// underneath it that <see cref="FirebaseMoveRelay"/> listens to. A
    /// friendly match's room code (see <see cref="RoomCodeGenerator"/>) is
    /// literally the match document's id; a ranked match gets a freshly
    /// generated one nobody ever sees, used only as that document's id.
    /// </para>
    /// <para>
    /// <b>Known simplification, matching the one the original Photon Fusion
    /// design already called out:</b> ranked matchmaking claims whichever
    /// player has been waiting longest, not the closest in rating — real
    /// Elo-narrowed pairing would need a more elaborate query (or a Cloud
    /// Function) than an indie-scale Othello game needs today.
    /// </para>
    /// <para>
    /// <b>Written without the real Firebase Unity SDK available to compile
    /// against</b> — same caveat as <see cref="FirebaseCloudSaveService"/>
    /// and <see cref="FirebaseLeaderboardService"/> already carry. If your
    /// IDE flags an API mismatch, it's almost always a rename between SDK
    /// versions, not a logic error.
    /// </para>
    /// <para>
    /// <b>Needs Firestore Security Rules before it's safe to ship</b> —
    /// today's rules (if any) don't yet restrict who can write to
    /// <c>matchmakingQueue</c> or <c>onlineMatches</c>; anyone signed in
    /// can currently write moves for either side. That belongs on the same
    /// commercial-readiness checklist as the client-trusted currency and
    /// rating grants elsewhere in the game, not solved by this class alone.
    /// </para>
    /// </summary>
    public sealed class FirebaseOnlineMatchService : IOnlineMatchService, IOnlineMoveRelayProvider
    {
        private const string MatchmakingQueueCollection = "matchmakingQueue";
        private const string OnlineMatchesCollection = "onlineMatches";
        private const string MovesSubcollection = "moves";

        private const string StatusField = "status";
        private const string RoomCodeField = "roomCode";
        private const string OpponentField = "opponent";
        private const string IsFriendlyField = "isFriendly";
        private const string JoinedAtField = "joinedAt";
        private const string CreatedAtField = "createdAt";
        private const string Player1Field = "player1";
        private const string Player2Field = "player2";
        private const string DisplayNameField = "displayName";
        private const string RatingField = "rating";

        private const string StatusWaiting = "waiting";
        private const string StatusMatched = "matched";
        private const string StatusActive = "active";

        /// <summary>
        /// Identifies this session's writes within a single match or queue
        /// entry — e.g. so <see cref="FirebaseMoveRelay"/> can tell "my own
        /// move, already applied locally" apart from the opponent's. It
        /// does not need to be a stable identity across sessions, only
        /// unique for the lifetime of one search or match, so a fresh
        /// random id per instance is enough — no dependency on
        /// <see cref="IAuthService"/> needed here.
        /// </summary>
        private readonly string _localPlayerId = Guid.NewGuid().ToString("N");

        private DocumentReference _queueEntryRef;
        private ListenerRegistration _queueListener;
        private ListenerRegistration _matchListener;

        private FirebaseFirestore Db => FirebaseFirestore.DefaultInstance;

        public MatchmakingStatus Status { get; private set; } = MatchmakingStatus.Idle;

        public IOnlineMoveRelay Relay { get; private set; }

        public event Action<OnlineOpponent> MatchFound;

        public void FindMatch(string displayName, int rating)
        {
            if (Status == MatchmakingStatus.Searching)
            {
                return;
            }

            Status = MatchmakingStatus.Searching;
            FindWaitingOpponentAsync().ContinueWithOnMainThread(task =>
            {
                if (Status != MatchmakingStatus.Searching)
                {
                    return; // Cancelled while the query was in flight.
                }

                DocumentSnapshot waitingEntry = task.IsCanceled || task.IsFaulted ? null : task.Result;
                if (waitingEntry == null)
                {
                    EnqueueForQuickMatch(displayName, rating);
                    return;
                }

                ClaimQueueEntryAsync(waitingEntry.Reference, displayName, rating).ContinueWithOnMainThread(claimTask =>
                {
                    if (Status != MatchmakingStatus.Searching)
                    {
                        return;
                    }

                    if (claimTask.IsCanceled || claimTask.IsFaulted || !claimTask.Result.Claimed)
                    {
                        // Someone else claimed it a moment earlier — fall
                        // back to waiting in line instead of retrying, at
                        // this scale a second collision is very unlikely.
                        EnqueueForQuickMatch(displayName, rating);
                        return;
                    }

                    Status = MatchmakingStatus.InMatch;
                    AttachRelay(claimTask.Result.RoomCode);
                    MatchFound?.Invoke(claimTask.Result.Opponent);
                });
            });
        }

        /// <summary>
        /// Creates a private "friendly" match under a fresh room code and
        /// waits for a second player to join it via <see cref="JoinFriendlyMatch"/>.
        /// </summary>
        public string CreateFriendlyMatch(string displayName)
        {
            string code = RoomCodeGenerator.Generate();
            Status = MatchmakingStatus.Searching;

            DocumentReference matchRef = Db.Collection(OnlineMatchesCollection).Document(code);
            var data = new Dictionary<string, object>
            {
                [IsFriendlyField] = true,
                [StatusField] = StatusWaiting,
                [CreatedAtField] = FieldValue.ServerTimestamp,
                [Player1Field] = PlayerFields(displayName, EloRating.DefaultRating),
                [Player2Field] = null
            };

            matchRef.SetAsync(data).ContinueWithOnMainThread(task =>
            {
                if (Status != MatchmakingStatus.Searching)
                {
                    return;
                }

                if (task.IsCanceled || task.IsFaulted)
                {
                    Debug.LogError($"Falha ao criar partida amistosa: {task.Exception}");
                    Status = MatchmakingStatus.Idle;
                    return;
                }

                _matchListener = matchRef.Listen(snapshot => OnFriendlyRoomSnapshot(snapshot, code));
            });

            return code;
        }

        /// <summary>Joins a friendly match previously created with <see cref="CreateFriendlyMatch"/>, by its room code.</summary>
        public void JoinFriendlyMatch(string displayName, string roomCode)
        {
            if (Status == MatchmakingStatus.Searching)
            {
                return;
            }

            Status = MatchmakingStatus.Searching;
            string code = RoomCodeGenerator.Normalize(roomCode);
            DocumentReference matchRef = Db.Collection(OnlineMatchesCollection).Document(code);

            ClaimFriendlyRoomAsync(matchRef, displayName).ContinueWithOnMainThread(task =>
            {
                if (Status != MatchmakingStatus.Searching)
                {
                    return;
                }

                if (task.IsCanceled || task.IsFaulted || task.Result == null)
                {
                    Debug.LogWarning($"Não foi possível entrar na sala '{code}': {task.Exception}");
                    Status = MatchmakingStatus.Idle;
                    return;
                }

                Status = MatchmakingStatus.InMatch;
                AttachRelay(code);
                MatchFound?.Invoke(task.Result);
            });
        }

        public void CancelMatchmaking()
        {
            if (Status != MatchmakingStatus.Searching)
            {
                return;
            }

            Status = MatchmakingStatus.Idle;
            StopListeners();

            if (_queueEntryRef != null)
            {
                _queueEntryRef.DeleteAsync();
                _queueEntryRef = null;
            }
        }

        public void LeaveMatch()
        {
            Status = MatchmakingStatus.Idle;
            (Relay as FirebaseMoveRelay)?.StopListening();
            Relay = null;
            StopListeners();
        }

        /// <summary>Firestore delivers updates through its own listener callbacks; nothing to poll here.</summary>
        public void Tick(float deltaTimeSeconds)
        {
        }

        private void EnqueueForQuickMatch(string displayName, int rating)
        {
            _queueEntryRef = Db.Collection(MatchmakingQueueCollection).Document();
            var data = new Dictionary<string, object>
            {
                [StatusField] = StatusWaiting,
                [DisplayNameField] = displayName,
                [RatingField] = rating,
                [JoinedAtField] = FieldValue.ServerTimestamp
            };

            _queueEntryRef.SetAsync(data).ContinueWithOnMainThread(task =>
            {
                if (Status != MatchmakingStatus.Searching || task.IsCanceled || task.IsFaulted)
                {
                    return;
                }

                _queueListener = _queueEntryRef.Listen(OnQueueEntrySnapshot);
            });
        }

        private void OnQueueEntrySnapshot(DocumentSnapshot snapshot)
        {
            if (Status != MatchmakingStatus.Searching || !snapshot.Exists)
            {
                return;
            }

            if (snapshot.GetValue<string>(StatusField) != StatusMatched)
            {
                return;
            }

            string roomCode = snapshot.GetValue<string>(RoomCodeField);
            var opponentData = snapshot.GetValue<Dictionary<string, object>>(OpponentField);

            Status = MatchmakingStatus.InMatch;
            StopListeners();
            AttachRelay(roomCode);
            MatchFound?.Invoke(BuildOpponent(opponentData, isFriendly: false));
        }

        private void OnFriendlyRoomSnapshot(DocumentSnapshot snapshot, string roomCode)
        {
            if (Status != MatchmakingStatus.Searching || !snapshot.Exists)
            {
                return;
            }

            if (snapshot.GetValue<string>(StatusField) != StatusActive)
            {
                return;
            }

            var opponentData = snapshot.GetValue<Dictionary<string, object>>(Player2Field);
            if (opponentData == null)
            {
                return;
            }

            Status = MatchmakingStatus.InMatch;
            StopListeners();
            AttachRelay(roomCode);
            MatchFound?.Invoke(BuildOpponent(opponentData, isFriendly: true));
        }

        /// <summary>Oldest still-waiting entry in the queue, if any — see the class doc comment for why "oldest" rather than "closest rated".</summary>
        private Task<DocumentSnapshot> FindWaitingOpponentAsync()
        {
            Query waitingQuery = Db.Collection(MatchmakingQueueCollection)
                .WhereEqualTo(StatusField, StatusWaiting)
                .OrderBy(JoinedAtField)
                .Limit(1);

            return waitingQuery.GetSnapshotAsync().ContinueWith(task =>
            {
                if (task.IsCanceled || task.IsFaulted)
                {
                    return null;
                }

                foreach (DocumentSnapshot document in task.Result.Documents)
                {
                    return document;
                }

                return null;
            });
        }

        /// <summary>
        /// Atomically claims a waiting queue entry: fails harmlessly if
        /// another search claimed it first between the query above and
        /// this transaction running. On success, also creates the match
        /// document both sides will use from here on.
        /// </summary>
        private Task<(bool Claimed, string RoomCode, OnlineOpponent Opponent)> ClaimQueueEntryAsync(
            DocumentReference queueEntryRef, string displayName, int rating)
        {
            string roomCode = RoomCodeGenerator.Generate();
            DocumentReference matchRef = Db.Collection(OnlineMatchesCollection).Document(roomCode);

            // Explicit type argument: an async lambda returning a tuple
            // otherwise leaves the compiler nothing but structural
            // matching to infer the named-element type from, which is one
            // more thing to get wrong in a file already written without
            // the real SDK to check it against.
            return Db.RunTransactionAsync<(bool Claimed, string RoomCode, OnlineOpponent Opponent)>(async transaction =>
            {
                DocumentSnapshot entrySnapshot = await transaction.GetSnapshotAsync(queueEntryRef);
                if (!entrySnapshot.Exists || entrySnapshot.GetValue<string>(StatusField) != StatusWaiting)
                {
                    return (false, null, null);
                }

                string waitingDisplayName = entrySnapshot.GetValue<string>(DisplayNameField);
                int waitingRating = entrySnapshot.GetValue<int>(RatingField);

                transaction.Update(queueEntryRef, new Dictionary<string, object>
                {
                    [StatusField] = StatusMatched,
                    [RoomCodeField] = roomCode,
                    [OpponentField] = PlayerFields(displayName, rating)
                });

                transaction.Set(matchRef, new Dictionary<string, object>
                {
                    [IsFriendlyField] = false,
                    [StatusField] = StatusActive,
                    [CreatedAtField] = FieldValue.ServerTimestamp,
                    [Player1Field] = PlayerFields(waitingDisplayName, waitingRating),
                    [Player2Field] = PlayerFields(displayName, rating)
                });

                var opponent = new OnlineOpponent(waitingDisplayName, waitingRating, InventoryCatalog.AvatarDefault, isFriendly: false);
                return (true, roomCode, opponent);
            });
        }

        /// <summary>
        /// Atomically joins a friendly room: fails (returns null) if the
        /// code doesn't exist, was already used by a second player, or
        /// belongs to a match that already started.
        /// </summary>
        private Task<OnlineOpponent> ClaimFriendlyRoomAsync(DocumentReference matchRef, string displayName)
        {
            return Db.RunTransactionAsync<OnlineOpponent>(async transaction =>
            {
                DocumentSnapshot snapshot = await transaction.GetSnapshotAsync(matchRef);
                if (!snapshot.Exists || snapshot.GetValue<string>(StatusField) != StatusWaiting)
                {
                    return null;
                }

                var player1Data = snapshot.GetValue<Dictionary<string, object>>(Player1Field);
                string hostDisplayName = player1Data != null && player1Data.TryGetValue(DisplayNameField, out var name) ? name as string : "Player";
                int hostRating = player1Data != null && player1Data.TryGetValue(RatingField, out var rating) ? FirestoreValues.ToInt(rating, EloRating.DefaultRating) : EloRating.DefaultRating;

                transaction.Update(matchRef, new Dictionary<string, object>
                {
                    [Player2Field] = PlayerFields(displayName, EloRating.DefaultRating),
                    [StatusField] = StatusActive
                });

                return new OnlineOpponent(hostDisplayName, hostRating, InventoryCatalog.AvatarDefault, isFriendly: true);
            });
        }

        private void AttachRelay(string roomCode)
        {
            CollectionReference movesCollection = Db.Collection(OnlineMatchesCollection).Document(roomCode).Collection(MovesSubcollection);
            Relay = new FirebaseMoveRelay(movesCollection, _localPlayerId);
        }

        private void StopListeners()
        {
            _queueListener?.Stop();
            _queueListener = null;
            _matchListener?.Stop();
            _matchListener = null;
        }

        private static Dictionary<string, object> PlayerFields(string displayName, int rating) => new()
        {
            [DisplayNameField] = displayName,
            [RatingField] = rating
        };

        private static OnlineOpponent BuildOpponent(Dictionary<string, object> playerData, bool isFriendly)
        {
            string name = playerData != null && playerData.TryGetValue(DisplayNameField, out var n) ? n as string : "Opponent";
            int rating = playerData != null && playerData.TryGetValue(RatingField, out var r)
                ? FirestoreValues.ToInt(r, EloRating.DefaultRating)
                : EloRating.DefaultRating;

            return new OnlineOpponent(name, rating, InventoryCatalog.AvatarDefault, isFriendly);
        }
    }
}

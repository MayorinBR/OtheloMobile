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
    /// Real <see cref="ILeaderboardService"/> backed by two Firestore range
    /// queries against the same <c>users</c> collection
    /// <see cref="FirebaseCloudSaveService"/> saves profiles to — one for
    /// players rated at or above the local player, one for those below —
    /// merged into a single window. Drop-in replacement for
    /// <see cref="LocalLeaderboardService"/> — register this instead, in
    /// <see cref="LeaderboardController.Awake"/>. Nothing else in the game
    /// changes.
    /// <para>
    /// Both range queries are single-field (<c>rating</c>), so Firestore's
    /// automatic single-field indexes cover them — no composite index needs
    /// creating in the console.
    /// </para>
    /// <para>
    /// <b>Known limitation:</b> <see cref="LeaderboardEntry.IsLocalPlayer"/>
    /// is set by matching <c>displayName</c> and <c>rating</c> against the
    /// values passed in, since <see cref="ILeaderboardService.GetRanked"/>
    /// doesn't carry the local player's user id. Two players who share both
    /// a display name and an exact rating (rare, but possible) would both
    /// be flagged. Passing the signed-in <see cref="IAuthService.UserId"/>
    /// through instead would remove this edge case entirely, if it turns
    /// out to matter in practice.
    /// </para>
    /// </summary>
    public sealed class FirebaseLeaderboardService : ILeaderboardService
    {
        private const string UsersCollection = "users";
        private const string RatingField = "rating";
        private const string DisplayNameField = "displayName";

        private FirebaseFirestore Db => FirebaseFirestore.DefaultInstance;

        public void GetRanked(string localPlayerName, int localPlayerRating, int windowSize, Action<IReadOnlyList<LeaderboardEntry>> onCompleted)
        {
            int aboveLimit = windowSize / 2 + 1; // includes ties at the local player's own rating
            int belowLimit = Math.Max(0, windowSize - aboveLimit);

            Query aboveQuery = Db.Collection(UsersCollection)
                .WhereGreaterThanOrEqualTo(RatingField, localPlayerRating)
                .OrderBy(RatingField)
                .Limit(aboveLimit);

            Task<QuerySnapshot> aboveTask = aboveQuery.GetSnapshotAsync();

            // Firestore rejects Limit(0) outright (e.g. windowSize: 1, where
            // the whole window is covered by aboveQuery already) — skip the
            // query entirely instead of sending an invalid one.
            Task<QuerySnapshot> belowTask = belowLimit > 0
                ? Db.Collection(UsersCollection)
                    .WhereLessThan(RatingField, localPlayerRating)
                    .OrderByDescending(RatingField)
                    .Limit(belowLimit)
                    .GetSnapshotAsync()
                : Task.FromResult<QuerySnapshot>(null);

            Task.WhenAll(aboveTask, belowTask).ContinueWithOnMainThread(windowTask =>
            {
                if (windowTask.IsCanceled || windowTask.IsFaulted)
                {
                    Debug.LogError($"Falha ao consultar o ranking: {windowTask.Exception}");
                    onCompleted?.Invoke(Array.Empty<LeaderboardEntry>());
                    return;
                }

                // "below" comes back highest-to-lowest (closest to the local
                // player first); reversed, then "above" (already ascending)
                // appended, gives the whole window ascending by rating.
                var ascending = new List<(string Name, int Rating)>();
                var belowList = new List<(string, int)>();
                if (belowTask.Result != null)
                {
                    foreach (var doc in belowTask.Result.Documents)
                    {
                        belowList.Add((doc.GetValue<string>(DisplayNameField), doc.GetValue<int>(RatingField)));
                    }
                    belowList.Reverse();
                }
                ascending.AddRange(belowList);

                foreach (var doc in aboveTask.Result.Documents)
                {
                    ascending.Add((doc.GetValue<string>(DisplayNameField), doc.GetValue<int>(RatingField)));
                }

                if (ascending.Count == 0)
                {
                    onCompleted?.Invoke(Array.Empty<LeaderboardEntry>());
                    return;
                }

                // The window only proves relative order, not absolute rank —
                // a separate count of everyone rated above the window's top
                // entry gives the offset to number from.
                int topRatingInWindow = ascending[^1].Rating;
                Db.Collection(UsersCollection).WhereGreaterThan(RatingField, topRatingInWindow).Count
                    .GetSnapshotAsync(AggregateSource.Server).ContinueWithOnMainThread(countTask =>
                {
                    long rankOffset = countTask.IsCompletedSuccessfully ? countTask.Result.Count : 0;

                    var ranked = new List<LeaderboardEntry>(ascending.Count);
                    for (int i = 0; i < ascending.Count; i++)
                    {
                        // Descending display order: last (highest-rated) entry gets the best rank.
                        var (name, rating) = ascending[ascending.Count - 1 - i];
                        bool isLocal = rating == localPlayerRating && string.Equals(name, localPlayerName, StringComparison.Ordinal);
                        ranked.Add(new LeaderboardEntry((int)rankOffset + i + 1, name, rating, isLocal));
                    }

                    onCompleted?.Invoke(ranked);
                });
            });
        }
    }
}

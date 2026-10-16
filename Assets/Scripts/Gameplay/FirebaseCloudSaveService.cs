using System;
using System.Collections.Generic;
using Firebase.Extensions;
using Firebase.Firestore;
using Othello.Services;
using UnityEngine;

namespace Othello.Unity
{
    /// <summary>
    /// Real <see cref="ICloudSaveService"/> backed by Cloud Firestore, one
    /// document per player under <c>users/{userId}</c> — exactly the layout
    /// <see cref="PlayerProfileData"/>'s own doc comment describes. Drop-in
    /// replacement for <see cref="LocalCloudSaveService"/> — register this
    /// instead, in <see cref="PlayerSessionController.Start"/>. Nothing else
    /// in the game changes.
    /// <para>
    /// Maps to and from a plain <see cref="Dictionary{TKey,TValue}"/> by
    /// hand instead of using Firestore's <c>[FirestoreData]</c> attribute
    /// mapping, so <see cref="PlayerProfileData"/> itself stays a Firebase-free
    /// POCO — it's shared with the dependency-free test harness
    /// (<c>Othello.Tests</c>), which never references the Firebase SDK and
    /// would fail to build if this class's attributes leaked into it.
    /// </para>
    /// </summary>
    public sealed class FirebaseCloudSaveService : ICloudSaveService
    {
        private const string UsersCollection = "users";

        private FirebaseFirestore Db => FirebaseFirestore.DefaultInstance;

        public void Save(string userId, PlayerProfileData profile, Action<bool> onCompleted = null)
        {
            if (string.IsNullOrEmpty(userId) || profile == null)
            {
                onCompleted?.Invoke(false);
                return;
            }

            Db.Collection(UsersCollection).Document(userId).SetAsync(ToDocument(profile)).ContinueWithOnMainThread(task =>
            {
                if (task.IsCanceled || task.IsFaulted)
                {
                    Debug.LogError($"Falha ao salvar o perfil no Firestore: {task.Exception}");
                    onCompleted?.Invoke(false);
                    return;
                }

                onCompleted?.Invoke(true);
            });
        }

        public void Load(string userId, Action<PlayerProfileData, bool> onCompleted)
        {
            if (string.IsNullOrEmpty(userId))
            {
                onCompleted?.Invoke(null, false);
                return;
            }

            Db.Collection(UsersCollection).Document(userId).GetSnapshotAsync().ContinueWithOnMainThread(task =>
            {
                if (task.IsCanceled || task.IsFaulted || !task.Result.Exists)
                {
                    if (task.IsFaulted)
                    {
                        Debug.LogError($"Falha ao carregar o perfil do Firestore: {task.Exception}");
                    }
                    onCompleted?.Invoke(null, false);
                    return;
                }

                onCompleted?.Invoke(FromDocument(task.Result.ToDictionary()), true);
            });
        }

        public void Delete(string userId, Action<bool> onCompleted = null)
        {
            if (string.IsNullOrEmpty(userId))
            {
                onCompleted?.Invoke(false);
                return;
            }

            Db.Collection(UsersCollection).Document(userId).DeleteAsync().ContinueWithOnMainThread(task =>
            {
                if (task.IsCanceled || task.IsFaulted)
                {
                    Debug.LogError($"Falha ao excluir o perfil no Firestore: {task.Exception}");
                    onCompleted?.Invoke(false);
                    return;
                }

                onCompleted?.Invoke(true);
            });
        }

        private static Dictionary<string, object> ToDocument(PlayerProfileData profile) => new()
        {
            ["userId"] = profile.UserId,
            ["displayName"] = profile.DisplayName,
            ["avatarId"] = profile.AvatarId,
            ["unlockedSkinIds"] = new List<string>(profile.UnlockedSkinIds ?? new List<string>()),
            ["softBalance"] = profile.SoftBalance,
            ["hardBalance"] = profile.HardBalance,
            ["isNoAdsPurchased"] = profile.IsNoAdsPurchased,
            ["equippedPieceSkinId"] = profile.EquippedPieceSkinId,
            ["equippedBoardSkinId"] = profile.EquippedBoardSkinId,
            ["language"] = (int)profile.Language,
            ["rating"] = profile.Rating,
            ["matchesPlayed"] = profile.MatchesPlayed,
            ["wins"] = profile.Wins,
            ["losses"] = profile.Losses,
            ["draws"] = profile.Draws,
            ["onlineWins"] = profile.OnlineWins,
            ["currentWinStreak"] = profile.CurrentWinStreak,
            ["bestWinStreak"] = profile.BestWinStreak,
            ["unlockedAchievementIds"] = new List<string>(profile.UnlockedAchievementIds ?? new List<string>()),
            ["dailyRewardStreak"] = profile.DailyRewardStreak,
            ["lastDailyRewardClaimUtc"] = profile.LastDailyRewardClaimUtc.HasValue
                ? Timestamp.FromDateTime(DateTime.SpecifyKind(profile.LastDailyRewardClaimUtc.Value, DateTimeKind.Utc))
                : null
        };

        private static PlayerProfileData FromDocument(Dictionary<string, object> data)
        {
            var profile = new PlayerProfileData
            {
                UserId = GetString(data, "userId"),
                DisplayName = GetString(data, "displayName") ?? "Player",
                AvatarId = GetString(data, "avatarId") ?? "default",
                UnlockedSkinIds = GetStringList(data, "unlockedSkinIds"),
                SoftBalance = GetInt(data, "softBalance"),
                HardBalance = GetInt(data, "hardBalance"),
                IsNoAdsPurchased = data.TryGetValue("isNoAdsPurchased", out var noAds) && noAds is bool isNoAds && isNoAds,
                EquippedPieceSkinId = GetString(data, "equippedPieceSkinId"),
                EquippedBoardSkinId = GetString(data, "equippedBoardSkinId"),
                Language = (Language)GetInt(data, "language"),
                Rating = GetInt(data, "rating", EloRating.DefaultRating),
                MatchesPlayed = GetInt(data, "matchesPlayed"),
                Wins = GetInt(data, "wins"),
                Losses = GetInt(data, "losses"),
                Draws = GetInt(data, "draws"),
                OnlineWins = GetInt(data, "onlineWins"),
                CurrentWinStreak = GetInt(data, "currentWinStreak"),
                BestWinStreak = GetInt(data, "bestWinStreak"),
                UnlockedAchievementIds = GetStringList(data, "unlockedAchievementIds"),
                DailyRewardStreak = GetInt(data, "dailyRewardStreak")
            };

            if (data.TryGetValue("lastDailyRewardClaimUtc", out var claimRaw) && claimRaw is Timestamp claimTimestamp)
            {
                profile.LastDailyRewardClaimUtc = claimTimestamp.ToDateTime();
            }

            return profile;
        }

        private static string GetString(Dictionary<string, object> data, string key) =>
            data.TryGetValue(key, out var value) ? value as string : null;

        private static int GetInt(Dictionary<string, object> data, string key, int fallback = 0)
        {
            if (!data.TryGetValue(key, out var value) || value == null)
            {
                return fallback;
            }

            // Firestore round-trips whole numbers as long, not int.
            return value switch
            {
                long asLong => (int)asLong,
                int asInt => asInt,
                double asDouble => (int)asDouble,
                _ => fallback
            };
        }

        private static List<string> GetStringList(Dictionary<string, object> data, string key)
        {
            var result = new List<string>();
            if (data.TryGetValue(key, out var value) && value is List<object> rawList)
            {
                foreach (var item in rawList)
                {
                    if (item is string text)
                    {
                        result.Add(text);
                    }
                }
            }

            return result;
        }
    }
}

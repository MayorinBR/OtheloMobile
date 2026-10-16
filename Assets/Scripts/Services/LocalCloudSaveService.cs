using System;
using System.Collections.Generic;

namespace Othello.Services
{
    /// <summary>
    /// In-memory stand-in for Firestore/UGS Cloud Save: a dictionary keyed
    /// by user id, exactly like the real <c>users/{userId}</c> document
    /// layout, just without the network round trip. Swap this for the real
    /// implementation behind <see cref="ICloudSaveService"/> once that
    /// backend is wired in — <see cref="PlayerSession"/> doesn't change.
    /// </summary>
    public sealed class LocalCloudSaveService : ICloudSaveService
    {
        private readonly Dictionary<string, PlayerProfileData> _documents = new();

        public void Save(string userId, PlayerProfileData profile, Action<bool> onCompleted = null)
        {
            if (string.IsNullOrEmpty(userId) || profile == null)
            {
                onCompleted?.Invoke(false);
                return;
            }

            _documents[userId] = profile;
            onCompleted?.Invoke(true);
        }

        public void Load(string userId, Action<PlayerProfileData, bool> onCompleted)
        {
            if (userId != null && _documents.TryGetValue(userId, out var profile))
            {
                onCompleted?.Invoke(profile, true);
            }
            else
            {
                onCompleted?.Invoke(null, false);
            }
        }

        public void Delete(string userId, Action<bool> onCompleted = null)
        {
            if (userId != null)
            {
                _documents.Remove(userId);
            }

            onCompleted?.Invoke(true);
        }
    }
}

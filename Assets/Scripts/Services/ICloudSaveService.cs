using System;

namespace Othello.Services
{
    /// <summary>
    /// Facade over the cloud document store (Firestore or UGS Cloud Save).
    /// Named around what the game needs — save/load one player's profile —
    /// rather than around either SDK's own API shape, so swapping the
    /// backend later doesn't ripple into <see cref="PlayerSession"/>.
    /// </summary>
    public interface ICloudSaveService
    {
        void Save(string userId, PlayerProfileData profile, Action<bool> onCompleted = null);

        /// <summary><paramref name="onCompleted"/> receives (null, false) when no profile exists yet for this user.</summary>
        void Load(string userId, Action<PlayerProfileData, bool> onCompleted);

        /// <summary>Permanently deletes this player's saved profile document — part of <see cref="PlayerSession.DeleteAccount"/>.</summary>
        void Delete(string userId, Action<bool> onCompleted = null);
    }
}

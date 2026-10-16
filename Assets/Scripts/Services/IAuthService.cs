using System;
using System.Collections.Generic;

namespace Othello.Services
{
    /// <summary>Permanent identity providers an anonymous account can be linked to.</summary>
    public enum AuthProvider
    {
        Google,
        Apple,
        EmailPassword
    }

    /// <summary>
    /// Facade over the real identity provider (Firebase Auth or UGS
    /// Authentication). The game always starts signed in anonymously — see
    /// <see cref="SignInAnonymously"/> — and offers linking a permanent
    /// provider later without ever forcing a login screen up front.
    /// </summary>
    public interface IAuthService
    {
        bool IsSignedIn { get; }

        /// <summary>Stable id for the signed-in account; used as the Firestore document key.</summary>
        string UserId { get; }

        IReadOnlyList<AuthProvider> LinkedProviders { get; }

        void SignInAnonymously(Action<bool> onCompleted);

        void LinkWithGoogle(Action<bool> onCompleted);

        void LinkWithApple(Action<bool> onCompleted);

        void LinkWithEmailPassword(string email, string password, Action<bool> onCompleted);

        /// <summary>
        /// Permanently deletes the signed-in account from the identity
        /// provider itself — the account can never sign in again after
        /// this succeeds. Callers are responsible for deleting the matching
        /// <see cref="ICloudSaveService"/> document first (see
        /// <see cref="PlayerSession.DeleteAccount"/>), since <see cref="UserId"/>
        /// stops being usable once the account is gone.
        /// </summary>
        void DeleteAccount(Action<bool> onCompleted);
    }
}

using System;
using System.Collections.Generic;
using Firebase;
using Firebase.Auth;
using Firebase.Extensions;
using Othello.Services;
using UnityEngine;

namespace Othello.Unity
{
    /// <summary>
    /// Real <see cref="IAuthService"/> backed by Firebase Auth. Drop-in
    /// replacement for <see cref="LocalAuthService"/> — register this
    /// instead, in <see cref="PlayerSessionController.Start"/>, once the
    /// Firebase Unity SDK is installed and the project's
    /// <c>google-services.json</c> / <c>GoogleService-Info.plist</c> is in
    /// place. Nothing else in the game changes.
    /// <para>
    /// Every call waits on Firebase's own <c>CheckAndFixDependenciesAsync</c>
    /// first — required on Android, where Google Play services can be
    /// missing or out of date — instead of assuming Firebase is already
    /// usable the moment this class is constructed.
    /// </para>
    /// <para>
    /// <b>Google/Apple linking is only half-wired.</b> Firebase Auth itself
    /// only ever links a <em>credential</em> (an ID token) to the anonymous
    /// account — it does not obtain that token. Getting one requires a
    /// separate native sign-in flow (Google Sign-In for Unity on Android,
    /// Sign in with Apple on iOS) configured with your own OAuth client
    /// IDs, which only you can set up. <see cref="LinkWithGoogle"/> and
    /// <see cref="LinkWithApple"/> satisfy <see cref="IAuthService"/> by
    /// failing clearly until that plugin is wired in and calls
    /// <see cref="LinkWithGoogleIdToken"/> / <see cref="LinkWithAppleIdToken"/>
    /// instead — the Firebase-side linking those do is already complete.
    /// </para>
    /// </summary>
    public sealed class FirebaseAuthService : IAuthService
    {
        private static readonly Dictionary<string, AuthProvider> ProviderIdToAuthProvider = new()
        {
            ["google.com"] = AuthProvider.Google,
            ["apple.com"] = AuthProvider.Apple,
            ["password"] = AuthProvider.EmailPassword
        };

        private readonly List<AuthProvider> _linkedProviders = new();
        private readonly System.Threading.Tasks.Task<DependencyStatus> _dependenciesReady;
        private FirebaseAuth _auth;

        public FirebaseAuthService()
        {
            _dependenciesReady = FirebaseApp.CheckAndFixDependenciesAsync();
        }

        public bool IsSignedIn => _auth?.CurrentUser != null;

        public string UserId => _auth?.CurrentUser?.UserId;

        public IReadOnlyList<AuthProvider> LinkedProviders => _linkedProviders;

        public void SignInAnonymously(Action<bool> onCompleted)
        {
            _dependenciesReady.ContinueWithOnMainThread(dependencyTask =>
            {
                if (dependencyTask.IsCanceled || dependencyTask.IsFaulted || dependencyTask.Result != DependencyStatus.Available)
                {
                    Debug.LogError($"Firebase indisponível: {dependencyTask.Result}");
                    onCompleted?.Invoke(false);
                    return;
                }

                _auth = FirebaseAuth.DefaultInstance;

                // Firebase restores a previously signed-in user (anonymous
                // or linked) on its own before this ever runs — calling
                // SignInAnonymouslyAsync then just returns that same user
                // rather than minting a new one. Either way, this is also
                // the only place a *returning* linked account's providers
                // get back into _linkedProviders (see RefreshLinkedProviders),
                // since that list starts empty every time this class is
                // constructed, unlike Firebase's own session.
                _auth.SignInAnonymouslyAsync().ContinueWithOnMainThread(signInTask =>
                {
                    if (signInTask.IsCanceled || signInTask.IsFaulted)
                    {
                        Debug.LogError($"Falha no sign-in anônimo: {signInTask.Exception}");
                        onCompleted?.Invoke(false);
                        return;
                    }

                    RefreshLinkedProviders();
                    onCompleted?.Invoke(true);
                });
            });
        }

        public void LinkWithGoogle(Action<bool> onCompleted)
        {
            Debug.LogWarning(
                "LinkWithGoogle: nenhum plugin de Google Sign-In wireado. " +
                "Chame LinkWithGoogleIdToken depois de obter um ID token real.");
            onCompleted?.Invoke(false);
        }

        public void LinkWithApple(Action<bool> onCompleted)
        {
            Debug.LogWarning(
                "LinkWithApple: nenhum plugin de Sign in with Apple wireado. " +
                "Chame LinkWithAppleIdToken depois de obter um ID token real.");
            onCompleted?.Invoke(false);
        }

        /// <summary>
        /// Completes Google linking once you have a real ID token from a
        /// Google Sign-In plugin. <paramref name="accessToken"/> may be null
        /// depending on which plugin you use — Firebase only requires it for
        /// some flows.
        /// </summary>
        public void LinkWithGoogleIdToken(string idToken, string accessToken, Action<bool> onCompleted) =>
            LinkWithCredential(GoogleAuthProvider.GetCredential(idToken, accessToken), AuthProvider.Google, onCompleted);

        /// <summary>
        /// Completes Apple linking once you have a real ID token and raw
        /// nonce from a Sign in with Apple plugin.
        /// </summary>
        public void LinkWithAppleIdToken(string idToken, string rawNonce, Action<bool> onCompleted) =>
            LinkWithCredential(OAuthProvider.GetCredential("apple.com", idToken, rawNonce, null), AuthProvider.Apple, onCompleted);

        public void LinkWithEmailPassword(string email, string password, Action<bool> onCompleted)
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                onCompleted?.Invoke(false);
                return;
            }

            LinkWithCredential(EmailAuthProvider.GetCredential(email, password), AuthProvider.EmailPassword, onCompleted);
        }

        /// <summary>
        /// Permanently deletes the current Firebase user. Firebase requires
        /// a "recent" sign-in for this on some providers — an anonymous
        /// account (every account starts anonymous; see
        /// <see cref="SignInAnonymously"/>) never hits that requirement, so
        /// this only risks failing for an account linked a long time ago
        /// that hasn't been used in a while, in which case Firebase reports
        /// the failure through <paramref name="onCompleted"/> rather than
        /// throwing.
        /// </summary>
        public void DeleteAccount(Action<bool> onCompleted)
        {
            if (_auth?.CurrentUser == null)
            {
                onCompleted?.Invoke(false);
                return;
            }

            _auth.CurrentUser.DeleteAsync().ContinueWithOnMainThread(task =>
            {
                if (task.IsCanceled || task.IsFaulted)
                {
                    Debug.LogError($"Falha ao excluir a conta: {task.Exception}");
                    onCompleted?.Invoke(false);
                    return;
                }

                _linkedProviders.Clear();
                onCompleted?.Invoke(true);
            });
        }

        private void LinkWithCredential(Credential credential, AuthProvider provider, Action<bool> onCompleted)
        {
            if (_auth?.CurrentUser == null)
            {
                onCompleted?.Invoke(false);
                return;
            }

            _auth.CurrentUser.LinkWithCredentialAsync(credential).ContinueWithOnMainThread(task =>
            {
                if (task.IsCanceled || task.IsFaulted)
                {
                    Debug.LogError($"Falha ao vincular {provider}: {task.Exception}");
                    onCompleted?.Invoke(false);
                    return;
                }

                RefreshLinkedProviders();
                onCompleted?.Invoke(true);
            });
        }

        /// <summary>
        /// Rebuilds <see cref="_linkedProviders"/> from Firebase's own
        /// record of which providers this user has (<c>CurrentUser.ProviderData</c>)
        /// instead of only tracking what this particular instance linked —
        /// otherwise <see cref="LinkedProviders"/> (and so
        /// <see cref="PlayerSession.HasAccount"/>) would incorrectly read
        /// empty for a player who linked an account in an earlier session
        /// and simply reopened the app, since a fresh <see cref="FirebaseAuthService"/>
        /// starts with nothing recorded locally.
        /// </summary>
        private void RefreshLinkedProviders()
        {
            _linkedProviders.Clear();
            if (_auth?.CurrentUser == null)
            {
                return;
            }

            foreach (var info in _auth.CurrentUser.ProviderData)
            {
                if (ProviderIdToAuthProvider.TryGetValue(info.ProviderId, out var provider) && !_linkedProviders.Contains(provider))
                {
                    _linkedProviders.Add(provider);
                }
            }
        }
    }
}

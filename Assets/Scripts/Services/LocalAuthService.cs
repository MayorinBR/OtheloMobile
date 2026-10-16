using System;
using System.Collections.Generic;

namespace Othello.Services
{
    /// <summary>
    /// Local stand-in for Firebase Auth / UGS Authentication: signing in
    /// "anonymously" just mints a local id, and linking a provider always
    /// succeeds once signed in. Enough to exercise
    /// <see cref="PlayerSession"/> end to end without a real backend or
    /// network access.
    /// </summary>
    public sealed class LocalAuthService : IAuthService
    {
        private readonly List<AuthProvider> _linkedProviders = new();

        public bool IsSignedIn { get; private set; }
        public string UserId { get; private set; }
        public IReadOnlyList<AuthProvider> LinkedProviders => _linkedProviders;

        public void SignInAnonymously(Action<bool> onCompleted)
        {
            UserId = "local-" + Guid.NewGuid().ToString("N");
            IsSignedIn = true;
            onCompleted?.Invoke(true);
        }

        public void LinkWithGoogle(Action<bool> onCompleted) => Link(AuthProvider.Google, onCompleted);

        public void LinkWithApple(Action<bool> onCompleted) => Link(AuthProvider.Apple, onCompleted);

        public void LinkWithEmailPassword(string email, string password, Action<bool> onCompleted)
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                onCompleted?.Invoke(false);
                return;
            }

            Link(AuthProvider.EmailPassword, onCompleted);
        }

        private void Link(AuthProvider provider, Action<bool> onCompleted)
        {
            if (!IsSignedIn)
            {
                onCompleted?.Invoke(false);
                return;
            }

            if (!_linkedProviders.Contains(provider))
            {
                _linkedProviders.Add(provider);
            }

            onCompleted?.Invoke(true);
        }

        public void DeleteAccount(Action<bool> onCompleted)
        {
            IsSignedIn = false;
            UserId = null;
            _linkedProviders.Clear();
            onCompleted?.Invoke(true);
        }
    }
}

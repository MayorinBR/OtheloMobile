using Othello.Services;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Othello.Unity
{
    /// <summary>
    /// The first screen most players see (see <see cref="ScreenManager"/>'s
    /// <c>_initialScreen</c>): offers linking Google, Apple, or email —
    /// modern mobile login options — plus continuing as a guest on the
    /// anonymous session <see cref="PlayerSessionController"/> already
    /// started. Two device-local toggles (<see cref="LoginPreferences.RememberUser"/>,
    /// <see cref="LoginPreferences.AutoConnect"/>) control whether this
    /// screen shows itself again on a later launch — see <see cref="OnShown"/>.
    /// <para>
    /// Linking never blocks play: every button here operates on the
    /// anonymous account already in progress (see <see cref="IAuthService"/>'s
    /// own doc comment), so skipping straight to "Continuar como
    /// convidado" is always a real, first-class option, not a hidden escape
    /// hatch.
    /// </para>
    /// <para>
    /// <b>Google and Apple only go as far as <see cref="IAuthService"/>
    /// itself does today</b> — <see cref="FirebaseAuthService.LinkWithGoogle"/>/
    /// <see cref="FirebaseAuthService.LinkWithApple"/> report failure until a
    /// native sign-in plugin is wired in and calls
    /// <see cref="FirebaseAuthService.LinkWithGoogleIdToken"/>/
    /// <see cref="FirebaseAuthService.LinkWithAppleIdToken"/> with a real
    /// token. The buttons are built and wired now so the screen is ready
    /// the moment that plugin exists.
    /// </para>
    /// </summary>
    public sealed class LoginScreen : UIScreen
    {
        [SerializeField]
        private LocalizationController _localization;

        [SerializeField]
        private PlayerSessionController _playerSession;

        [SerializeField]
        private Toggle _rememberToggle;

        [SerializeField]
        private Toggle _autoConnectToggle;

        [SerializeField]
        private Button _googleButton;

        [SerializeField]
        private Button _appleButton;

        [SerializeField]
        private Button _emailButton;

        [SerializeField]
        private GameObject _emailPanel;

        [SerializeField]
        private TMP_InputField _emailInput;

        [SerializeField]
        private TMP_InputField _passwordInput;

        [SerializeField]
        private Button _emailConfirmButton;

        [SerializeField]
        private Button _guestButton;

        [SerializeField]
        private TMP_Text _feedbackLabel;

        private void Awake()
        {
            _rememberToggle?.onValueChanged.AddListener(value => LoginPreferences.RememberUser = value);
            _autoConnectToggle?.onValueChanged.AddListener(value => LoginPreferences.AutoConnect = value);

            _googleButton?.onClick.AddListener(() => LinkAndProceed(_playerSession.Session.LinkGoogleAccount));
            _appleButton?.onClick.AddListener(() => LinkAndProceed(_playerSession.Session.LinkAppleAccount));
            _emailButton?.onClick.AddListener(() => _emailPanel?.SetActive(!_emailPanel.activeSelf));
            _emailConfirmButton?.onClick.AddListener(SubmitEmail);
            _guestButton?.onClick.AddListener(ContinueAsGuest);
        }

        /// <summary>
        /// Skips straight to the main menu when both toggles are on and
        /// this session already has a linked account — see
        /// <see cref="LoginPreferences.ShouldSkipLogin"/>. Otherwise shows
        /// the two toggles at their saved values and clears any leftover
        /// feedback from a previous visit.
        /// </summary>
        protected override void OnShown()
        {
            bool hasAccount = _playerSession != null && _playerSession.Session != null && _playerSession.Session.HasAccount;
            if (LoginPreferences.ShouldSkipLogin(hasAccount))
            {
                Manager.ShowRoot<MainMenuScreen>();
                return;
            }

            if (_rememberToggle != null)
            {
                _rememberToggle.isOn = LoginPreferences.RememberUser;
            }

            if (_autoConnectToggle != null)
            {
                _autoConnectToggle.isOn = LoginPreferences.AutoConnect;
            }

            _emailPanel?.SetActive(false);
            _feedbackLabel?.gameObject.SetActive(false);
        }

        private void LinkAndProceed(System.Action<System.Action<bool>> link)
        {
            if (_playerSession == null || _playerSession.Session == null)
            {
                return;
            }

            link(success => HandleLinkResult(success));
        }

        private void SubmitEmail()
        {
            if (_playerSession == null || _playerSession.Session == null)
            {
                return;
            }

            string email = _emailInput != null ? _emailInput.text : string.Empty;
            string password = _passwordInput != null ? _passwordInput.text : string.Empty;
            _playerSession.Session.LinkEmailPasswordAccount(email, password, HandleLinkResult);
        }

        private void HandleLinkResult(bool success)
        {
            if (_feedbackLabel != null && _localization != null)
            {
                _feedbackLabel.text = _localization.Service.Get(
                    success ? LocalizationKey.LoginLinkSuccess : LocalizationKey.LoginLinkFailed);
                _feedbackLabel.gameObject.SetActive(true);
            }

            if (success)
            {
                Manager.ShowRoot<MainMenuScreen>();
            }
        }

        /// <summary>
        /// Proceeds on the anonymous session as-is — see the class doc
        /// comment on why this is always available, not a fallback for
        /// when the other options fail.
        /// </summary>
        private void ContinueAsGuest()
        {
            Manager.ShowRoot<MainMenuScreen>();
        }
    }
}

using Othello.Services;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Othello.Unity
{
    /// <summary>
    /// Options screen: overall volume, language, and back. The language
    /// picker cycles through every <see cref="Language"/> on each click
    /// rather than using a dropdown — simpler to build and wire for a
    /// handful of options, and every other screen picks up the change
    /// immediately through <see cref="LocalizationService.Changed"/>.
    /// </summary>
    public sealed class SettingsScreen : UIScreen
    {
        [SerializeField]
        private LocalizationController _localization;

        [SerializeField]
        private PlayerSessionController _playerSession;

        [SerializeField]
        private Slider _volumeSlider;

        [SerializeField]
        private Button _languageButton;

        [SerializeField]
        private TMP_Text _languageValueLabel;

        [SerializeField]
        private TMP_InputField _displayNameInput;

        [SerializeField]
        private Button _saveNameButton;

        [SerializeField]
        private GameObject _accountLinkPanel;

        [SerializeField]
        private TMP_Text _accountStatusLabel;

        [SerializeField]
        private TMP_InputField _accountEmailInput;

        [SerializeField]
        private TMP_InputField _accountPasswordInput;

        [SerializeField]
        private Button _createAccountButton;

        [SerializeField]
        private TMP_Text _accountFeedbackLabel;

        [SerializeField]
        private Button _deleteAccountButton;

        [SerializeField]
        private GameObject _deleteAccountConfirmPanel;

        [SerializeField]
        private Button _deleteAccountConfirmYesButton;

        [SerializeField]
        private Button _deleteAccountConfirmNoButton;

        [SerializeField]
        private Button _backButton;

        private void Awake()
        {
            _backButton?.onClick.AddListener(() => Manager.GoBack());

            if (_volumeSlider != null)
            {
                _volumeSlider.value = AudioListener.volume;
                _volumeSlider.onValueChanged.AddListener(value => AudioListener.volume = value);
            }

            _languageButton?.onClick.AddListener(CycleLanguage);
            _saveNameButton?.onClick.AddListener(SaveDisplayName);
            _createAccountButton?.onClick.AddListener(CreateAccount);
            _deleteAccountButton?.onClick.AddListener(() => _deleteAccountConfirmPanel?.SetActive(true));
            _deleteAccountConfirmYesButton?.onClick.AddListener(ConfirmDeleteAccount);
            _deleteAccountConfirmNoButton?.onClick.AddListener(() => _deleteAccountConfirmPanel?.SetActive(false));
        }

        protected override void OnShown()
        {
            RefreshLanguageLabel();
            RefreshDisplayNameInput();
            RefreshAccountStatus();
            _accountFeedbackLabel?.gameObject.SetActive(false);
            _deleteAccountConfirmPanel?.SetActive(false);
        }

        private void CycleLanguage()
        {
            if (_localization == null)
            {
                return;
            }

            _localization.Service.SetLanguage(_localization.Service.CurrentLanguage.Next());
            RefreshLanguageLabel();
        }

        private void RefreshLanguageLabel()
        {
            if (_languageValueLabel == null || _localization == null)
            {
                return;
            }

            string languageLabel = _localization.Service.Get(LocalizationKey.SettingsLanguage);
            string autonym = _localization.Service.CurrentLanguage.Autonym();
            _languageValueLabel.text = $"{languageLabel}: {autonym}";
        }

        private void RefreshDisplayNameInput()
        {
            if (_displayNameInput == null || _playerSession == null || _playerSession.Session == null)
            {
                return;
            }

            _displayNameInput.text = _playerSession.Session.DisplayName;
        }

        /// <summary>
        /// Applies the typed name via <see cref="PlayerSession.TrySetDisplayName"/>
        /// and then re-reads it back into the field — this both reflects a
        /// successful rename and reverts an invalid one back to the current
        /// name, with no separate error message needed.
        /// </summary>
        private void SaveDisplayName()
        {
            if (_displayNameInput == null || _playerSession == null || _playerSession.Session == null)
            {
                return;
            }

            _playerSession.Session.TrySetDisplayName(_displayNameInput.text);
            RefreshDisplayNameInput();
        }

        /// <summary>
        /// Reflects <see cref="PlayerSession.HasAccount"/>: a guest sees the
        /// email/password fields and the create-account button, so they can
        /// link a permanent identity; once linked there's nothing left to do
        /// here, so the link panel just shows the "linked" status instead.
        /// </summary>
        private void RefreshAccountStatus()
        {
            if (_playerSession == null || _playerSession.Session == null || _localization == null)
            {
                return;
            }

            bool hasAccount = _playerSession.Session.HasAccount;
            if (_accountStatusLabel != null)
            {
                _accountStatusLabel.text = _localization.Service.Get(
                    hasAccount ? LocalizationKey.SettingsAccountLinkedStatus : LocalizationKey.SettingsAccountGuestStatus);
            }

            _accountLinkPanel?.SetActive(!hasAccount);
        }

        /// <summary>
        /// Links the typed email and password to the current session via
        /// <see cref="PlayerSession.LinkEmailPasswordAccount"/> and reports
        /// the result — on success this turns a guest into a real account
        /// (see <see cref="PlayerSession.HasAccount"/>), which is what
        /// unlocks the ranking system and the full match currency reward.
        /// </summary>
        private void CreateAccount()
        {
            if (_playerSession == null || _playerSession.Session == null || _localization == null)
            {
                return;
            }

            string email = _accountEmailInput != null ? _accountEmailInput.text : string.Empty;
            string password = _accountPasswordInput != null ? _accountPasswordInput.text : string.Empty;

            _playerSession.Session.LinkEmailPasswordAccount(email, password, success =>
            {
                if (_accountFeedbackLabel != null)
                {
                    _accountFeedbackLabel.text = _localization.Service.Get(
                        success ? LocalizationKey.SettingsAccountLinkSuccess : LocalizationKey.SettingsAccountLinkFailed);
                    _accountFeedbackLabel.gameObject.SetActive(true);
                }

                if (success)
                {
                    if (_accountPasswordInput != null)
                    {
                        _accountPasswordInput.text = string.Empty;
                    }

                    RefreshAccountStatus();
                }
            });
        }

        /// <summary>
        /// Permanently deletes the signed-in account (see
        /// <see cref="PlayerSession.DeleteAccount"/>) — required by both
        /// storefronts' account-deletion policies. There is no session left
        /// to return to afterward, so this closes the app a couple of
        /// seconds after confirming, giving the player time to read the
        /// result before it does.
        /// </summary>
        private void ConfirmDeleteAccount()
        {
            _deleteAccountConfirmPanel?.SetActive(false);

            if (_playerSession == null || _playerSession.Session == null || _localization == null)
            {
                return;
            }

            _playerSession.Session.DeleteAccount(success =>
            {
                if (_accountFeedbackLabel != null)
                {
                    _accountFeedbackLabel.text = _localization.Service.Get(
                        success ? LocalizationKey.SettingsDeleteAccountSuccess : LocalizationKey.SettingsDeleteAccountFailed);
                    _accountFeedbackLabel.gameObject.SetActive(true);
                }

                if (success)
                {
                    Invoke(nameof(QuitAfterAccountDeletion), 2f);
                }
            });
        }

        private void QuitAfterAccountDeletion()
        {
            Application.Quit();
        }
    }
}

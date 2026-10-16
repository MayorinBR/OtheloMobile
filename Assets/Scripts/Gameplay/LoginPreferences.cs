using UnityEngine;

namespace Othello.Unity
{
    /// <summary>
    /// The two device-local toggles <see cref="LoginScreen"/> offers —
    /// "save my login" and "connect automatically" — stored via
    /// <see cref="PlayerPrefs"/>, not the cloud profile. These describe how
    /// this device should behave before a session even exists, not a fact
    /// about the player, so they don't belong in <see cref="PlayerProfileData"/>.
    /// <para>
    /// Firebase Auth already restores a signed-in session across launches
    /// on its own (anonymous or linked) — these two only decide whether
    /// <see cref="LoginScreen"/> shows itself again or skips straight to
    /// the main menu. Neither one ever deletes or signs anything out;
    /// turning "save my login" off just means being asked again next time,
    /// same as any app's own "remember me" checkbox.
    /// </para>
    /// </summary>
    public static class LoginPreferences
    {
        private const string RememberUserKey = "othello_remember_user";
        private const string AutoConnectKey = "othello_auto_connect";

        /// <summary>On by default — most players expect to stay signed in without being asked every launch.</summary>
        public static bool RememberUser
        {
            get => PlayerPrefs.GetInt(RememberUserKey, 1) == 1;
            set => PlayerPrefs.SetInt(RememberUserKey, value ? 1 : 0);
        }

        public static bool AutoConnect
        {
            get => PlayerPrefs.GetInt(AutoConnectKey, 1) == 1;
            set => PlayerPrefs.SetInt(AutoConnectKey, value ? 1 : 0);
        }

        /// <summary>
        /// Whether <see cref="LoginScreen"/> should skip itself and go
        /// straight to the main menu — both toggles have to be on, and the
        /// session needs an actual linked identity to reconnect to.
        /// </summary>
        public static bool ShouldSkipLogin(bool hasLinkedAccount) => RememberUser && AutoConnect && hasLinkedAccount;
    }
}

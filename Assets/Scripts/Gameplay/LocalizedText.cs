using Othello.Services;
using TMPro;
using UnityEngine;

namespace Othello.Unity
{
    /// <summary>
    /// Binds a <see cref="TMP_Text"/> to a <see cref="LocalizationKey"/>:
    /// sets its text from the current language on startup and keeps it in
    /// sync whenever the language changes. Put this on any label whose
    /// text never changes for a reason other than language — button
    /// captions, static headers. Text that changes for other reasons too
    /// (HUD scores, item names) is resolved directly by the screen that
    /// owns it instead, via <see cref="LocalizationController"/>.
    /// </summary>
    [RequireComponent(typeof(TMP_Text))]
    public sealed class LocalizedText : MonoBehaviour
    {
        [SerializeField]
        private LocalizationController _localization;

        [SerializeField]
        private LocalizationKey _key;

        private TMP_Text _label;

        private void Awake()
        {
            _label = GetComponent<TMP_Text>();
        }

        private void Start()
        {
            // Safe to read _localization.Service here (but not from Awake):
            // Unity guarantees every Awake has run before any Start, and
            // LocalizationController creates its Service in its own Awake.
            if (_localization == null)
            {
                Debug.LogError($"LocalizedText em '{name}' precisa de _localization atribuído no Inspector.");
                return;
            }

            _localization.Service.Changed += Refresh;
            Refresh();
        }

        private void OnDestroy()
        {
            if (_localization != null && _localization.Service != null)
            {
                _localization.Service.Changed -= Refresh;
            }
        }

        private void Refresh()
        {
            _label.text = _localization.Service.Get(_key);
        }
    }
}

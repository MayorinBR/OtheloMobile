using UnityEngine;

namespace Othello.Unity
{
    /// <summary>
    /// Base class for every full screen the game navigates between (main
    /// menu, play mode picker, an in-progress match, the shop, and so on).
    /// A screen never activates or deactivates itself — <see cref="ScreenManager"/>
    /// owns that, so there is exactly one place that decides what is
    /// currently visible instead of every screen managing its own show/hide
    /// state and risking exactly the "open but no way to close" bug that
    /// motivated this class.
    /// </summary>
    public abstract class UIScreen : MonoBehaviour
    {
        [SerializeField]
        private GameObject _root;

        /// <summary>The manager that owns navigation between screens.</summary>
        protected ScreenManager Manager { get; private set; }

        /// <summary>Called once by <see cref="ScreenManager"/> during its own setup.</summary>
        internal void Initialize(ScreenManager manager)
        {
            Manager = manager;
        }

        /// <summary>Activates this screen's root and notifies it via <see cref="OnShown"/>.</summary>
        internal void Activate()
        {
            if (_root != null)
            {
                _root.SetActive(true);
            }
            OnShown();
        }

        /// <summary>Notifies this screen via <see cref="OnHidden"/> and deactivates its root.</summary>
        internal void Deactivate()
        {
            OnHidden();
            if (_root != null)
            {
                _root.SetActive(false);
            }
        }

        /// <summary>Called every time this screen becomes the active one.</summary>
        protected virtual void OnShown()
        {
        }

        /// <summary>Called right before this screen is replaced by another one.</summary>
        protected virtual void OnHidden()
        {
        }
    }
}

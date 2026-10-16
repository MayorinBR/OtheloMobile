using System;
using System.Collections.Generic;
using UnityEngine;

namespace Othello.Unity
{
    /// <summary>
    /// Owns which single <see cref="UIScreen"/> is currently visible and the
    /// back-stack of how the player got there. Screens never activate or
    /// deactivate themselves or each other — they only ever ask this
    /// manager to <see cref="Show{TScreen}"/> or <see cref="GoBack"/>, which
    /// keeps every screen decoupled from every other one.
    /// <para>
    /// Editor setup: put one of these on a GameObject alongside every
    /// screen's own GameObject (they don't need to be children of it),
    /// assign every screen component to <see cref="_screens"/>, and pick
    /// which one is <see cref="_initialScreen"/> (normally the main menu).
    /// </para>
    /// </summary>
    public sealed class ScreenManager : MonoBehaviour
    {
        [SerializeField]
        private UIScreen[] _screens;

        [SerializeField]
        private UIScreen _initialScreen;

        private readonly Dictionary<Type, UIScreen> _byType = new();
        private readonly Stack<UIScreen> _history = new();
        private UIScreen _current;

        private void Awake()
        {
            foreach (var screen in _screens)
            {
                if (screen == null)
                {
                    continue;
                }

                _byType[screen.GetType()] = screen;
                screen.Initialize(this);
                screen.Deactivate();
            }
        }

        private void Start()
        {
            if (_initialScreen != null)
            {
                Navigate(_initialScreen.GetType(), pushHistory: false);
            }
        }

        /// <summary>Navigates forward to <typeparamref name="TScreen"/>, remembering the current screen so <see cref="GoBack"/> can return to it.</summary>
        public void Show<TScreen>() where TScreen : UIScreen => Navigate(typeof(TScreen), pushHistory: true);

        /// <summary>
        /// Navigates to <typeparamref name="TScreen"/> and clears the
        /// back-stack, so <see cref="GoBack"/> can no longer return to
        /// anything before it. Use this for a "return to the main menu"
        /// action from deep in the navigation flow (e.g. after a match
        /// ends), where stepping back one screen at a time doesn't make sense.
        /// </summary>
        public void ShowRoot<TScreen>() where TScreen : UIScreen
        {
            _history.Clear();
            Navigate(typeof(TScreen), pushHistory: false);
        }

        /// <summary>Returns to the previous screen on the back-stack, if any.</summary>
        public void GoBack()
        {
            if (_history.Count == 0)
            {
                return;
            }

            _current?.Deactivate();
            _current = _history.Pop();
            _current.Activate();
        }

        private void Navigate(Type screenType, bool pushHistory)
        {
            if (!_byType.TryGetValue(screenType, out var next))
            {
                Debug.LogError($"Tela não registrada em ScreenManager: {screenType.Name}");
                return;
            }

            if (_current != null)
            {
                _current.Deactivate();
                if (pushHistory)
                {
                    _history.Push(_current);
                }
            }

            _current = next;
            _current.Activate();
        }
    }
}

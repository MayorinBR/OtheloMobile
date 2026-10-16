using System;
using UnityEngine;

namespace Othello.Unity
{
    /// <summary>
    /// Counts down the time left for whichever side currently has the move
    /// in a PvP match, and raises <see cref="TimeExpired"/> once it reaches
    /// zero — <see cref="GameController"/> subscribes to that to force a
    /// loss for whoever ran out of time. Deliberately has no notion of
    /// "whose turn it is" itself; it just counts down and resets, driven
    /// entirely by <see cref="GameController"/> calling
    /// <see cref="ResetTimer"/> after every move.
    /// <para>
    /// Disabled entirely for <see cref="GameMode.VsAI"/> via
    /// <see cref="SetEnabled"/> — there is no time pressure against the AI.
    /// </para>
    /// </summary>
    public sealed class TurnTimerController : MonoBehaviour
    {
        // Seconds allowed per turn in PvP modes; configurable per-project in the Inspector.
        [SerializeField]
        private float _secondsPerTurn = 30f;

        private bool _enabled;

        /// <summary>Seconds left in the current turn.</summary>
        public float SecondsRemaining { get; private set; }

        /// <summary>Whether the countdown is currently ticking.</summary>
        public bool IsRunning { get; private set; }

        /// <summary>Raised once, the moment <see cref="SecondsRemaining"/> reaches zero.</summary>
        public event Action TimeExpired;

        /// <summary>Raised every frame the countdown ticks, with the new <see cref="SecondsRemaining"/> value — a HUD label binds to this to show a live countdown.</summary>
        public event Action<float> TimeRemainingChanged;

        /// <summary>
        /// Enables or disables the timer for the current match. Disabling
        /// also stops any countdown in progress.
        /// </summary>
        public void SetEnabled(bool enabled)
        {
            _enabled = enabled;
            if (!enabled)
            {
                StopTimer();
            }
        }

        /// <summary>
        /// Restarts the countdown at the full <see cref="_secondsPerTurn"/>.
        /// Does nothing while disabled (see <see cref="SetEnabled"/>).
        /// </summary>
        public void ResetTimer()
        {
            if (!_enabled)
            {
                return;
            }

            SecondsRemaining = Mathf.Max(0f, _secondsPerTurn);
            IsRunning = true;
            TimeRemainingChanged?.Invoke(SecondsRemaining);
        }

        /// <summary>Stops the countdown without raising <see cref="TimeExpired"/>.</summary>
        public void StopTimer()
        {
            IsRunning = false;
        }

        private void Update()
        {
            if (!IsRunning)
            {
                return;
            }

            SecondsRemaining -= Time.deltaTime;
            if (SecondsRemaining <= 0f)
            {
                SecondsRemaining = 0f;
                IsRunning = false;
                TimeRemainingChanged?.Invoke(0f);
                TimeExpired?.Invoke();
                return;
            }

            TimeRemainingChanged?.Invoke(SecondsRemaining);
        }
    }
}

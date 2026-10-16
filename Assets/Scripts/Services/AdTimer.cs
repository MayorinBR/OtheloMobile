using System;

namespace Othello.Services
{
    /// <summary>
    /// Accumulates active play time and reports when an interstitial is due.
    /// Deliberately has no idea what a frame or a match is — a MonoBehaviour
    /// calls <see cref="Tick"/> with elapsed seconds every frame, and calls
    /// <see cref="Reset"/> after actually showing the ad — which is what
    /// makes the threshold logic itself testable without Unity running.
    /// </summary>
    public sealed class AdTimer
    {
        private readonly float _intervalSeconds;
        private float _elapsedSeconds;

        public AdTimer(float intervalMinutes)
        {
            if (intervalMinutes <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(intervalMinutes), "O intervalo precisa ser maior que zero.");
            }

            _intervalSeconds = intervalMinutes * 60f;
        }

        /// <summary>True once accumulated play time has reached the configured interval.</summary>
        public bool IsDue => _elapsedSeconds >= _intervalSeconds;

        public void Tick(float deltaSeconds)
        {
            if (deltaSeconds < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(deltaSeconds), "Delta de tempo não pode ser negativo.");
            }

            _elapsedSeconds += deltaSeconds;
        }

        /// <summary>Call after actually showing the ad, to start counting toward the next one.</summary>
        public void Reset()
        {
            _elapsedSeconds = 0f;
        }
    }
}

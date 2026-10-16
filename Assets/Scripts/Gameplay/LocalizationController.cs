using Othello.Services;
using UnityEngine;

namespace Othello.Unity
{
    /// <summary>
    /// Owns the scene's single <see cref="LocalizationService"/>. Built
    /// eagerly in <see cref="Awake"/>, like <see cref="MonetizationController.State"/>,
    /// since it has no dependencies on any other component — every other
    /// script can safely read <see cref="Service"/> from its own <c>Start</c>
    /// onward, because Unity guarantees every <c>Awake</c> finishes before
    /// any <c>Start</c> runs.
    /// </summary>
    public sealed class LocalizationController : MonoBehaviour
    {
        public LocalizationService Service { get; private set; }

        private void Awake()
        {
            Service = new LocalizationService();
        }
    }
}

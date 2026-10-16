using Othello.Services;
using UnityEngine;

namespace Othello.Unity
{
    /// <summary>
    /// Owns the <see cref="ILeaderboardService"/> the Ranking screen queries.
    /// Built eagerly in <see cref="Awake"/> — like
    /// <see cref="LocalizationController"/>, it has no dependencies on any
    /// other component, so anything that needs <see cref="Service"/> can
    /// safely read it from its own <c>Start</c> onward.
    /// <para>
    /// To move off the local mock once a real ranked backend exists, replace
    /// the assignment in <see cref="Awake"/> with the real implementation —
    /// nothing else in the game references <see cref="LocalLeaderboardService"/> directly.
    /// </para>
    /// </summary>
    public sealed class LeaderboardController : MonoBehaviour
    {
        public ILeaderboardService Service { get; private set; }

        private void Awake()
        {
            Service = new FirebaseLeaderboardService();
        }
    }
}

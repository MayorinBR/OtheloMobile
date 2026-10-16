using Othello.Services;
using UnityEngine;

namespace Othello.Unity
{
    /// <summary>
    /// Owns the <see cref="IOnlineMatchService"/> used to find an online
    /// opponent. Built eagerly in <see cref="Awake"/> — like
    /// <see cref="LocalizationController"/>, it has no dependencies on any
    /// other component, so anything that needs <see cref="Service"/> can
    /// safely read it from its own <c>Start</c> onward.
    /// <para>
    /// Defaults to <see cref="LocalOnlineMatchService"/>, the same as
    /// <see cref="LeaderboardController"/> defaults to
    /// <see cref="LocalLeaderboardService"/>. The real backend
    /// (<see cref="FirebaseOnlineMatchService"/>) is a full drop-in
    /// replacement — swap the assignment in <see cref="Awake"/> once the
    /// Firestore project has its <c>matchmakingQueue</c> and
    /// <c>onlineMatches</c> collections covered by security rules (see
    /// that class's own documentation for the schema and the known
    /// limitations). Nothing else in the game references either
    /// implementation directly.
    /// </para>
    /// </summary>
    public sealed class OnlineMatchController : MonoBehaviour
    {
        public IOnlineMatchService Service { get; private set; }

        /// <summary>
        /// The real move relay to whichever opponent <see cref="Service"/>
        /// last found, read live through <see cref="IOnlineMoveRelayProvider"/>
        /// when <see cref="Service"/> implements it. Stays null with today's
        /// local mock, which implements neither the provider interface nor
        /// has a real opponent to relay moves to or from —
        /// <see cref="GameController"/> falls back to an AI standing in for
        /// the opponent whenever this is null.
        /// </summary>
        public IOnlineMoveRelay Relay => (Service as IOnlineMoveRelayProvider)?.Relay;

        private void Awake()
        {
            Service = new LocalOnlineMatchService();
        }

        private void Update()
        {
            Service.Tick(Time.deltaTime);
        }
    }
}

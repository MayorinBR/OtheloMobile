namespace Othello.Services
{
    /// <summary>
    /// Implemented by an <see cref="IOnlineMatchService"/> that also owns a
    /// real <see cref="IOnlineMoveRelay"/> once it finds a match —
    /// <see cref="Othello.Unity.FirebaseOnlineMatchService"/>, once wired
    /// in. Kept as a separate interface
    /// rather than added to <see cref="IOnlineMatchService"/> itself so
    /// <see cref="LocalOnlineMatchService"/> (which has no real relay to
    /// offer) doesn't need to implement it at all;
    /// <see cref="OnlineMatchController.Relay"/> reads through this
    /// interface when <see cref="OnlineMatchController.Service"/> supports
    /// it, and stays null otherwise.
    /// </summary>
    public interface IOnlineMoveRelayProvider
    {
        /// <summary>The relay to the opponent found by the most recent match, or null before one is found.</summary>
        IOnlineMoveRelay Relay { get; }
    }
}

using Othello.Core;

namespace Othello.AI
{
    /// <summary>
    /// Anything that can pick a move for the side to move in a given position.
    /// Implementations must not mutate <paramref name="state"/> — it is a
    /// value type, but the contract is stated explicitly since callers rely on it.
    /// </summary>
    public interface IOthelloAI
    {
        /// <summary>
        /// Chooses a square to play for <c>state.CurrentPlayer</c>.
        /// </summary>
        /// <param name="state">Position to move in. Must have at least one legal move.</param>
        /// <returns>Index (0-63) of the chosen square.</returns>
        int SelectMove(BoardState state);
    }
}

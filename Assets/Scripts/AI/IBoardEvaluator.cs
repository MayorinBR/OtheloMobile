using Othello.Core;

namespace Othello.AI
{
    /// <summary>
    /// Scores a position from the perspective of the side to move: positive
    /// means the position favors <c>state.CurrentPlayer</c>, negative favors the
    /// opponent. This sign convention (rather than always scoring from Black's
    /// perspective) is what lets the search use the compact negamax form.
    /// </summary>
    public interface IBoardEvaluator
    {
        int Evaluate(BoardState state);
    }
}

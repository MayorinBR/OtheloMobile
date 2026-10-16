using Othello.Core;

namespace Othello.AI
{
    /// <summary>
    /// Evaluates a position purely by <see cref="PositionWeights"/>: corners and
    /// edges count for a lot, squares next to a corner count against you. Cheap
    /// to compute, which is what makes it suitable for the Medium AI's shallower,
    /// faster search.
    /// </summary>
    public sealed class PositionalEvaluator : IBoardEvaluator
    {
        public int Evaluate(BoardState state)
        {
            int score = PositionalDifference(state);
            return state.CurrentPlayer == Player.Black ? score : -score;
        }

        /// <summary>Black's total weight minus White's, independent of whose turn it is.</summary>
        internal static int PositionalDifference(BoardState state)
        {
            int black = 0;
            int white = 0;
            for (int square = 0; square < 64; square++)
            {
                ulong bit = 1UL << square;
                if ((state.Black & bit) != 0)
                {
                    black += PositionWeights.Table[square];
                }
                else if ((state.White & bit) != 0)
                {
                    white += PositionWeights.Table[square];
                }
            }
            return black - white;
        }
    }
}

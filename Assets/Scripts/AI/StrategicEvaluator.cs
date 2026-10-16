using Othello.Core;

namespace Othello.AI
{
    /// <summary>
    /// Evaluator for the Hard AI: combines positional weights with mobility
    /// (fewer options for the opponent is good even before material shows it)
    /// and direct corner control, then switches to exact disc counting once the
    /// board is full and no heuristic is needed anymore. This is the extra
    /// signal that lets a deep search actually convert an advantage instead of
    /// just seeing further into a flat evaluation.
    /// </summary>
    public sealed class StrategicEvaluator : IBoardEvaluator
    {
        private const int MobilityWeight = 15;
        private const int CornerWeight = 25;
        private const int TerminalWeight = 100_000;

        private static readonly ulong CornerMask =
            (1UL << 0) | (1UL << 7) | (1UL << 56) | (1UL << 63);

        public int Evaluate(BoardState state)
        {
            int occupied = Bitboard.PopCount(state.Black | state.White);
            if (occupied == 64)
            {
                return FinalDiscDifference(state) * TerminalWeight;
            }

            int positional = PositionalEvaluator.PositionalDifference(state);
            int mobility = MobilityDifference(state);
            int corners = CornerDifference(state);

            int scoreForBlack = positional + mobility * MobilityWeight + corners * CornerWeight;
            return state.CurrentPlayer == Player.Black ? scoreForBlack : -scoreForBlack;
        }

        private static int MobilityDifference(BoardState state)
        {
            int black = Bitboard.PopCount(Bitboard.ValidMoves(state.Black, state.White));
            int white = Bitboard.PopCount(Bitboard.ValidMoves(state.White, state.Black));
            return black - white;
        }

        private static int CornerDifference(BoardState state)
        {
            int black = Bitboard.PopCount(state.Black & CornerMask);
            int white = Bitboard.PopCount(state.White & CornerMask);
            return black - white;
        }

        private static int FinalDiscDifference(BoardState state)
        {
            int diff = state.DiscCount(Player.Black) - state.DiscCount(Player.White);
            return state.CurrentPlayer == Player.Black ? diff : -diff;
        }
    }
}

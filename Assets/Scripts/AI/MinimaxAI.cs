using System;
using System.Collections.Generic;
using Othello.Core;

#nullable enable

namespace Othello.AI
{
    /// <summary>
    /// Minimax search with alpha-beta pruning, written in negamax form (each
    /// recursive call returns the score from the mover's own perspective and
    /// negates it for the caller, so there is a single recursive method instead
    /// of separate maximizing/minimizing branches).
    /// <para>
    /// Configurable enough to serve both the Medium AI (shallow depth, cheap
    /// evaluator, no transposition table) and the Hard AI (deep search, richer
    /// evaluator, transposition table, and an endgame mode that searches to the
    /// exact end of the game once few enough squares remain empty).
    /// </para>
    /// </summary>
    public sealed class MinimaxAI : IOthelloAI
    {
        private readonly int _maxDepth;
        private readonly IBoardEvaluator _evaluator;
        private readonly TranspositionTable? _transpositionTable;
        private readonly int _endgameExactThreshold;

        /// <param name="maxDepth">Plies to search from the root.</param>
        /// <param name="evaluator">Heuristic used to score leaf positions.</param>
        /// <param name="useTranspositionTable">Memoize positions seen more than once in this search.</param>
        /// <param name="endgameExactThreshold">
        /// When the number of empty squares drops to this value or below, search
        /// depth is extended to solve the game exactly instead of stopping at
        /// <paramref name="maxDepth"/>. Zero disables the extension.
        /// </param>
        public MinimaxAI(
            int maxDepth,
            IBoardEvaluator evaluator,
            bool useTranspositionTable = false,
            int endgameExactThreshold = 0)
        {
            if (maxDepth < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(maxDepth), "A busca precisa de profundidade de pelo menos 1.");
            }

            _maxDepth = maxDepth;
            _evaluator = evaluator;
            _transpositionTable = useTranspositionTable ? new TranspositionTable() : null;
            _endgameExactThreshold = endgameExactThreshold;
        }

        public int SelectMove(BoardState state)
        {
            var moves = state.GetValidMoves();
            if (moves.Count == 0)
            {
                throw new InvalidOperationException("SelectMove foi chamado numa posição sem jogadas válidas.");
            }

            _transpositionTable?.Clear();

            int emptySquares = 64 - Bitboard.PopCount(state.Black | state.White);
            int depth = _endgameExactThreshold > 0 && emptySquares <= _endgameExactThreshold
                ? emptySquares
                : _maxDepth;

            int bestMove = moves[0];
            int bestScore = int.MinValue;
            int alpha = int.MinValue + 1;
            const int beta = int.MaxValue - 1;

            foreach (int move in OrderByStaticWeight(moves))
            {
                var next = state.ApplyMove(move);
                int score = -Negamax(next, depth - 1, -beta, -alpha);

                if (score > bestScore)
                {
                    bestScore = score;
                    bestMove = move;
                }
                if (score > alpha)
                {
                    alpha = score;
                }
            }

            return bestMove;
        }

        private int Negamax(BoardState state, int depth, int alpha, int beta)
        {
            if (depth <= 0)
            {
                return _evaluator.Evaluate(state);
            }

            var moves = state.GetValidMoves();
            if (moves.Count == 0)
            {
                var passed = state.PassTurn();
                return passed.GetValidMovesMask() == 0
                    ? _evaluator.Evaluate(state)
                    : -Negamax(passed, depth - 1, -beta, -alpha);
            }

            ulong hash = 0;
            if (_transpositionTable != null)
            {
                hash = Zobrist.Hash(state);
                if (_transpositionTable.TryGetScore(hash, depth, alpha, beta, out int cached))
                {
                    return cached;
                }
            }

            int originalAlpha = alpha;
            int best = int.MinValue + 1;

            foreach (int move in OrderByStaticWeight(moves))
            {
                var next = state.ApplyMove(move);
                int score = -Negamax(next, depth - 1, -beta, -alpha);

                if (score > best)
                {
                    best = score;
                }
                if (best > alpha)
                {
                    alpha = best;
                }
                if (alpha >= beta)
                {
                    break; // beta cutoff: the opponent already has a better alternative elsewhere
                }
            }

            _transpositionTable?.Store(hash, depth, best, originalAlpha, beta);
            return best;
        }

        /// <summary>
        /// Cheap move ordering by the static weight of the destination square.
        /// Trying likely-strong moves (corners, edges) first lets alpha-beta
        /// prune far more branches than searching moves in board order.
        /// </summary>
        private static List<int> OrderByStaticWeight(List<int> moves)
        {
            var ordered = new List<int>(moves);
            ordered.Sort((a, b) => PositionWeights.Table[b].CompareTo(PositionWeights.Table[a]));
            return ordered;
        }
    }
}

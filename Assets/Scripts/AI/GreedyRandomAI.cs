using System;
using System.Collections.Generic;
using Othello.Core;

#nullable enable

namespace Othello.AI
{
    /// <summary>
    /// Easy AI: either a uniformly random legal move, or the greedy move that
    /// flips the most opponent discs this turn — no lookahead, no notion that a
    /// greedy capture now can hand over a corner later. Intentionally weak.
    /// </summary>
    public sealed class GreedyRandomAI : IOthelloAI
    {
        public enum Strategy
        {
            Random,
            Greedy
        }

        private readonly Strategy _strategy;
        private readonly Random _random;

        public GreedyRandomAI(Strategy strategy, Random? random = null)
        {
            _strategy = strategy;
            _random = random ?? new Random();
        }

        public int SelectMove(BoardState state)
        {
            var moves = state.GetValidMoves();
            if (moves.Count == 0)
            {
                throw new InvalidOperationException("SelectMove foi chamado numa posição sem jogadas válidas.");
            }

            return _strategy == Strategy.Random
                ? moves[_random.Next(moves.Count)]
                : MostCapturingMove(state, moves);
        }

        private static int MostCapturingMove(BoardState state, List<int> moves)
        {
            var mover = state.CurrentPlayer;
            int discsBefore = state.DiscCount(mover);

            int bestMove = moves[0];
            int bestCaptured = -1;

            foreach (int move in moves)
            {
                int discsAfter = state.ApplyMove(move).DiscCount(mover);
                int captured = discsAfter - discsBefore - 1; // -1 excludes the disc just placed
                if (captured > bestCaptured)
                {
                    bestCaptured = captured;
                    bestMove = move;
                }
            }

            return bestMove;
        }
    }
}

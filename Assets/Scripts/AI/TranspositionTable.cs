using System.Collections.Generic;

namespace Othello.AI
{
    /// <summary>
    /// How the stored score of a <see cref="TTEntry"/> relates to the true value
    /// of the position, given that alpha-beta pruning can cut a search short
    /// before the exact value is known.
    /// </summary>
    public enum TTFlag : byte
    {
        /// <summary>The stored score is the position's exact minimax value.</summary>
        Exact,

        /// <summary>The true value is at least this score (a beta cutoff occurred).</summary>
        LowerBound,

        /// <summary>The true value is at most this score (nothing beat alpha).</summary>
        UpperBound
    }

    /// <summary>One memoized search result for a position.</summary>
    public readonly struct TTEntry
    {
        public int Depth { get; }
        public int Score { get; }
        public TTFlag Flag { get; }

        public TTEntry(int depth, int score, TTFlag flag)
        {
            Depth = depth;
            Score = score;
            Flag = flag;
        }
    }

    /// <summary>
    /// Memoizes minimax results keyed by <see cref="Zobrist"/> hash, so
    /// transpositions — the same position reached through different move orders
    /// — are only ever searched once per top-level call. Cleared at the start of
    /// each <c>SelectMove</c> rather than kept across moves, which keeps the
    /// depth/bound bookkeeping simple at the cost of some reusable work between
    /// consecutive AI turns.
    /// </summary>
    public sealed class TranspositionTable
    {
        private readonly Dictionary<ulong, TTEntry> _entries = new();

        public void Clear() => _entries.Clear();

        /// <summary>
        /// Looks up <paramref name="hash"/> and, if a stored result at least as
        /// deep as <paramref name="depth"/> settles the search window
        /// <c>[alpha, beta)</c>, returns the usable score.
        /// </summary>
        public bool TryGetScore(ulong hash, int depth, int alpha, int beta, out int score)
        {
            score = 0;
            if (!_entries.TryGetValue(hash, out var entry) || entry.Depth < depth)
            {
                return false;
            }

            switch (entry.Flag)
            {
                case TTFlag.Exact:
                    score = entry.Score;
                    return true;
                case TTFlag.LowerBound when entry.Score >= beta:
                    score = entry.Score;
                    return true;
                case TTFlag.UpperBound when entry.Score <= alpha:
                    score = entry.Score;
                    return true;
                default:
                    return false;
            }
        }

        public void Store(ulong hash, int depth, int score, int alpha, int beta)
        {
            TTFlag flag = score <= alpha ? TTFlag.UpperBound
                : score >= beta ? TTFlag.LowerBound
                : TTFlag.Exact;

            _entries[hash] = new TTEntry(depth, score, flag);
        }
    }
}

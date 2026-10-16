using System;
using Othello.Core;

namespace Othello.AI
{
    /// <summary>
    /// Zobrist hashing: assigns a random 64-bit number to every (square, color)
    /// combination once, then hashes any position by XOR-ing together the
    /// numbers for its occupied squares. XOR makes the hash cheap to update
    /// incrementally and, in practice, collision-free enough to key a
    /// transposition table. The table is seeded deterministically so hashes are
    /// reproducible across runs, which is only useful for debugging — any seed
    /// works equally well for the table's actual purpose.
    /// </summary>
    public static class Zobrist
    {
        private static readonly ulong[,] SquareKeys;
        private static readonly ulong SideToMoveKey;

        static Zobrist()
        {
            var random = new Random(20260826);
            SquareKeys = new ulong[64, 2];
            for (int square = 0; square < 64; square++)
            {
                SquareKeys[square, 0] = NextUInt64(random);
                SquareKeys[square, 1] = NextUInt64(random);
            }
            SideToMoveKey = NextUInt64(random);
        }

        private static ulong NextUInt64(Random random)
        {
            Span<byte> buffer = stackalloc byte[8];
            random.NextBytes(buffer);
            return BitConverter.ToUInt64(buffer);
        }

        public static ulong Hash(BoardState state)
        {
            ulong hash = 0;

            ulong black = state.Black;
            while (black != 0)
            {
                int square = Bitboard.TrailingZeroCount(black);
                hash ^= SquareKeys[square, 0];
                black &= black - 1;
            }

            ulong white = state.White;
            while (white != 0)
            {
                int square = Bitboard.TrailingZeroCount(white);
                hash ^= SquareKeys[square, 1];
                white &= white - 1;
            }

            if (state.CurrentPlayer == Player.White)
            {
                hash ^= SideToMoveKey;
            }

            return hash;
        }
    }
}

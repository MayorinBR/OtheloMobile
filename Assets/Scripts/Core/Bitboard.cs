using System;
using System.Collections.Generic;

namespace Othello.Core
{
    /// <summary>
    /// Pure bit-manipulation core of the Othello engine. The board is two 64-bit
    /// masks (one per side); bit <c>row * 8 + col</c> represents the square at
    /// that row/column, row 0 at the top and column 0 on the left.
    /// <para>
    /// Move generation and flipping both use the same "flood shift" technique:
    /// starting from a piece and stepping in one of the 8 directions, keep
    /// extending across opposing discs and inspect what lies one step beyond the
    /// run. This is the standard bitboard algorithm used by fast Othello engines,
    /// and it is what makes evaluating thousands of positions per second
    /// practical for the AI.
    /// </para>
    /// </summary>
    public static class Bitboard
    {
        private const ulong NotFileA = ~0x0101010101010101UL;
        private const ulong NotFileH = ~0x8080808080808080UL;

        private enum Direction
        {
            North,
            South,
            East,
            West,
            NorthEast,
            NorthWest,
            SouthEast,
            SouthWest
        }

        private static readonly Direction[] AllDirections =
        {
            Direction.North, Direction.South, Direction.East, Direction.West,
            Direction.NorthEast, Direction.NorthWest, Direction.SouthEast, Direction.SouthWest
        };

        /// <summary>
        /// Shifts every bit in <paramref name="bits"/> one square in
        /// <paramref name="dir"/>, masking out the file a piece would otherwise
        /// wrap around from (e.g. column H wrapping into column A of the next row).
        /// </summary>
        private static ulong Shift(ulong bits, Direction dir) => dir switch
        {
            Direction.North => bits >> 8,
            Direction.South => bits << 8,
            Direction.East => (bits & NotFileH) << 1,
            Direction.West => (bits & NotFileA) >> 1,
            Direction.NorthEast => (bits & NotFileH) >> 7,
            Direction.NorthWest => (bits & NotFileA) >> 9,
            Direction.SouthEast => (bits & NotFileH) << 9,
            Direction.SouthWest => (bits & NotFileA) << 7,
            _ => throw new ArgumentOutOfRangeException(nameof(dir))
        };

        /// <summary>
        /// Computes every square <paramref name="player"/> may legally play on.
        /// </summary>
        /// <param name="player">Bitboard of the side to move.</param>
        /// <param name="opponent">Bitboard of the other side.</param>
        /// <returns>A mask with one bit set per legal move.</returns>
        public static ulong ValidMoves(ulong player, ulong opponent)
        {
            ulong empty = ~(player | opponent);
            ulong moves = 0;

            foreach (var dir in AllDirections)
            {
                ulong flood = Shift(player, dir) & opponent;
                for (int i = 0; i < 5; i++)
                {
                    flood |= Shift(flood, dir) & opponent;
                }
                moves |= Shift(flood, dir) & empty;
            }

            return moves;
        }

        /// <summary>
        /// Plays <paramref name="square"/> for <paramref name="player"/> and
        /// returns the resulting bitboards with every bracketed opponent disc
        /// flipped. Does not validate that the move is legal — callers must
        /// check <see cref="ValidMoves"/> first.
        /// </summary>
        /// <param name="square">Index (0-63) of the square being played.</param>
        /// <param name="player">Bitboard of the side to move.</param>
        /// <param name="opponent">Bitboard of the other side.</param>
        /// <returns>The updated player and opponent bitboards after flips.</returns>
        public static (ulong player, ulong opponent) ApplyMove(int square, ulong player, ulong opponent)
        {
            ulong moveBit = 1UL << square;
            ulong captured = 0;

            foreach (var dir in AllDirections)
            {
                ulong flood = Shift(moveBit, dir) & opponent;
                for (int i = 0; i < 5; i++)
                {
                    flood |= Shift(flood, dir) & opponent;
                }
                if ((Shift(flood, dir) & player) != 0)
                {
                    captured |= flood;
                }
            }

            ulong newPlayer = player | moveBit | captured;
            ulong newOpponent = opponent & ~captured;
            return (newPlayer, newOpponent);
        }

        /// <summary>
        /// Expands a bitmask into the list of set square indices, ascending.
        /// </summary>
        public static List<int> BitsToSquares(ulong bits)
        {
            var squares = new List<int>();
            while (bits != 0)
            {
                squares.Add(TrailingZeroCount(bits));
                bits &= bits - 1; // clear the lowest set bit
            }
            return squares;
        }

        /// <summary>
        /// Index of the lowest set bit, or 64 if <paramref name="bits"/> is zero.
        /// </summary>
        public static int TrailingZeroCount(ulong bits)
        {
            if (bits == 0) return 64;
            int n = 0;
            while ((bits & 1) == 0)
            {
                bits >>= 1;
                n++;
            }
            return n;
        }

        /// <summary>
        /// Number of set bits in <paramref name="bits"/> (population count).
        /// </summary>
        public static int PopCount(ulong bits)
        {
            bits -= (bits >> 1) & 0x5555555555555555UL;
            bits = (bits & 0x3333333333333333UL) + ((bits >> 2) & 0x3333333333333333UL);
            bits = (bits + (bits >> 4)) & 0x0f0f0f0f0f0f0f0fUL;
            return (int)((bits * 0x0101010101010101UL) >> 56);
        }
    }
}

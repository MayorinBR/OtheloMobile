using System.Collections.Generic;

namespace Othello.Core
{
    /// <summary>
    /// Immutable snapshot of an Othello position: which squares each side
    /// occupies and whose turn it is. Every mutating operation returns a new
    /// value rather than modifying the instance, which is what makes this type
    /// safe to pass around a minimax search by value with no aliasing bugs and
    /// no per-node heap allocation for the board itself.
    /// </summary>
    public readonly struct BoardState
    {
        public ulong Black { get; }
        public ulong White { get; }
        public Player CurrentPlayer { get; }

        public BoardState(ulong black, ulong white, Player currentPlayer)
        {
            Black = black;
            White = white;
            CurrentPlayer = currentPlayer;
        }

        /// <summary>Bitboard of the side whose turn it currently is.</summary>
        public ulong PlayerBoard => CurrentPlayer == Player.Black ? Black : White;

        /// <summary>Bitboard of the side waiting for the current player to move.</summary>
        public ulong OpponentBoard => CurrentPlayer == Player.Black ? White : Black;

        /// <summary>
        /// The standard Othello starting position: two discs of each color on
        /// the four center squares, Black to move.
        /// </summary>
        public static BoardState CreateStartPosition()
        {
            static int Square(int row, int col) => row * 8 + col;

            ulong white = (1UL << Square(3, 3)) | (1UL << Square(4, 4));
            ulong black = (1UL << Square(3, 4)) | (1UL << Square(4, 3));
            return new BoardState(black, white, Player.Black);
        }

        /// <summary>Mask of every square the current player may legally play.</summary>
        public ulong GetValidMovesMask() => Bitboard.ValidMoves(PlayerBoard, OpponentBoard);

        /// <summary>Square indices the current player may legally play, ascending.</summary>
        public List<int> GetValidMoves() => Bitboard.BitsToSquares(GetValidMovesMask());

        /// <summary>
        /// Plays <paramref name="square"/> for the current player and returns the
        /// resulting state with the turn passed to the opponent. Does not check
        /// legality — validate against <see cref="GetValidMoves"/> first.
        /// </summary>
        public BoardState ApplyMove(int square)
        {
            var (newPlayer, newOpponent) = Bitboard.ApplyMove(square, PlayerBoard, OpponentBoard);
            return CurrentPlayer == Player.Black
                ? new BoardState(newPlayer, newOpponent, Player.White)
                : new BoardState(newOpponent, newPlayer, Player.Black);
        }

        /// <summary>
        /// Hands the turn to the opponent without placing a disc. Used when the
        /// current player has no legal move — a forced pass under Othello rules.
        /// </summary>
        public BoardState PassTurn() => new(Black, White, CurrentPlayer.Opponent());

        /// <summary>
        /// True when neither side has a legal move, meaning the game has ended.
        /// </summary>
        public bool IsTerminal() => GetValidMovesMask() == 0 && PassTurn().GetValidMovesMask() == 0;

        /// <summary>Number of discs <paramref name="player"/> currently has on the board.</summary>
        public int DiscCount(Player player) => Bitboard.PopCount(player == Player.Black ? Black : White);
    }
}

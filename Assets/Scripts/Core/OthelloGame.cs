using System;
using System.Collections.Generic;

#nullable enable

namespace Othello.Core
{
    /// <summary>
    /// Mutable, app-facing wrapper around <see cref="BoardState"/>. Owns turn
    /// flow — including automatically skipping a side that has no legal move,
    /// as Othello's rules require — and raises events a UI layer can subscribe
    /// to instead of polling state every frame.
    /// </summary>
    public sealed class OthelloGame
    {
        /// <summary>Raised after a move is applied, with the square played and who played it.</summary>
        public event Action<int, Player>? MoveApplied;

        /// <summary>Raised once, the moment neither side has a legal move left.</summary>
        public event Action? GameEnded;

        private Player? _forcedWinner;

        public BoardState State { get; private set; }
        public bool IsGameOver { get; private set; }

        public OthelloGame() : this(BoardState.CreateStartPosition())
        {
        }

        public OthelloGame(BoardState initialState)
        {
            State = initialState;
            IsGameOver = State.IsTerminal();
        }

        /// <summary>Square indices the player to move may legally play.</summary>
        public IReadOnlyList<int> GetValidMoves() => State.GetValidMoves();

        /// <summary>
        /// Attempts to play <paramref name="square"/> for the current player.
        /// </summary>
        /// <param name="square">Index (0-63) of the square to play.</param>
        /// <returns>
        /// False if the game has ended or the square is not a legal move —
        /// state is left unchanged in that case.
        /// </returns>
        public bool TryPlayMove(int square)
        {
            if (IsGameOver || !State.GetValidMoves().Contains(square))
            {
                return false;
            }

            var mover = State.CurrentPlayer;
            State = State.ApplyMove(square);
            MoveApplied?.Invoke(square, mover);

            SkipPlayersWithNoMove();
            return true;
        }

        private void SkipPlayersWithNoMove()
        {
            if (State.GetValidMovesMask() != 0)
            {
                return;
            }

            var passed = State.PassTurn();
            if (passed.GetValidMovesMask() != 0)
            {
                State = passed;
                return;
            }

            IsGameOver = true;
            GameEnded?.Invoke();
        }

        /// <summary>
        /// Ends the match immediately with <paramref name="winner"/> declared
        /// the winner, regardless of the current board state — used for a
        /// resignation or a turn timer expiring. No-op if the game has
        /// already ended, by either this or the normal no-more-legal-moves
        /// path, so a duplicate or late call can never overwrite an
        /// already-decided result.
        /// </summary>
        /// <param name="winner">The player to declare the winner.</param>
        public void ForceEnd(Player winner)
        {
            if (IsGameOver)
            {
                return;
            }

            _forcedWinner = winner;
            IsGameOver = true;
            GameEnded?.Invoke();
        }

        /// <summary>
        /// The winner once the game has ended, or null while it's still in
        /// progress or if it ended in a tie.
        /// </summary>
        public Player? GetWinner()
        {
            if (!IsGameOver)
            {
                return null;
            }

            if (_forcedWinner.HasValue)
            {
                return _forcedWinner;
            }

            int black = State.DiscCount(Player.Black);
            int white = State.DiscCount(Player.White);
            if (black == white)
            {
                return null;
            }

            return black > white ? Player.Black : Player.White;
        }
    }
}

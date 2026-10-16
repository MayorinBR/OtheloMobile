namespace Othello.Core
{
    /// <summary>
    /// The two sides that play Othello. Values are ordinal, not bit masks —
    /// board occupancy is tracked separately per side in <see cref="BoardState"/>.
    /// </summary>
    public enum Player
    {
        Black,
        White
    }

    /// <summary>
    /// Convenience extensions over <see cref="Player"/>.
    /// </summary>
    public static class PlayerExtensions
    {
        /// <summary>
        /// Returns the opposing side.
        /// </summary>
        /// <param name="player">The side to flip.</param>
        /// <returns>Black for White and White for Black.</returns>
        public static Player Opponent(this Player player) =>
            player == Player.Black ? Player.White : Player.Black;
    }
}

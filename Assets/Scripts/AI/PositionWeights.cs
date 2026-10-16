namespace Othello.AI
{
    /// <summary>
    /// Classic static positional weight table for Othello. Corners are the only
    /// squares that, once taken, can never be recaptured, so they are weighted
    /// far above everything else; the squares diagonally adjacent to a corner
    /// are penalized because playing them early tends to hand the corner to the
    /// opponent instead.
    /// </summary>
    public static class PositionWeights
    {
        /// <summary>Weight per square, indexed by <c>row * 8 + col</c>.</summary>
        public static readonly int[] Table = BuildTable();

        private static int[] BuildTable()
        {
            int[,] matrix =
            {
                { 120, -20, 20, 5, 5, 20, -20, 120 },
                { -20, -40, -5, -5, -5, -5, -40, -20 },
                { 20, -5, 15, 3, 3, 15, -5, 20 },
                { 5, -5, 3, 3, 3, 3, -5, 5 },
                { 5, -5, 3, 3, 3, 3, -5, 5 },
                { 20, -5, 15, 3, 3, 15, -5, 20 },
                { -20, -40, -5, -5, -5, -5, -40, -20 },
                { 120, -20, 20, 5, 5, 20, -20, 120 }
            };

            var table = new int[64];
            for (int row = 0; row < 8; row++)
            {
                for (int col = 0; col < 8; col++)
                {
                    table[row * 8 + col] = matrix[row, col];
                }
            }
            return table;
        }
    }
}

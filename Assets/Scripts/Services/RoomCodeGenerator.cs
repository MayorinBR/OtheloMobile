using System;

namespace Othello.Services
{
    /// <summary>
    /// Generates the short room codes a friendly online match is created
    /// and joined under (see <see cref="IOnlineMatchService.CreateFriendlyMatch"/>).
    /// The alphabet deliberately excludes characters easily confused with
    /// each other when read aloud or typed on a phone keyboard —
    /// <c>O</c>/<c>0</c> and <c>I</c>/<c>1</c> — since the whole point of a
    /// short code is that a player can read it to a friend or type it in
    /// without a mistake.
    /// </summary>
    public static class RoomCodeGenerator
    {
        private const string Alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        private static readonly Random Rng = new Random();

        /// <summary>Generates a random uppercase code of <paramref name="length"/> characters.</summary>
        public static string Generate(int length = 6)
        {
            var chars = new char[length];
            for (int i = 0; i < length; i++)
            {
                chars[i] = Alphabet[Rng.Next(Alphabet.Length)];
            }
            return new string(chars);
        }

        /// <summary>
        /// Normalizes a code a player typed in — trims whitespace and
        /// uppercases it — so "ab12cd" and " AB12CD " both match the same
        /// room as "AB12CD".
        /// </summary>
        public static string Normalize(string code) => code?.Trim().ToUpperInvariant() ?? string.Empty;
    }
}

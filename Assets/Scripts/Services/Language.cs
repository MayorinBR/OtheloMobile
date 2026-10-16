using System;

namespace Othello.Services
{
    /// <summary>The languages the app's UI can be displayed in. English is the default for a new profile.</summary>
    public enum Language
    {
        English,
        Japanese,
        Spanish,
        French,
        German,
        Italian,
        Portuguese
    }

    /// <summary>Small conveniences over <see cref="Language"/>, kept next to it the way <c>PlayerExtensions</c> sits next to <c>Player</c>.</summary>
    public static class LanguageExtensions
    {
        private static readonly Language[] All = (Language[])Enum.GetValues(typeof(Language));

        /// <summary>
        /// The language's own name for itself (e.g. "日本語" for Japanese),
        /// for a language picker that shows each option in its own
        /// language rather than translating language names.
        /// </summary>
        public static string Autonym(this Language language) => language switch
        {
            Language.English => "English",
            Language.Japanese => "日本語",
            Language.Spanish => "Español",
            Language.French => "Français",
            Language.German => "Deutsch",
            Language.Italian => "Italiano",
            Language.Portuguese => "Português",
            _ => language.ToString()
        };

        /// <summary>The next language in declaration order, wrapping back to the first — drives a simple "cycle language" picker.</summary>
        public static Language Next(this Language language)
        {
            int index = Array.IndexOf(All, language);
            return All[(index + 1) % All.Length];
        }
    }
}

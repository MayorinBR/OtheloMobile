using System;

namespace Othello.Services
{
    /// <summary>
    /// Holds the player's currently selected <see cref="Language"/> and
    /// resolves <see cref="LocalizationKey"/>s against it via
    /// <see cref="LocalizationCatalog"/>. UI subscribes to
    /// <see cref="Changed"/> to refresh its text the instant the language
    /// changes, instead of requiring a scene reload.
    /// </summary>
    public sealed class LocalizationService
    {
        public Language CurrentLanguage { get; private set; } = Language.English;

        /// <summary>Raised after <see cref="SetLanguage"/> actually changes the language.</summary>
        public event Action Changed;

        public void SetLanguage(Language language)
        {
            if (CurrentLanguage == language)
            {
                return;
            }

            CurrentLanguage = language;
            Changed?.Invoke();
        }

        /// <summary>Resolves <paramref name="key"/> in the current language.</summary>
        public string Get(LocalizationKey key) => LocalizationCatalog.Get(CurrentLanguage, key);
    }
}

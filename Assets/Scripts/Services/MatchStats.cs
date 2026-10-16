namespace Othello.Services
{
    /// <summary>
    /// A player's aggregate Vs-AI and online match history: how many they've
    /// played and won, and their current and best win streaks. Local
    /// pass-and-play matches never call <see cref="RecordMatch"/> — there is
    /// no single "the player" perspective to credit a win to when both
    /// sides are human.
    /// </summary>
    public sealed class MatchStats
    {
        public int MatchesPlayed { get; private set; }
        public int Wins { get; private set; }
        public int Losses { get; private set; }
        public int Draws { get; private set; }
        public int OnlineWins { get; private set; }
        public int CurrentWinStreak { get; private set; }
        public int BestWinStreak { get; private set; }

        /// <summary>Folds one finished match into these stats.</summary>
        public void RecordMatch(MatchOutcome outcome, bool isOnline)
        {
            MatchesPlayed++;

            switch (outcome)
            {
                case MatchOutcome.Win:
                    Wins++;
                    if (isOnline)
                    {
                        OnlineWins++;
                    }
                    CurrentWinStreak++;
                    if (CurrentWinStreak > BestWinStreak)
                    {
                        BestWinStreak = CurrentWinStreak;
                    }
                    break;

                case MatchOutcome.Loss:
                    Losses++;
                    CurrentWinStreak = 0;
                    break;

                case MatchOutcome.Draw:
                    Draws++;
                    CurrentWinStreak = 0;
                    break;
            }
        }

        /// <summary>Overwrites these stats with previously saved values, e.g. when a profile loads.</summary>
        public void Restore(int matchesPlayed, int wins, int losses, int draws, int onlineWins, int currentWinStreak, int bestWinStreak)
        {
            MatchesPlayed = matchesPlayed;
            Wins = wins;
            Losses = losses;
            Draws = draws;
            OnlineWins = onlineWins;
            CurrentWinStreak = currentWinStreak;
            BestWinStreak = bestWinStreak;
        }
    }
}

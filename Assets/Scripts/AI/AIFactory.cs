using System;

namespace Othello.AI
{
    /// <summary>The three PvE difficulty levels described in the game's design.</summary>
    public enum Difficulty
    {
        Easy,
        Medium,
        Hard
    }

    /// <summary>
    /// Builds an <see cref="IOthelloAI"/> for a difficulty level. Keeping this as
    /// a single factory method means the difficulty picker in the UI depends
    /// only on the <see cref="Difficulty"/> enum, never on the concrete AI
    /// classes or their tuning parameters.
    /// </summary>
    public static class AIFactory
    {
        public static IOthelloAI Create(Difficulty difficulty) => difficulty switch
        {
            Difficulty.Easy => new GreedyRandomAI(GreedyRandomAI.Strategy.Greedy),

            Difficulty.Medium => new MinimaxAI(
                maxDepth: 4,
                evaluator: new PositionalEvaluator()),

            Difficulty.Hard => new MinimaxAI(
                maxDepth: 8,
                evaluator: new StrategicEvaluator(),
                useTranspositionTable: true,
                endgameExactThreshold: 12),

            _ => throw new ArgumentOutOfRangeException(nameof(difficulty))
        };
    }
}

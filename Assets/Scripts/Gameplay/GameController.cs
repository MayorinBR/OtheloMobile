using System;
using Othello.AI;
using Othello.Core;
using Othello.Services;
using UnityEngine;

namespace Othello.Unity
{
    /// <summary>How the two sides of a match are controlled.</summary>
    public enum GameMode
    {
        /// <summary>Both sides are played by humans sharing the same device.</summary>
        LocalPvP,

        /// <summary>One side is human, the other is played by an <see cref="Othello.AI.IOthelloAI"/>.</summary>
        VsAI,

        /// <summary>
        /// One side is human, the other stands in for a matched
        /// <see cref="OnlineOpponent"/> — played today by an
        /// <see cref="Othello.AI.IOthelloAI"/> picked to roughly match the
        /// opponent's rating, until <see cref="FirebaseOnlineMatchService"/>
        /// replaces that stand-in with real remote moves. Tracked separately from
        /// <see cref="VsAI"/> because only online matches move the player's
        /// <see cref="PlayerSession.Rating"/>.
        /// </summary>
        OnlinePvP
    }

    /// <summary>How the current (or most recently finished) match ended, or is expected to end — see <see cref="GameController.EndReason"/>.</summary>
    public enum MatchEndReason
    {
        /// <summary>Ran out of legal moves for both sides, or hasn't ended yet.</summary>
        Natural,

        /// <summary>Someone hit "Resign" (<see cref="GameController.Resign"/>) — for a mode with a real opponent, this could be either side.</summary>
        Resignation,

        /// <summary>A turn's time ran out (<see cref="GameController.NotifyTurnTimedOut"/>).</summary>
        Timeout
    }

    /// <summary>
    /// Wires the engine (<see cref="Othello.Core"/>) and the AI (<see cref="Othello.AI"/>)
    /// into a Unity scene. It owns turn flow for whichever match is
    /// currently running; it deliberately has no rendering code, so it can
    /// sit under any board visual without change — <see cref="BoardView"/>
    /// binds to it via <see cref="Game"/> and <see cref="MatchStarted"/>.
    /// <para>
    /// A match only exists once <see cref="StartNewMatch"/> is called, from
    /// <see cref="PlayModeScreen"/> after the player picks a mode — there is
    /// no Inspector-fixed match auto-starting at scene load anymore.
    /// </para>
    /// </summary>
    public sealed class GameController : MonoBehaviour
    {
        /// <summary>
        /// Reserved <see cref="IOnlineMoveRelay.SendMove"/> square value
        /// meaning "I forfeit" — see <see cref="IOnlineMoveRelay.SendMove"/>.
        /// </summary>
        private const int ForfeitSentinel = -1;

        /// <summary>
        /// Flavor identities for the "random opponent" profile shown next to
        /// a <see cref="GameMode.VsAI"/> match — there is no real opponent to
        /// name, so the profile panel gets a small, fixed persona instead of
        /// staying blank. Purely cosmetic; never affects AI strength.
        /// </summary>
        private static readonly (string Name, string AvatarId)[] AiPersonaPool =
        {
            ("Sombra", InventoryCatalog.AvatarFox),
            ("Nébula", InventoryCatalog.AvatarOwl),
            ("Vortex", InventoryCatalog.AvatarCat),
            ("Ronin", InventoryCatalog.AvatarPanda),
            ("Ecos", InventoryCatalog.AvatarDefault)
        };

        private static readonly System.Random PersonaRng = new System.Random();

        [SerializeField]
        private TurnTimerController _turnTimer;

        private IOthelloAI _ai;
        private IOnlineMoveRelay _relay;

        /// <summary>The match state and turn-flow API a board view listens to and reads from. Null until <see cref="StartNewMatch"/> is called.</summary>
        public OthelloGame Game { get; private set; }

        /// <summary>How the current (or most recently started) match is played.</summary>
        public GameMode Mode { get; private set; }

        /// <summary>Which side the human plays; the other side is played by the AI in <see cref="GameMode.VsAI"/> and <see cref="GameMode.OnlinePvP"/>.</summary>
        public Player HumanPlayer { get; private set; }

        /// <summary>The matched opponent for the current match, when <see cref="Mode"/> is <see cref="GameMode.OnlinePvP"/>; null otherwise.</summary>
        public OnlineOpponent CurrentOpponent { get; private set; }

        /// <summary>The AI difficulty backing the current match — either picked directly (<see cref="GameMode.VsAI"/>) or derived from the matched opponent's rating (<see cref="GameMode.OnlinePvP"/>).</summary>
        public Difficulty AiDifficulty { get; private set; }

        /// <summary>Display name for the <see cref="GameMode.VsAI"/> opponent persona; null in every other mode.</summary>
        public string OpponentDisplayName { get; private set; }

        /// <summary>Avatar id for the <see cref="GameMode.VsAI"/> opponent persona; null in every other mode.</summary>
        public string OpponentAvatarId { get; private set; }

        /// <summary>
        /// How the current match ended, once <see cref="Game"/>'s
        /// <c>IsGameOver</c> is true — <see cref="MatchEndReason.Natural"/>
        /// until then. Read by whoever computes the match currency reward
        /// (see <see cref="MatchRewardCalculator"/>) to tell a resignation
        /// or a timeout apart from a match that simply ran its course.
        /// </summary>
        public MatchEndReason EndReason { get; private set; } = MatchEndReason.Natural;

        /// <summary>Raised after <see cref="StartNewMatch"/> or <see cref="StartOnlineMatch"/> creates a new <see cref="Game"/>, so listeners can (re)bind to it.</summary>
        public event Action MatchStarted;

        private void Awake()
        {
            if (_turnTimer != null)
            {
                _turnTimer.TimeExpired += NotifyTurnTimedOut;
            }
        }

        /// <summary>
        /// Starts a fresh local or Vs-AI match, replacing whatever match was running before.
        /// </summary>
        /// <param name="mode">Whether both sides are human or one is AI-controlled. Use <see cref="StartOnlineMatch"/> for <see cref="GameMode.OnlinePvP"/> instead.</param>
        /// <param name="aiDifficulty">Difficulty for the AI side; ignored when <paramref name="mode"/> is <see cref="GameMode.LocalPvP"/>.</param>
        /// <param name="humanPlayer">Which side the human plays.</param>
        public void StartNewMatch(GameMode mode, Difficulty aiDifficulty, Player humanPlayer)
        {
            CurrentOpponent = null;
            BeginMatch(mode, aiDifficulty, humanPlayer);
        }

        /// <summary>
        /// Starts a fresh online match against <paramref name="opponent"/>.
        /// </summary>
        /// <param name="opponent">The opponent found by matchmaking.</param>
        /// <param name="humanPlayer">Which side the human plays.</param>
        /// <param name="relay">
        /// The real move relay to the matched opponent (Firestore, via
        /// <see cref="FirebaseOnlineMatchService"/>, once wired in). When
        /// null — today's default, since
        /// <see cref="LocalOnlineMatchService"/> has no real opponent to
        /// relay moves to or from — the opponent's moves are generated
        /// locally instead, by an AI picked to roughly match their rating
        /// (see <see cref="DifficultyForRating"/>).
        /// </param>
        public void StartOnlineMatch(OnlineOpponent opponent, Player humanPlayer, IOnlineMoveRelay relay = null)
        {
            CurrentOpponent = opponent ?? throw new ArgumentNullException(nameof(opponent));
            BeginMatch(GameMode.OnlinePvP, DifficultyForRating(opponent.Rating), humanPlayer, relay);
        }

        private void BeginMatch(GameMode mode, Difficulty aiDifficulty, Player humanPlayer, IOnlineMoveRelay relay = null)
        {
            if (_relay != null)
            {
                _relay.RemoteMoveReceived -= OnRemoteMoveReceived;
            }

            Mode = mode;
            HumanPlayer = humanPlayer;
            AiDifficulty = aiDifficulty;
            _relay = relay;

            // The AI only stands in for the opponent when there is no real
            // relay to receive their actual moves from.
            _ai = (mode != GameMode.LocalPvP && relay == null) ? AIFactory.Create(aiDifficulty) : null;

            if (mode == GameMode.VsAI)
            {
                var persona = AiPersonaPool[PersonaRng.Next(AiPersonaPool.Length)];
                OpponentDisplayName = persona.Name;
                OpponentAvatarId = persona.AvatarId;
            }
            else
            {
                OpponentDisplayName = null;
                OpponentAvatarId = null;
            }

            if (_relay != null)
            {
                _relay.RemoteMoveReceived += OnRemoteMoveReceived;
            }

            Game = new OthelloGame();
            Game.MoveApplied += OnMoveApplied;
            Game.GameEnded += OnGameEnded;
            EndReason = MatchEndReason.Natural;

            // No timer pressure against the AI — only real (or simulated)
            // opponents need one.
            _turnTimer?.SetEnabled(mode != GameMode.VsAI);
            _turnTimer?.ResetTimer();

            MatchStarted?.Invoke();
            PlayAiTurnIfNeeded();
        }

        /// <summary>Maps an opponent's Elo rating to the AI difficulty that stands in for them.</summary>
        private static Difficulty DifficultyForRating(int rating) => rating switch
        {
            < 1150 => Difficulty.Easy,
            < 1450 => Difficulty.Medium,
            _ => Difficulty.Hard
        };

        /// <summary>
        /// Call this from the board view when a human player taps a square.
        /// Returns false (and changes nothing) if the square is not
        /// currently a legal move, or no match is running yet.
        /// </summary>
        public bool TryPlayHumanMove(int square)
        {
            if (Game == null || Game.IsGameOver)
            {
                return false;
            }

            // In LocalPvP both sides are played by taps on the same device —
            // there is no single "human player" whose turn gates a tap, only
            // whichever color currently has the move. Every other mode keeps
            // the strict check: a tap while it's the AI's or the remote
            // opponent's turn is ignored.
            if (Mode != GameMode.LocalPvP && Game.State.CurrentPlayer != HumanPlayer)
            {
                return false;
            }

            bool played = Game.TryPlayMove(square);
            if (played)
            {
                _relay?.SendMove(square);
                PlayAiTurnIfNeeded();
            }
            return played;
        }

        private void PlayAiTurnIfNeeded()
        {
            // With a real relay, the opponent's turn is resolved by
            // OnRemoteMoveReceived instead — there is no AI to ask.
            if (Mode == GameMode.LocalPvP || Game.IsGameOver || Game.State.CurrentPlayer == HumanPlayer || _ai == null)
            {
                return;
            }

            int move = _ai.SelectMove(Game.State);
            Game.TryPlayMove(move);

            // If the AI plays both colors (e.g. the human passed involuntarily)
            // or faces another AI turn, keep going until it's the human's turn.
            PlayAiTurnIfNeeded();
        }

        /// <summary>
        /// Applies the remote opponent's move once it arrives over
        /// <see cref="_relay"/>. Ignored if it's not currently their turn —
        /// a stray or duplicate network event should never desync the local
        /// board from what the player can already see. The forfeit sentinel
        /// (see <see cref="IOnlineMoveRelay.SendMove"/>) is checked first, so
        /// a resignation or timeout is always honored regardless of whose
        /// turn it locally appears to be.
        /// </summary>
        private void OnRemoteMoveReceived(int square)
        {
            if (Game == null || Game.IsGameOver)
            {
                return;
            }

            if (square == ForfeitSentinel)
            {
                EndReason = MatchEndReason.Resignation;
                Game.ForceEnd(HumanPlayer);
                return;
            }

            if (Game.State.CurrentPlayer == HumanPlayer)
            {
                return;
            }

            Game.TryPlayMove(square);
        }

        private void OnMoveApplied(int square, Player mover)
        {
            Debug.Log($"{mover} jogou na casa {square}.");

            if (!Game.IsGameOver)
            {
                _turnTimer?.ResetTimer();
            }
        }

        private void OnGameEnded()
        {
            _turnTimer?.StopTimer();

            Player? winner = Game.GetWinner();
            string result = winner.HasValue ? $"{winner.Value} venceu." : "Empate.";
            Debug.Log($"Fim de jogo. Preto: {Game.State.DiscCount(Player.Black)}, " +
                      $"Branco: {Game.State.DiscCount(Player.White)}. {result}");
        }

        /// <summary>
        /// Concedes the match immediately, giving the other side the win.
        /// Available in every mode — conceding to the AI is as valid as
        /// conceding to a real opponent. In <see cref="GameMode.LocalPvP"/>,
        /// where there is no single "human player", whoever currently has
        /// the move is the one resigning.
        /// </summary>
        public void Resign()
        {
            if (Game == null || Game.IsGameOver)
            {
                return;
            }

            Player resigningPlayer = Mode == GameMode.LocalPvP ? Game.State.CurrentPlayer : HumanPlayer;
            Player winner = resigningPlayer == Player.Black ? Player.White : Player.Black;

            EndReason = MatchEndReason.Resignation;
            _relay?.SendMove(ForfeitSentinel);
            Game.ForceEnd(winner);
        }

        /// <summary>
        /// Called by <see cref="_turnTimer"/> when the current turn's time
        /// runs out — the side to move loses automatically. No-op in
        /// <see cref="GameMode.VsAI"/>, where the timer is disabled.
        /// <para>
        /// Both online peers run this same timer independently for whoever
        /// currently has the move, not gated to "only when it's my own
        /// turn" — so a disconnected or silent opponent's timeout is still
        /// detected locally as a win, without depending on their peer to
        /// report it. Only the side that actually timed out sends the
        /// explicit forfeit over the relay, for responsiveness; the other
        /// peer's own independent timer is the safety net if that message
        /// never arrives.
        /// </para>
        /// </summary>
        public void NotifyTurnTimedOut()
        {
            if (Game == null || Game.IsGameOver || Mode == GameMode.VsAI)
            {
                return;
            }

            Player timedOutPlayer = Game.State.CurrentPlayer;
            Player winner = timedOutPlayer == Player.Black ? Player.White : Player.Black;

            EndReason = MatchEndReason.Timeout;
            if (Mode == GameMode.OnlinePvP && timedOutPlayer == HumanPlayer)
            {
                _relay?.SendMove(ForfeitSentinel);
            }

            Game.ForceEnd(winner);
        }
    }
}

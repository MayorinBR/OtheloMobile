# Othello Mobile

A full Othello (Reversi) game built in Unity for mobile and PC, with a
bitboard-based engine, a three-tier AI, Firebase-backed accounts/cloud
save/ranking, and a dual-currency monetization layer (ads + IAP).

## Tech stack

- **Unity 6000.3.13f1 LTS**, C#, uGUI (Canvas/TextMeshPro) for all screens.
- **Firebase** — Authentication, Firestore (cloud save, leaderboard,
  online matchmaking/move relay).
- **Unity IAP** and **Google Mobile Ads (AdMob)** for monetization.
- Photon Fusion and LevelPlay packages are installed as placeholders for a
  future low-latency transport / ad mediation layer, but nothing in the
  game currently depends on them.

## Features

- **Engine & AI** (`Assets/Scripts/Core`, `Assets/Scripts/AI`) — bitboard
  move generation/validation, and three difficulty tiers: a greedy/random
  Easy AI, and a Minimax + alpha-beta Medium/Hard AI (depth 6+ on Hard)
  with a transposition table and positional weight tables.
- **Full screen flow** (`Assets/Scripts/Gameplay`) — Login, Main Menu, Play
  Mode picker, Game (HUD, turn timer, resign, match result, achievement
  toasts), Shop, Customization, Settings, Achievements, and Ranking, all
  navigated through a single `ScreenManager`/`UIScreen` pair so exactly one
  screen is ever active at a time.
- **Monetization** — a dual currency wallet (Soft currency earned by
  playing or watching rewarded ads, Hard currency bought via IAP),
  cosmetic piece/board skins and avatars, an interstitial ad shown after a
  configurable amount of active play time, and non-consumable/consumable
  IAP products (Remove Ads, gem packs, an exclusive board skin).
- **Accounts & cloud save** — Firebase Authentication (anonymous by
  default, linkable to a permanent provider) with player profile, wallet,
  and unlocked cosmetics saved to Firestore.
- **Online play & ranking** — Elo-style rating, rank tiers, and a
  leaderboard screen. The real Firebase-backed matchmaking/leaderboard
  services (`FirebaseOnlineMatchService`, `FirebaseLeaderboardService`)
  are implemented as drop-in replacements for the local mocks each
  controller defaults to today — see `OnlineMatchController`/
  `LeaderboardController` for how to switch.
- **Achievements & daily rewards** — a catalog-driven achievement list with
  unlock tracking, and a daily login reward screen.
- **Localization** — English (default), Portuguese, Japanese, Spanish,
  French, German, and Italian.

## Architecture

Every external dependency (ads, IAP, auth, cloud save, leaderboard, online
matchmaking) sits behind a small interface (`IAdsService`, `IIAPService`,
`IAuthService`, `ICloudSaveService`, `ILeaderboardService`,
`IOnlineMatchService`...) with a local, no-network mock implementation and
a real one backed by Firebase/AdMob/Unity IAP. Gameplay code only ever
talks to the interface, so the whole game — engine, AI, UI flow, shop,
achievements — can be played and tested end to end without any network
access, and swapping a mock for the real service is a one-line change in
that feature's controller.

## Project structure

- `Assets/Scripts/Core` — game rules: `Bitboard`, `BoardState`,
  `OthelloGame`, `Player`. No UI, no AI, no Unity dependency.
- `Assets/Scripts/AI` — `IOthelloAI` and the three difficulty
  implementations, plus the evaluators, transposition table, and Zobrist
  hashing the Medium/Hard AI use.
- `Assets/Scripts/Gameplay` — every `MonoBehaviour`: screens, the board
  view, monetization/ads/IAP controllers, Firebase service wrappers, and
  the procedural-UI helpers the Shop/Customization/Ranking/Achievements
  screens share.
- `Assets/Scripts/Services` — the plain-C# service interfaces, their local
  mocks, and game-data definitions (catalogs, pricing, Elo math) that
  don't depend on Unity.
- `Assets/Editor` — `OthelloUiBuilder`, an editor-only tool that builds and
  repairs the scene's UI hierarchy from code (menu: `Othello ▸ Build
  Screen UI`), so the whole UI layout can be regenerated instead of
  hand-edited in the Inspector.

## Getting started

1. Open the project in Unity Hub with **Unity 6000.3.13f1** (or newer 6000
   LTS).
2. Add your own `Assets/google-services.json` (from the Firebase Console)
   — it's git-ignored since it carries the Firebase project's IDs/keys.
3. For ads/AdMob testing, switch the active build target to **Android**
   in Build Settings — the `OTHELLO_ADMOB_ENABLED` scripting define symbol
   (which brings in the real `AdMobAdsService`) is only set for that
   platform. On any other platform, ads run through the local mock, which
   logs to the Console instead of showing a creative.
4. Press Play — `ScreenManager` opens on the Login screen, and every
   feature works against its local mock by default, no backend required.

## Verification

The core engine (`Core` + `AI`) was tested outside of Unity before being
wired into the game: move generation was checked against a naive
implementation across 300+ complete random matches (19,000+ assertions in
total), confirming the AI never picks an illegal move, that the
transposition table never changes the chosen move (it only speeds up the
search), and that the Hard AI consistently beats the Easy one.

## Next steps

- Wire `OnlineMatchController` and `LeaderboardController` to their real
  Firebase-backed services once the Firestore security rules for
  `matchmakingQueue`/`onlineMatches` are in place.
- Move Elo updates to a server-authoritative Cloud Function instead of the
  client, to prevent a modified client from reporting a fake result.
- Finish native Google/Apple sign-in wiring for linking an anonymous
  account to a permanent one.

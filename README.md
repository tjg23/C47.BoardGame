# Chung Toi (Unity, with Minimax AI)

A Unity 6 implementation of the abstract two-player game **Chung Toi**, featuring a configurable
board size (3x3 or 4x4) and a built-in opponent powered by **minimax search with alpha-beta
pruning**. The AI exists primarily as a showcase of the algorithm — node counts, alpha-beta
cutoffs, and search times are surfaced in the in-game HUD after every AI move.

The game logic is written as a pure C# library with no Unity dependencies, then rendered and
driven by Unity-side controllers. The project ships with a thorough EditMode test suite covering
rule semantics, win detection, and AI behavior.

## Features

- **Two board sizes:** classic 3x3 (3 pieces per player, 3-in-a-row to win) or extended 4x4
  (5 pieces per player, full 4-in-a-row required).
- **Five AI difficulty levels** — Beginner through Expert, mapped to minimax search depths 1–5.
- **Configurable seats:** any combination of human and AI on either side, including AI vs. AI.
- **Live search statistics** displayed during play (nodes explored, alpha-beta cutoffs, elapsed time).
- **Pure-C# rules engine** with full unit-test coverage; the AI runs on a worker thread so the
  editor stays responsive.

## How to play Chung Toi

Each player has 3 pieces on the 3x3 board (5 on 4x4). Each piece has a *facing*: **Cardinal**
(aligned with the board sides) or **Diagonal** (rotated 45°). Facing determines how the piece
can move in the second phase of the game.

### Phase 1 — Placement

Players alternate placing one piece per turn on an empty cell. When you place, you choose its
orientation. On the **3x3** board only, the very first move of the game cannot be the center
cell. The phase ends when both players have placed all their pieces.

### Phase 2 — Movement

On your turn, pick one of your pieces and either:

- **Rotate it in place** — flips its orientation between Cardinal and Diagonal. (Rotation must
  change the orientation; a no-op "rotate" is not a legal move.)
- **Slide it any number of empty squares in one direction** allowed by its current orientation:
  - Cardinal pieces slide N / S / E / W.
  - Diagonal pieces slide NE / NW / SE / SW.
- Slides can jump over other pieces but cannot land on an
  occupied cell — there are **no captures** in Chung Toi.
- A slide may optionally change the piece's orientation as it moves.

### Winning

Form a complete line of your color in any row, column, or main diagonal:

- **3x3:** any row, column, or either main diagonal — three of your pieces in a row.
- **4x4:** any full row, column, or either main diagonal — four of your pieces in a row.
  (Diagonals of length less than 4 do not count.)

Orientation does not affect winning — only ownership of the cells in a line. If a player on
their turn has no legal move, they lose.

### Controls

| Input | Effect |
| --- | --- |
| **Click** | Place a piece (placement) / select a piece / commit a move (movement) |
| **R** | Toggle orientation (placement) or preview-rotate the next slide (movement) |
| **Esc** | Deselect the current piece |
| **N** | New game with the current settings |
| **M** | Return to the main menu |

## Setup

The project requires **Unity 6** (developed against `6000.4.4f1`; any Unity 6.x release should work).

### Option A — Open the source project in Unity

1. Install **[Unity Hub](https://unity.com/download)**.
2. Through Unity Hub, install the latest Unity 6 LTS editor (or `6000.4.4f1` if available).
3. Get the project:
   - **From a Git clone:** `git clone <repo url>` and `cd` into the resulting folder.
   - **From a ZIP archive:** unzip and note the path of the resulting folder.
4. In Unity Hub click **Add → Add project from disk**, then select the project folder.
5. Click the project entry. The first open takes 1–3 minutes: Unity restores packages from
   `Packages/manifest.json`, generates the `Library/` cache, compiles all assemblies, and
   imports assets.
6. In the **Project** window, open `Assets/Scenes/BoardView.unity` (double-click).
7. Press **Play** at the top of the editor. The main menu appears; configure board size,
   players, and difficulty, then click **Start game**.

> The first run requires an internet connection so Unity can resolve packages. Subsequent
> runs are offline.

### Option B — Run a standalone build

If a `.app` (macOS) or `.exe` (Windows) build is provided, just double-click it. No Unity
installation needed. On macOS, the first launch may require right-click → **Open** to bypass
Gatekeeper, since the build is not code-signed.

### Building it yourself

From an open Unity editor: **File → Build Settings → Add Open Scenes → Build**. The output
is a self-contained executable.

## Project structure

```
Assets/Scripts/
├── Core/             Pure C# game logic (no Unity references)
│   ├── Board, Cell, Coord, Move, Player, Rules, GameState, ...
├── Core.Tests/       EditMode unit tests for the rules engine
├── AI/               Minimax + alpha-beta search, line heuristic
├── AI.Tests/         EditMode tests for AI behavior + matchup
├── View/             Unity rendering: BoardView, HighlightOverlay, CellRef
└── Game/             Top-level controllers + UI
    ├── GameController     Game loop, owns GameState and IPlayer slots
    ├── HumanPlayer        Mouse + keyboard player
    ├── AIPlayer           Wraps MinimaxAI, runs on a worker thread
    ├── InputController    Raw mouse → semantic events
    ├── HudOverlay         In-game turn / orientation / stats display
    └── MainMenuController Pre-game settings menu
```

The `Core` and `AI` assemblies have **no Unity dependencies** — they're plain .NET 8 / C# 9
code and could be lifted into another project (or unit-tested outside Unity) without changes.

## Running the tests

In the Unity editor: **Window → General → Test Runner**, choose the **EditMode** tab, and click
**Run All**. The suite covers placement / movement rule generation, win detection on both board
sizes, AI tactical correctness (forced wins, forced blocks), and a small AI-vs-random matchup.

From the command line (macOS, adjust the path for other platforms):

```bash
/Applications/Unity/Hub/Editor/6000.4.4f1/Unity.app/Contents/MacOS/Unity \
  -batchmode -nographics \
  -projectPath . \
  -runTests -testPlatform EditMode \
  -testResults test-results.xml
```

## Implementation notes

- **Minimax with alpha-beta pruning** is implemented in explicit max/min form (rather than
  negamax) for readability. The `MinimaxAI.UseAlphaBeta` field can be flipped at runtime to
  compare node counts with pruning on vs. off — useful for the showcase angle.
- **Search stats** (`SearchStats`) include nodes visited, leaf evaluations, terminal nodes,
  alpha-beta cutoffs, and elapsed time. They appear in the bottom-left of the HUD after every
  AI move and are also logged to the Console.
- **Heuristic evaluator** (`LineHeuristicEvaluator`) scores `k²` per uncontested line of length
  `k` for the side being evaluated, summed across rows, columns, and main diagonals. The
  `IEvaluator` interface lets you swap in smarter heuristics without touching the search.
- **Threading:** the AI search runs on a thread-pool thread via `Task.Run`. The Unity main
  thread awaits its result, so `Rules.Apply` and rendering always happen on the main thread.


<h1 align="center">Project Two: Red vs Blue</h1>

<p align="center"><b>Draft your squad. Defeat the Superior.</b><br>
A turn-based tactics duel on an isometric board, for two players on one screen or against the computer.</p>

<p align="center">
  <img src="https://img.shields.io/badge/Unity-6-D62D64?style=for-the-badge&labelColor=2B2B2B" alt="Unity 6">
  <img src="https://img.shields.io/badge/Windows-x64-F2F2F2?style=for-the-badge&labelColor=2B2B2B" alt="Windows x64">
  <img src="https://img.shields.io/badge/version-2.0.0-D62D64?style=for-the-badge&labelColor=2B2B2B" alt="Version 2.0.0">
  <img src="https://img.shields.io/badge/language-EN_%2F_PL-F2F2F2?style=for-the-badge&labelColor=2B2B2B" alt="English and Polish">
</p>

<p align="center">
  <a href="README.pl.md">Polski</a> · <b>English</b> ·
  <a href="#quick-start">Quick start</a> · <a href="#screenshots">Screenshots</a> ·
  <a href="#what-changed-since-the-initial-release">What changed</a> · <a href="#goals">Goals</a> ·
  <a href="#running-from-source">Running from source</a> · <a href="#credits">Credits</a>
</p>

An updated and improved version of the project initially created by Team 13 during Game Dev School in 2021.
All updates since the initial release are by Daniel Pytel.

A turn-based tactics duel on an isometric board. Team Red, led by the Superior Red Boss, faces
Team Blue, led by Blue Boss. Before the battle the players draft their Doppelgangers in pairs.
During the battle they call units in next to their Superior, move them and attack. Board tiles
heal, damage, give cover, slow units down or extend their range. The first player to defeat the
opposing Superior wins, and every finished game is scored and saved.

Two players can play on one screen, or one player can face the computer on three difficulty levels.

## Quick start

1. Download **[v2.0.0.Project.Two.Redone.zip](https://github.com/danielmaciejpytel/project-two/releases/download/v2.0.0/v2.0.0.Project.Two.Redone.zip)** from the [v2.0.0 release](https://github.com/danielmaciejpytel/project-two/releases/tag/v2.0.0).
2. Extract the entire archive to a folder; do not run the game from inside the ZIP.
3. Launch **`PROJECT TWO.exe`**.

The download is a standalone **Windows 64-bit** build; Unity is not required to play. The unchanged 2021 course
submission is kept as the separate [v1.0.0 release](https://github.com/danielmaciejpytel/project-two/releases/tag/v1.0.0).
To open the project in the editor, see [Running from source](#running-from-source).

## Screenshots

| Menu | Credits |
| --- | --- |
| ![Main menu](Docs/Screenshots/menu.png) | ![Credits](Docs/Screenshots/credits.png) |

![Unit choice](Docs/Screenshots/unit-choice.png)

![Gameplay](Docs/Screenshots/gameplay.png)

![Winner panel](Docs/Screenshots/winner.png)

## What changed since the initial release

The initial release was the 2021 Game Dev School prototype for two players on one screen, built in Unity 2019.4
by the whole Team 13. Since then Daniel Pytel has added:

- **Unity 6.** The project runs on Unity 6000.3.28f1. Input is handled only by the Input System
  package, and mouse and touch are both supported. The unused GitHub for Unity plugin is gone.
- **Bug fixes.**
  - Ending a turn during deployment no longer throws errors.
  - Units that die from losing bonus health are really removed.
  - Provoked units respect the provoking unit.
  - The music no longer duplicates and the turn timer stops when the game ends.
  - Clicks on HUD buttons no longer select tiles underneath.
  - A unit killed while its Superior acted no longer blocks the turn.
  - A unit killed by an attack dies only after the attacker's attack animation, and its death
    starts right away without a jump from the idle animation.
  - A unit that has already moved no longer shows its move range again, but still shows where
    it can attack.
- **Cleaner code.** The board computes move range and paths with a single breadth-first search.
  The scripts follow common C# conventions, and animations are safely linked to their objects.
  Files and folders in `Assets` use PascalCase names, and unused assets were removed.
- **New interface.** The UI scales with the screen resolution and uses TextMeshPro with a sharp
  pixel font at fixed sizes. The HUD has a team-coloured turn label and a collapsible battle log
  on the active player's side, which scrolls with the mouse wheel and jumps to new moves. The menu
  has new instructions and slowly floating artwork, and the info panel shows maximum health
  including bonuses.
- **Pause and end screen.** Esc opens a pause menu over a darkened, frozen board with Play again,
  Back to Menu, Settings and Quit. Play again, Back to Menu and Quit ask "Are you sure?" first.
  When a game ends the same screen shows the winner and a summary of turns played, units called,
  units killed and the score of both teams. Against the computer you can start a rematch with the
  sides swapped.
- **Hints and shortcuts.** Hints about the possible actions of a unit appear only for the team
  that is playing, with extra tips in the first turn. Space ends the turn and C calls a unit.
  The turn timer turns red and ticks in the last 10 seconds.
- **Scores and records.** Every finished game is scored: points for the win, kills, a quick
  victory and the health the winner's Superior has left, minus lost units, scaled by the AI
  difficulty. The end screen shows the score and a new high score or the place on the
  leaderboard. The new **RECORDS** screen in the menu has a leaderboard (top 10, filtered by
  difficulty), the history of the last 10 games with details and the battle log, and statistics.
  The latest 200 games are saved in a JSON file.
- **New unit choice screen.** The draft has a new layout with a panel for each team, unit stats,
  tag icons, a move and attack range grid and pulsing arrows. It also works against the computer.
  Menu, draft and battle share the same animated background, which can be darkened with optional
  dim and shade layers.
- **Options.** Sound and music volume sliders, AI difficulty, screen resolution (native by
  default) and language (English or Polish, with a full Polish translation). Everything is saved
  automatically, and a button in the menu resets them. Sound, music, difficulty and resolution can
  also be changed during a game from the pause menu.
- **Faster start.** Unit animations are loaded only for the units picked in the draft.
- **Automatic tests.** The `Tests` folder has EditMode and PlayMode suites for the rules, the AI,
  the scoring and the records, the menus and the end screen, so a change can be checked without
  playing every game by hand. See [Tests/TestsUpdate.md](Tests/TestsUpdate.md) (in Polish) for
  how to run them and how to update them when the game changes.
- **Computer opponent.** The new PLAY VS AI mode (under PLAY) lets you lead Red Boss against a computer-led
  Blue Boss. The computer drafts its units, calls reinforcements, moves, attacks and uses
  Confuse and Teleport. Three difficulty levels can be chosen in Options:
  - **Easy** makes clumsy, partly random moves.
  - **Normal** plays a solid game.
  - **Hard** avoids danger, focuses wounded units and guards its Superior.

  The computer also recognises a stalemate and pushes forward.
- **Balance.**
  - The starting player is random.
  - Confuse can be used every other turn, and Teleport moves an ally up to 2 tiles.
  - Repeater and Tormentor have 7 health, and Operative deals 2 damage.
  - The Superiors are unchanged, so they remain the strongest units on the board.

## Goals

- **Short, readable duels.** A match should take a few minutes, and every rule should be visible
  on the board or in the info panel.
- **Meaningful choices.** The draft, unit tags, tile effects and abilities should give each
  match a different plan instead of one dominant strategy.
- **Fair sides.** Team Red and Team Blue should win equally often. The board is symmetric, the
  first move is random and paired units are kept close in strength.
- **Single-player worth playing.** Each AI level should match a different kind of player, from a
  first game to a real challenge.
- **A project that is easy to keep working on.** The game uses a current engine, clear code and
  no leftover plugins.

## Running from source

1. Open the project in Unity 6000.3.28f1.
2. Open `Assets/Scenes/MenuScene.unity` and press Play.
3. Choose **PLAY**, then **PLAYER VS PLAYER** for two players on one screen or **PLAY VS AI**
   to play against the computer. The AI difficulty can be changed in **Options**, and finished
   games are listed in **Records**.

## Credits

Initial release (Team 13, Game Dev School 2021):

- Code: Karol Ławicki
- Art: Daniel Pytel
- Design: Matt Matuszewski, Mateusz Niziołek

Updated version: Daniel Pytel

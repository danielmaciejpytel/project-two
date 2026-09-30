# Project Two: Super Hot vs Super Cold

**English** | [Polski](README.pl.md)

An updated and improved version of the project initially created by Team 13 during Game Dev School in 2021.
All updates since the initial release are by Daniel Pytel.

A turn-based tactics duel on an isometric board. Two Superiors, Super Hot and Super Cold, each
lead a team of Doppelgangers. Before the battle the players draft their units in pairs. During the
battle they call units in next to their Superior, move them and attack. Board tiles heal, damage,
give cover, slow units down or extend their range. The first player to defeat the opposing
Superior wins.

Two players can play on one screen, or one player can face the computer.

## Screenshots

| Menu | Credits |
| --- | --- |
| ![Main menu](Docs/Screenshots/menu.png) | ![Credits](Docs/Screenshots/credits.png) |

![Unit choice](Docs/Screenshots/unit-choice.png)

![Gameplay](Docs/Screenshots/gameplay.png)

## What changed since the initial release

The initial release was the 2021 Game Dev School prototype for two players on one screen, built in Unity 2019.4
by the whole Team 13. Since then Daniel Pytel has added:

- **Unity 6.** The project runs on Unity 6000.3.23f1. Input is handled only by the Input System
  package, and mouse and touch are both supported. The unused GitHub for Unity plugin is gone.
- **Bug fixes.**
  - Ending a turn during deployment no longer throws errors.
  - Units that die from losing bonus health are really removed.
  - Provoked units respect the provoking unit.
  - The music no longer duplicates and the turn timer stops when the game ends.
  - Clicks on HUD buttons no longer select tiles underneath.
  - A unit killed while its Superior acted no longer blocks the turn.
- **Cleaner code.** The board computes move range and paths with a single breadth-first search.
  The scripts follow common C# conventions, and animations are safely linked to their objects.
  Files and folders in `Assets` use PascalCase names, and unused assets were removed.
- **New interface.** The UI scales with the screen resolution and uses TextMeshPro with a sharp
  pixel font at fixed sizes. The HUD has a team-coloured turn label and a collapsible battle log
  on the active player's side. The menu has new instructions and slowly floating artwork, and the
  info panel shows maximum health including bonuses.
- **New unit choice screen.** The draft has a new layout with a panel for each team, unit stats,
  tag icons, a move and attack range grid and pulsing arrows. It also works against the computer.
  Menu, draft and battle share the same animated background, which can be darkened with optional
  dim and shade layers.
- **Options.** Sound and music volume sliders, AI difficulty, screen resolution (native by
  default) and language (English or Polish, with a full Polish translation). Everything is saved
  automatically.
- **Faster start.** Unit animations are loaded only for the units picked in the draft.
- **Computer opponent.** The new PLAY VS AI mode (under PLAY) lets you lead Super Hot against a computer-led
  Super Cold. The computer drafts its units, calls reinforcements, moves, attacks and uses
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
- **Fair sides.** Super Hot and Super Cold should win equally often. The board is symmetric, the
  first move is random and paired units are kept close in strength.
- **Single-player worth playing.** Each AI level should match a different kind of player, from a
  first game to a real challenge.
- **A project that is easy to keep working on.** The game uses a current engine, clear code and
  no leftover plugins.

## Running the game

1. Open the project in Unity 6000.3.23f1.
2. Open `Assets/Scenes/MenuScene.unity` and press Play.
3. Choose **PLAY**, then **PLAYER VS PLAYER** for two players on one screen or **PLAY VS AI**
   to play against the computer. The AI difficulty can be changed in **Options**.

## Credits

Initial release (Team 13, Game Dev School 2021):

- Code: Karol Ławicki
- Art: Daniel Pytel
- Design: Matt Matuszewski, Mateusz Niziołek

Updated version: Daniel Pytel

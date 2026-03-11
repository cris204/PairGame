# PairGame Prototype

A gameplay-focused memory card match prototype built in Unity 2021 LTS.

The project was developed with emphasis on clean architecture, responsive interactions, and maintainable code rather than final art production.

## Features

- Variable board layouts, including odd layouts such as `3x3`
- Smooth card flip animations
- Continuous input flow while match resolution is processed
- Score system with combo support
- JSON save/load flow
- Start menu with `New Game` / `Load Game`
- HUD with score, combo, and game-over state
- Basic SFX hooks for flip, match, mismatch, and game over
- Simple UI and card feedback VFX

## Tech

- Unity `2021 LTS`
- C#
- JSON-based persistence

## How To Run

1. Open the project in Unity 2021 LTS.
2. Open the main gameplay scene.
3. Press Play in the Editor.

## Board Configuration

The default board layout can be edited from the `GameConfig` asset:

- `Assets/GameConfig.asset`

To change the starting board size, update:

- `Default Rows`
- `Default Columns`

Other gameplay values such as spacing, scoring, combo behavior, autosave, audio volume, and initial reveal timing are also configured from the same asset.

## Gameplay Notes

- `New Game` starts a fresh board using the current configured layout.
- `Load Game` is available only when a compatible save exists.
- On a new game, cards are briefly revealed before gameplay starts.
- Completed matches disappear from the board.
- If a saved run is already completed, the game starts a fresh session instead of restoring a finished board.

## Project Structure

- `Assets/Scripts/CardMatch/Core`
  Core gameplay flow, matching, scoring, and save/load logic
- `Assets/Scripts/CardMatch/Presentation`
  Board creation, card visuals, HUD, audio, and UI flow
- `Assets/Scripts/CardMatch/Config`
  ScriptableObject-driven game configuration

## Main Systems

- `GameManager`
- `BoardManager`
- `CardSelectionController`
- `MatchResolver`
- `ScoreManager`
- `SaveLoadService`
- `HUDView`
- `AudioManager`

## Current Goal

This repository is intended as a prototype focused on gameplay feel, system clarity, and code quality.


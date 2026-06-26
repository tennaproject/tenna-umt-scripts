# Tenna UndertaleModTool Scripts

A collection of [UndertaleModTool](https://github.com/UnderminersTeam/UndertaleModTool) scripts for Deltarune created by Tenna Project.

## Installation

1. Copy the contents of `src/` into UndertaleModTool's `Scripts` folder.
2. Open `data.win` in UndertaleModTool.
3. Run `Scripts/TennaProject/GameAll.csx`.
4. Choose the runtime tools to install. `GameCore.csx` is required and is always installed first.
5. Save `data.win`, then launch the game.

Run `GameAll.csx` again after script updates. The installers refresh their helper scripts and clean their own injected event blocks without adding duplicates. Individual `Game*.csx` scripts can still be run manually.

## Runtime Scripts

### GameCore.csx
Base script required by the other runtime scripts. Shows version and elapsed time in the bottom-right corner and writes a log file to `tenna/logs/tenna-YYYY-MM-DD_HH-MM-SS.txt`.
Creates `tenna/config.json` on first run. The config keeps overlay visibility persistent across sessions and provides shared runtime settings used by the other tools.

```
Tenna Core v0.0.098 2026-02-04_21-24-25

[0:15] [FlagWatcher] [42]: 0 -> 1
[0:23] [PlotWatcher] 10 -> 15
```

**Hotkey:** Alt+1 to toggle display

### GameFlagWatcher.csx
![flagwatcher](/assets/flagwatcher.png)

Hooks all flag writes at the code level using compile-time GML injection. Displays changes in the top-right corner and records flag deltas with room, plot, and chapter context.
Bitmask writes are shown as decoded packed fields, for example `Flag[1843:1w4]: 0 -> 2`, while the JSONL row still includes the parent flag change. Array-packed writes show the changed slot when only one packed value changes, or an `arrayw<N>` summary when multiple slots change.
Reinstall also upgrades known watcher-generated flag wrappers, including `scr_set_bitmask_value`, `scr_flag_set_ext`, and `scr_array_to_bitmask` forms. 

**Hotkey:** Alt+2 to toggle display
**Ignore last flag:** Alt+I toggles the most recently displayed flag between watched and ignored.
**Ignores:** defaults to flags 6, 20, 21, 33. Edit `flagWatcher.ignoredFlags` in `tenna/config.json` to add or remove ignored flags.

### GamePlotWatcher.csx
![plotwatcher](/assets/plotwatcher.png)
Shows current plot value and notification when it changes.

**Hotkey:** Alt+3 to toggle display

### GameFlagEditor.csx
![flageditor](/assets/flageditor.png)

In-game editor for setting `global.flag[id]`.

**Hotkey:** Alt+4 to open editor

### GameStateDump.csx
Dumps save-relevant globals for the current state to `tenna/state/state-YYYY-MM-DD_HH-MM-SS.json`.

**Hotkey:** Alt+5 to dump state

### GameNotes.csx
Opens a note prompt and writes the message to the Tenna Core log.

**Hotkey:** Alt+N to open note prompt

### GameSaveManager.csx
![savemanager](/assets/savemanager.png)
In-game save manager with chapter-separated save slots stored in `tenna/saves/chapter<N>/`. Uses the vanilla save format.

**Hotkey:** Alt+S to open menu  
**Controls:** Up/Down move, Shift+Up/Down or PageUp/PageDown page, Home/End jump, Z/Enter select, X/Esc cancel, Left/Right switch Load/Delete

## Data Exporters

Run `DataAll.csx` to pick export categories and choose the output folder once. Run an individual `DataExport*.csx` script when only one JSON file is needed.

| Script | Output |
| --- | --- |
| `DataExportConsumables.csx` | `consumables.json` |
| `DataExportWeapons.csx` | `weapons.json` |
| `DataExportArmors.csx` | `armors.json` |
| `DataExportKeyItems.csx` | `key-items.json` |
| `DataExportLightWorldItems.csx` | `light-world-items.json` |
| `DataExportRooms.csx` | `rooms.json` |
| `DataExportSpells.csx` | `spells.json` |
| `DataExportEnemies.csx` | `enemies.json` |
| `DataExportCharacters.csx` | `characters.json` |

## License

This project is licensed under the MIT License. See the LICENSE file for details.

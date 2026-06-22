# Tenna UndertaleModTool Scripts

A collection of [UndertaleModTool](https://github.com/UnderminersTeam/UndertaleModTool) scripts for Deltarune created by Tenna Project.

## Installation

1. Copy the contents of `src/` into UndertaleModTool's `Scripts` folder.
2. Open `data.win` in UndertaleModTool.
3. Run `Scripts/TennaProject/GameAll.csx`.
4. Choose the runtime tools to install. `GameCore.csx` is required and is always installed first.
5. Save `data.win`, then launch the game.

Run `GameAll.csx` again after script updates. The installers refresh their helper scripts without adding duplicate event hooks. Individual `Game*.csx` scripts can still be run manually.

## Runtime Scripts

### GameCore.csx
Base script required by the other runtime scripts. Shows version and elapsed time in the bottom-right corner and writes a log file to `tenna/logs/tenna-YYYY-MM-DD_HH-MM-SS.txt`.

```
Tenna Core v0.0.098 2026-02-04_21-24-25

[0:15] [FlagWatcher] [42]: 0 -> 1
[0:23] [PlotWatcher] 10 -> 15
```

**Hotkey:** Alt+1 to toggle display

### GameFlagWatcher.csx
![flagwatcher](/assets/flagwatcher.png)

Displays flag changes in the top-right corner and records flag deltas with room, plot, and chapter context.

**Hotkey:** Alt+2 to toggle display  
**Ignores:** flags 21, 33

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

### GameSaveManager.csx
![savemanager](/assets/savemanager.png)
In-game save manager with chapter-separated save slots stored in `tenna/saves/chapter<N>/`. Uses the vanilla save format.

**Hotkey:** Alt+S to open menu  
**Controls:** Arrows navigate, Z/Enter select, X/Esc cancel, Left/Right switch Load/Delete

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

## License

This project is licensed under the MIT License. See the LICENSE file for details.

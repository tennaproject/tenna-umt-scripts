# Tenna UndertaleModTool Scripts

A collection of [UndertaleModTool](https://github.com/UnderminersTeam/UndertaleModTool) scripts for Deltarune created by Tenna Project.

## Scripts

### Core.csx
Base script required by others scripts to work. Shows version and elapsed time in bottom-right corner. Creates a log file (`tenna/logs/tenna-YYYY-MM-DD_HH-MM-SS.txt`) for tracking changes.

```
Tenna Core v0.0.098 2026-02-04_21-24-25

[0:15] [FlagWatcher] [42]: 0 -> 1
[0:23] [PlotWatcher] 10 -> 15
```

**Hotkey:** Alt+1 to toggle display

### FlagWatcher.csx
![flagwatcher](/assets/flagwatcher.png)

Displays flag changes in the top-right corner.

**Hotkey:** Alt+2 to toggle display  
**Ignores:** flags 21, 33

### PlotWatcher.csx
![plotwatcher](/assets/plotwatcher.png)
Shows current plot value and notification when it changes.

**Hotkey:** Alt+3 to toggle display

### SaveManager.csx
![savemanager](/assets/savemanager.png)
In-game save manager with unlimited save slots stored in `tenna/saves/`. Uses vanilla save format.

**Hotkey:** Alt+S to open menu  
**Controls:** Arrows navigate, Z/Enter select, X/Esc cancel, Left/Right switch Load/Delete

## Installation

1. Copy scripts to UndertaleModTool's `Scripts` folder
2. Open your `data.win` in UMT
3. Run `Core.csx` first
4. Run any other scripts you want
5. Save and launch game

## License

This project is licensed under the MIT License. See the LICENSE file for details.

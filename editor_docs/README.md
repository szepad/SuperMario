# Level Editor & Level Data: Developer Documentation

This folder explains how levels are stored, loaded and edited: the level data layer, the `.lvl` file format, the level editor, its Gum-based UI, and how playtesting hooks into the game. Read it in order the first time. Every chapter builds on the previous ones.

| # | Chapter | What you'll learn |
|---|---------|-------------------|
| 1 | [Architecture](01-architecture.md) | The layers, which way dependencies point, scenes, and one frame of the editor end to end |
| 2 | [Level data](02-level-data.md) | `TileSpec`, autotiles, the entity catalog and property schema, `LevelData`, building a `World` |
| 3 | [The `.lvl` format](03-level-format.md) | The v2 format byte by byte, the reader/writer, the legacy parser and migration |
| 4 | [Editor core](04-editor-core.md) | `LevelEditor`, the document and undo/redo, selection, the preview world, camera and viewport |
| 5 | [Tools, brushes & the palette](05-tools-brushes-palette.md) | Every tool's behaviour, brushes, stamps, entity placement and snapping |
| 6 | [The UI (Gum)](06-ui-gum.md) | A Gum primer, the layout tree, each view, how inspector fields bind to data, and Gum workarounds |
| 7 | [Input & shortcuts](07-input-and-shortcuts.md) | Who gets the mouse and keyboard and when, focus rules, the Escape cascade |
| 8 | [Playtesting](08-playtesting.md) | How "play what's on screen" works, `GameScene` changes, sublevels and pipe intros |
| 9 | [Recipes](09-recipes.md) | Step-by-step: add an entity, a property, a tile kind, an autotile, a stamp, a tool, a menu item |
| 10 | [Gotchas & limitations](10-gotchas.md) | Non-obvious behaviour, known limitations, things to watch when changing code |

## Where the code lives

```
Source/
├── Core/                  Engine-level code (game loop, assets, input, settings)
│   └── CommandLine.cs     --migrate-levels
├── Game/
│   ├── Level/             NEW: pure level data + file format + building worlds
│   └── ...                Gameplay (World, entities, tiles, cutscenes...)
├── Editor/                NEW: the level editor (namespace Mario2.Editor)
│   ├── Brushes/           What you paint with: tile, entity and stamp brushes
│   ├── Input/             Scroll/pinch input and the macOS trackpad hook
│   ├── Palette/           The catalog of palette items and their icons
│   ├── Tools/             Select, Brush, Rectangle, Erase, Pick
│   └── UI/                Gum views: menu, toolbar, palette, inspector, status bar, dialogs
└── Scenes/
    └── EditorScene.cs     Thin scene that hosts the LevelEditor
```

## Units used throughout

Four coordinate systems show up everywhere. Mixing them up is the most common source of bugs.

| Name | Unit | Example | Used by |
|------|------|---------|---------|
| **Screen** | window pixels | mouse position, Gum layout | UI, `EditorCamera.Viewport` |
| **World** | scaled pixels (`Game.TextureScale` = 4 per texture pixel) | `Entity.Position`, `Tile.Size` = 64 | game runtime, drawing |
| **Texture pixels** ("px") | 1 = one pixel of the 16×16 art | `EntityPlacement.X/Y`, `LevelMeta.SpawnX/Y` (both are feet: bottom-centre) | level files, `LevelData` |
| **Tiles** | 1 = 16 px = 64 world units | `col`, `row`, `TileRect` | tile grid |

Conversions: `world = px * 4`, `px = col * 16`, `col = floor(world / Tile.Size)`.

# 9. Recipes

Step-by-step guides for the common extensions. Each lists every file you need to touch.

## 9.1 Add a new placeable entity

Example: a Hammer Bro with a "jump height" setting.

1. **Write the entity** as usual (`Source/Game/Entities/...`). It needs a public `(World world)` constructor and settable members for anything the editor should configure.
2. **Register it** in `EntityCatalog`'s static constructor (`Source/Game/Level/EntityCatalog.cs`):
   ```csharp
   Add("hammer_bro", "Hammer Bro", EntityCategory.Enemy, w => new HammerBro(w))
       .Float("jump_height", "Jump Height (tiles)", 4f, (e, v) => e.JumpHeight = v);
   ```
   The key (`hammer_bro`) is what files store. Choose it carefully; changing it later breaks levels.
3. **Add it to the palette** (`Source/Editor/Palette/PaletteCatalog.cs`, in the static constructor):
   ```csharp
   Entity(PaletteCategory.Enemies, "Hammer Bro", Icon.Frame("hammer_bro", height: 24), "hammer_bro");
   ```
   Any texture region works as an icon. For curated icons, add a cell to `res/entities/entity_selector.png` and use `Icon.Entity(col, row)`.
4. Done. Saving, loading, the inspector (a "Jump Height" text field), ghost previews, hit-testing and playtesting all work automatically.

Optional extras:
- If the entity is **invisible in game** (a trigger or marker), override `DrawEditor()` to draw an icon. It's only called in the editor's preview world.
- If **only one may exist per level**, add `.Unique()`.
- If it **spawns helper entities** when created (like `Firebar`), nothing extra is needed. `EditorPreview` tracks and removes them with their parent.

## 9.2 Add a property to an existing entity

1. Add one builder call to its `EntityCatalog` entry (`.Bool`, `.Float`, `.String`, `.Path` or `.Enum<T>`), with a default and a setter.
2. That's it. Existing files don't have the key and get the default. The inspector shows the new field. Files saved from now on contain it.

To grey the field out depending on other properties, pass `isEnabled: props => ...` (see `warp_pipe.dest`).

If the entity reads the property in `OnSpawn`, that works too. Properties are applied before spawning (chapter 2.4).

## 9.3 Add a palette preset

A variant that's just different property values (like *Goomba (Underground)*):

```csharp
Entity(PaletteCategory.Enemies, "Goomba (Castle)", Icon.Entity(3, 0), "goomba", ("palette", nameof(Palette.Castle)));
```

For blocks, use `Tile(...)` with a preset spec, e.g. `new QuestionSpec(ItemType.Star)` and a badge `Icon.Item(ItemType.Star)`.

## 9.4 Add a new tile kind

Example: a note block with a bounce strength.

1. **Runtime tile:** a `Tile` subclass, as for gameplay.
2. **Spec** (`TileSpec.cs`):
   ```csharp
   public sealed record NoteSpec(int Strength = 1) : TileSpec
   {
       public override bool IsSolid => true;
       public override Tile CreateTile() => new NoteTile { Strength = Strength };
   }
   ```
3. **File format** (`LevelFormat.cs`):
   - in `WriteSpec`: `NoteSpec n => $"note strength={n.Strength}"`;
   - in `ReadSpec`: `case "note": return new NoteSpec(ArgInt(args, "strength", 1, lineNo));` (add an `ArgInt` helper next to `ArgBool`).
4. **Name** (`Source/Editor/TileNames.cs`): `NoteSpec n => $"Note Block ({n.Strength})"`.
5. **Palette:** `Tile(PaletteCategory.Blocks, "Note Block", Icon.Tile(...), new NoteSpec());`.
6. **Inspector** (optional, for editable fields): add a `case NoteSpec:` to `InspectorView.BuildTile` (placed tiles) and `BuildTileBrush` (the brush), following the `QuestionSpec` cases.
7. **Viewport overlay** (optional): `EditorViewport.DrawTileOverlays` if it needs an editor-only hint.

## 9.5 Add an autotile group

Example: a snowy ground.

1. Draw the pieces into the tileset: a top strip (left end, middle, right end, single) and optionally a body strip.
2. Declare the group in `AutoTiles.cs` and add it to `All`:
   ```csharp
   public static readonly AutoTileGroup Snow = new("snow", "Snow Ground", true,
       AutoTileStrip.Ids(128, 129, 130, 131, true),
       AutoTileStrip.Ids(136, 137, 138, 139, true));
   ```
3. Palette: `Auto("Snow Ground", 131, AutoTiles.Snow);`.

The file format, painting, ghost previews and building all handle it automatically (`def x auto snow`). Pass `connectToSolid: true` for decoration that should end neatly against solid tiles, like bushes.

If the look needs more than "left/right neighbours + is something above me" (e.g. corner pieces depending on diagonals), extend `AutoTileGroup.Resolve` and `AutoTiles.ResolveFor`. Everything else goes through those two functions.

## 9.6 Add a built-in stamp (structure)

1. Define it in `Stamps.cs`:
   ```csharp
   public static readonly Stamp Tree = new("Tree",
   [
       new StampCell(0, 0, AutoTiles.Tree.Spec, OnlyOnEmpty: true),
       new StampCell(0, 1, AutoTiles.TreeTrunk.Spec, OnlyOnEmpty: true, FillDown: true),
   ]);
   ```
   Entities go in the optional second list (offsets in texture pixels from the stamp's top-left).
2. Palette: `Stamp("Tree", Icon.Tile(42), Stamps.Tree);`.

## 9.7 Add a level setting (level metadata)

Example: a per-level "no piranha plants" flag.

1. Add a parameter to `LevelMeta` (`LevelData.cs`). It's a positional record, so the compiler will point at the two constructor calls to update: `LevelMeta.Default` and `LegacyLevelParser.Parse`.
2. **File format** (`LevelFormat.cs`): write a record in `Write` (e.g. `piranha_plants false`), and read it in `Read`'s `switch` (`case "piranha_plants": ...`). Old files without the record keep the default.
3. **Game:** apply it in `LevelBuilder.Build` (e.g. a new `World` property) and use it in gameplay.
4. **Inspector:** add a row in `InspectorView.BuildLevel`, e.g.
   `b.Check("Piranha Plants", () => Level().Meta.PiranhaPlants, v => _editor.SetLevelMeta(m => m with { PiranhaPlants = v }, "Toggle Piranha Plants"));`

## 9.8 Add a tool

1. Subclass `Tool` in `Source/Editor/Tools/`. Implement `Name`, `Shortcut`, `Icon`, `Hint` and the mouse callbacks you need. Mutate the level only through `Editor.Document` transactions or `LevelEditor` operations, and implement `Cancel()` to undo a half-finished gesture (`Editor.Document.Cancel()`).
2. In `LevelEditor`'s constructor: create it, expose it as a property, and add it to the `Tools` list.
3. The toolbar button, the keyboard shortcut and the status bar hint come from the `Tool` members automatically. Update the Controls dialog text (`MenuBarView.ShowControls`) and the README.

## 9.9 Add a menu item or shortcut

- **Menu:** in `MenuBarView`'s constructor, `Item(menu, "Label", "Shortcut text", action, isEnabled)`. For a toggle, `Check(menu, "Label", null, getter, setter)`.
- **Shortcut:** in `LevelEditor.Shortcuts.cs`, add to `HandleCommandShortcuts` (Cmd/Ctrl combos) or `HandleShortcuts`. Respect the existing guards. Put keys that a focused control might need (Enter, Delete, arrows) after the `uiHasFocus` check.

## 9.10 Add an editor preference

Add a static property to `EditorPrefs` and a `Check(...)` in the View menu (or a toolbar control). Only the recent files list is persisted, through the settings file. It's the last block of lines, one path per line, most recent first; the first one sits where older builds kept their single "recently opened" path, so both builds can read each other's files. To persist more, extend `GameSettingsLoader`. The format is positional, so put new values *before* the recent files and bump its version string.

## 9.11 Add an icon

1. Draw it in a free 16×16 cell of `res/editor/editor_icons.png`, continuing the row-major order (8 icons per row). Any pixel editor works. Keep to the sheet's look: a 1 px dark outline (#0B0E1C) and colours from the editor palette. A coloured icon (green play, red bin) should mean the same thing as that colour elsewhere.
2. Add its name to the end of the `EditorIcon` enum. Enum order is sheet order.
3. Use it with `UiKit.Button(text, action, EditorIcon.YourIcon)`, a `UiAction`, or `EditorIcons.Sprite(...)` for a standalone icon.

## 9.12 Add a theme

`Theme` is one static palette today. To offer a choice:
1. Turn its fields into an instance (e.g. `record ThemeColors(...)`), with `Theme.Current` pointing at the active one, and add the new palettes.
2. Choose one before `Theme.ConfigureGum` runs in `GumHost.EnsureInitialized`. Save the choice in `EditorPrefs` and the settings file (recipe 9.10).
3. Switching while the editor is open: set the new palette, re-run the colour assignments from `ConfigureGum` (split them out so `EditorTheme.Apply` itself runs only once), then dispose and rebuild `EditorUi`. Gum visuals only read colours when they're created.
4. The icons are colour art. If a theme makes them clash (a light theme, say), give it its own sheet.


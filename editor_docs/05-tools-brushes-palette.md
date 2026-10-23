# 5. Tools, brushes & the palette

The **brush** is *what* you paint with (a tile, an entity, a stamp). The **tool** is *how* the mouse uses it (paint, fill a rectangle, erase, select). They're independent: `LevelEditor.Brush` and `LevelEditor.Tool`.

## 5.1 The tool contract (`Tools/Tool.cs`)

```csharp
public readonly record struct ToolInput(Vector2 World, int Col, int Row, bool Shift, bool Alt)
{
    public Vector2 Pixels => World / Game.TextureScale;   // texture pixels
}

public abstract class Tool(LevelEditor editor)
{
    public abstract string Name { get; }               // toolbar label
    public abstract KeyboardKey Shortcut { get; }      // V, B, R, E, I
    public abstract string Hint { get; }               // status bar text when idle
    public virtual bool IsBusy => false;               // a gesture is in progress
    public virtual void OnPress(ToolInput input, MouseButton button) { }
    public virtual void OnDrag(ToolInput input) { }    // every frame while the button is held
    public virtual void OnRelease(ToolInput input) { }
    public virtual void Cancel() { }                   // abort and undo the gesture so far
    public virtual void DrawOverlay(ToolInput hover, bool isHovered) { }   // world space
}
```

`EditorViewport` calls these (chapter 4.6). A press "captures" its button until release, so a tool always gets a matching `OnRelease`, or a `Cancel` if something interrupts it.

Tools never touch the level directly. They call `LevelEditor` operations (`LevelEditor.Edit.cs`) inside document transactions. Two kinds of operation:
- **"Now" methods** (`PaintLineNow`, `RemoveEntityNow`, `MoveSelectionTo`) mutate inside the caller's open transaction. They're used while dragging, with the tool calling `Document.Begin` on press and `Commit` on release.
- **Complete operations** (`PlaceEntity`, `PlaceStamp`, `FillRect`, `DeleteSelection`…) are their own undo step (`Document.Execute`).

## 5.2 The tools

### Brush (B): `BrushTool.cs`

| Input | Tile brush | Entity brush | Stamp brush |
|-------|-----------|--------------|-------------|
| Left press | `Begin("Paint …")`, paint the cell | place one entity (`PlaceEntity`) | place the stamp with its top-left at the cell (`PlaceStamp`) |
| Left drag | paint a **line** from the last cell to the current one | – | – |
| Left release | `Commit` (the whole stroke = one undo step) | – | – |
| Right press/drag | **erase gesture** (below) | same | same |
| Alt + left | **pick** what's under the cursor into the brush (stays on the Brush tool) | same | same |

Strokes are drawn with Bresenham lines (`LevelEditor.Line`), so moving the mouse fast never leaves gaps. Painting a cell with the spec it already has changes nothing (`LevelData.Set` returns false), so scrubbing over painted tiles doesn't create undo steps.

### Rectangle (R): `RectTool.cs`

Left-drag fills the dragged rectangle with the tile brush. Right-drag clears it to air. Each is one undo step (`FillRect`). Filling with an autotile writes the group into every cell, so the rectangle gets proper edges and corners automatically. With a non-tile brush, a left press only shows a hint ("fills with tile brushes"). Picking an entity or stamp from the palette while on this tool switches to the Brush tool.

### Erase (E): `EraseTool.cs`

Left or right drag runs the **erase gesture** (`EraseGesture` in `Tool.cs`, shared with the Brush tool's right button):

- If the press lands **on an entity**, the gesture becomes "erase entities". It removes that entity and any other entity the cursor passes over, and leaves tiles alone.
- Otherwise it becomes "erase tiles". It clears cells along the path (Bresenham again) and leaves entities alone.

Deciding the mode on press means clearing a row of ground can't accidentally take out the Goomba standing on it, and vice versa. The whole gesture is one undo step.

### Pick (I): `EraseTool.cs` (`PickTool`)

Click an entity or tile to make it the brush, then switch to the Brush tool. `LevelEditor.PickAt`:
- **Entity** under the cursor → an `EntityBrush` with that placement's **exact properties** (pick a red-palette Koopa, paint red-palette Koopas).
- Otherwise a **non-air cell** → a `TileBrush` with that cell's *stored* spec. On an autotile that's the group, not the resolved piece, so you keep painting auto-tiling ground. On a `?` block it includes the item.
- The palette item that produces an equivalent brush is highlighted if there is one (`PaletteCatalog.FindMatch`).

### Select (V): `SelectTool.cs`

**Press:**
1. On the **player start** → select it (Shift toggles it in or out) and prepare to move.
2. Else on an **entity** (topmost) → Shift toggles it in or out. Without Shift: if it isn't already selected, it becomes the only selection, then prepare to move. Pressing an already-selected entity keeps the whole selection, so you can drag a group.
3. Else start a **marquee**.

**Drag (move):** the thing you grabbed (the entity under the press, or the player start) is the **anchor**. Each frame its original feet plus the mouse offset go through `LevelEditor.SnapFeet` (see 5.3), and the whole selection moves by however far that moved the anchor. With 16 px snapping, the grabbed entity's feet therefore always land on a tile's bottom-centre, and an entity that started off the grid snaps onto it. The rest of the selection keeps its arrangement. Holding Alt moves freely in whole pixels.

The move starts once the mouse has travelled 4 screen pixels and the offset is non-zero; only then does it open a `"Move"` transaction. A plain click must never move an off-grid entity just by snapping it. Every frame, `MoveSelectionTo` sets each selected entity to *its original feet + offset*, and the player start too if it's selected. Positions are computed from the originals each frame, never accumulated, so rounding can't drift.

**Release:** commit the move, or finish the marquee:
- **Click** (moved less than 4 screen pixels): selects the **tile** under the cursor if the cell isn't empty, otherwise clears the selection. Shift+click on empty space does nothing.
- **Drag:** selects every entity whose rectangle intersects the marquee (adding to the selection with Shift) and sets the **region** to the tiles covered.

Keyboard actions on a selection (chapter 7): arrows nudge (Shift = a whole tile), Del deletes, `Cmd/Ctrl+C/X/V/D` copy/cut/paste/duplicate.

**What Delete does depends on the selection:** entities selected → delete the **entities only**. Otherwise a region → clear its **tiles**. Otherwise a tile → clear that tile. This is deliberate. A box selection usually contains both ground and enemies, and pressing Delete to remove the enemies shouldn't also delete the ground. Use *Clear Tiles* in the inspector, or Cut, to remove both.

## 5.3 Entity placement and snapping

When an entity brush places an entity, `LevelEditor.FeetAt(input)` decides where its **feet** (bottom-centre) go, in texture pixels:

| Snap (toolbar) | Feet X | Feet Y |
|----------------|--------|--------|
| 16 px (tile) | centre of the hovered tile | bottom of the hovered tile |
| 8 px (default) | rounded to the nearest 8 px (tile edges *and* centres) | bottom of the hovered tile |
| 1 px | exact | exact |

Hovering any part of the tile above the ground therefore stands the enemy *on* the ground, and those feet are exactly what's stored (chapter 2).

`FeetAt` puts the cursor's Y on the hovered tile's bottom and then calls `SnapFeet(feet)`. `SnapFeet` snaps an existing feet point to the nearest allowed spot: X to a tile centre (16 px) or the 8 px grid, and Y to the nearest tile bottom (or whole pixels with 1 px). Dragging uses `SnapFeet` directly (5.2).

`PlaceEntity` also:
- **Unique entities** (the Midway): if one already exists, it's *moved* there and its properties replaced, instead of adding a second.
- Selects the new or moved entity, so the inspector immediately shows its properties.

## 5.4 Brushes (`Brushes/Brush.cs`)

```
Brush (abstract)        Name, PaintsTiles, Source
├── TileBrush           Spec             – With(spec)
├── EntityBrush         Def, Props       – With(props)
└── StampBrush          Stamp
```

- Brushes are **immutable**. Editing a brush's properties in the inspector replaces it with `With(...)`, and `LevelEditor.UpdateBrush` swaps it in without switching tools.
- `PaintsTiles` is true only for `TileBrush`. It decides whether the Rectangle tool can use it.
- `Source` says where the brush came from:
  - a `PaletteItem`, which highlights it in the palette;
  - `PaletteCatalog.TilesetSource`, picked from the raw tileset view, which keeps the atlas selection outline visible;
  - `null`, a pasted clipboard or a picked brush with no matching palette item. Escape uses "a stamp brush with no source" to recognize **paste mode** and leave it.

## 5.5 Stamps (`Brushes/Stamp.cs`, `Brushes/Stamps.cs`)

A `Stamp` is a multi-tile structure placed in one click: a list of cells plus a list of entities.

```csharp
record StampCell(int Dx, int Dy, TileSpec Spec, bool OnlyOnEmpty = false, bool FillDown = false);
record StampEntity(int Dx, int Dy, string Key, ImmutableSortedDictionary<string,string> Props); // Dx/Dy in px
```

- **`OnlyOnEmpty`**: skip the cell if something is already there (pipes don't overwrite ground).
- **`FillDown`**: repeat the cell downward from its position until it hits a non-empty cell (with `OnlyOnEmpty`) or the bottom of the level. That's how a pipe grows down to the ground, or a flagpole's pole reaches the floor.
- `ResolveCells(level, col, row)` lists exactly what would be written. It's used both to apply and to draw the ghost, so the ghost is always accurate, including the fill-down length.
- `Apply` resolves all cells **before** writing any. Otherwise a fill-down column would see the cells the same stamp had just written.
- Stamp entities are skipped if an identical one (same key and position) already exists, so stamping twice in the same spot doesn't stack flagpoles. A unique entity in a stamp replaces the existing one.
- `Width`/`Height` come from the largest `Dx`/`Dy`. They're used for the ghost outline.

**Built-in stamps** (ported from the old prefab tiles):

| Stamp | Cells | Entities |
|-------|-------|----------|
| Pipe | top 24, 25. Body 32, 33 fill down. All only-on-empty. | – |
| Flagpole | ball 29, pole 37 fills down (passable) | `flagpole` at (6, 16) px |
| Lava | `lava` top, `lava_bottom` fills down | – |
| Castle Ceiling | 124, 125 (only-on-empty) | – |
| Small Castle | 6×5 block of tiles 48+ (top two rows narrower), **overwrites** | `castle_entrance` at (40, 64) px |

**Stamps from regions:**
- **Copy/Cut** (`Stamp.FromRegion`): the region's non-air cells plus the entities inside it, relative to the region's top-left. Air is **transparent** when pasting: copied cells overwrite, empty cells leave what's underneath. An invisible marker cell in the bottom-right corner keeps the stamp's full size even when its edges are empty.
- **Copying only entities** (no region): a stamp with just the entities, positioned relative to the top-left of their bounding box.
- **Tileset drag** (`PaletteCatalog.TilesetBrush`): one cell → a `TileBrush`, several → a stamp of raw tiles with default solidity.

## 5.6 The palette catalog (`Palette/PaletteCatalog.cs`)

`PaletteCatalog.Items` is the list behind the palette panel. Each `PaletteItem` has a name, a category, an `Icon`, an optional `Badge` (a small second icon, e.g. the coin on "? Block (Coin)"), and a factory that creates its brush with `Source = item`.

`Icon` is a texture name plus a source rectangle. Helpers: `Icon.Tile(id)`, `Icon.TileRegion(col, row, w, h)`, `Icon.Entity(col, row)` (a cell of the curated `entity_selector.png`), `Icon.Item(type)` (from `items.png`), `Icon.Frame(texture, y, w, h)`.

| Category | Contents |
|----------|----------|
| Terrain | the six autotile groups |
| Blocks | Block, Underground Block, ? blocks (coin / power-up / hidden 1-Up), bricks (plain / coins / star / underground / castle), empty blocks, coin, castle stairs/walls/ceiling, bridge, chain |
| Structures | Pipe, Flagpole, Small Castle, Castle Ceiling, Lava stamps |
| Enemies | Goomba ×3 palettes, Koopa ×3 palettes, Red Koopa, Paratroopa, Firebar, Bowser |
| Platforms | Lift (Constant), Lift (Back & Forth) |
| Objects | Axe, Toad |
| Level Flow | Warp Pipe, Warp Exit, Midway, No Piranha Plant, Flagpole Trigger, Castle Entrance |
| Tileset | the whole raw atlas (not items; a special view, see chapter 6) |

Palette "variants" such as *Goomba (Underground)* aren't separate entity types. They're the same `goomba` entity with a preset property (`palette=Underground`). Once placed, the palette can be changed in the inspector.

Other helpers:
- `PaletteCatalog.Default` is the first item (Grass Ground). It's the starting brush, and the brush restored when leaving paste mode.
- `FindMatch(brush)` finds the item whose brush is equivalent (same spec, or same entity and same props). It's used to keep the palette highlight correct after picking.
- `TileNames.Describe(spec)` (`TileNames.cs`) produces the human-readable names in the status bar and inspector ("? Block (Star)", "Underground Brick (hidden)", "Bush (left end)"…). For plain tiles it uses the palette name if one exists, otherwise "Tile #id".

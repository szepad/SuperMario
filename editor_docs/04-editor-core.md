# 4. Editor core (`Source/Editor`)

Everything here is in the `Mario2.Editor` namespace. Chapter 1 covered how `LevelEditor` wires it together and the per-frame order. This chapter goes class by class.

## 4.1 `EditorDocument`: the level + undo/redo (`EditorDocument.cs`)

### State

| Member | Meaning |
|--------|---------|
| `Level` | the live `LevelData` being edited. Edits mutate it **in place**. |
| `FilePath` | where it's saved, or `""` if never saved |
| `Version` | increments on **every** change, including undo/redo and save. Anything that caches derived data (the preview) compares it. |
| `IsDirty` | `_stateId != _savedStateId` (see below) |
| `CanUndo` / `CanRedo`, `UndoLabel` / `RedoLabel` | for the Edit menu ("Undo Paint Brick") |
| `Changed` | event raised on every change. Nothing subscribes today; it's there as a hook. |

### Transactions

An edit is always wrapped in a transaction, and **one transaction = one undo step**.

```csharp
Document.Begin("Paint Brick");     // snapshot = Level.Clone()
Document.Level.Set(c, r, spec);    // mutate in place...
Document.MarkChanged();            // ...and say so
Document.Level.Set(c2, r2, spec);
Document.MarkChanged();
Document.Commit();                 // push the snapshot onto the undo stack (only if something changed)
```

- `Begin(label)` clones the level as the "before" snapshot. If a transaction is already open it is committed first, so transactions never nest.
- `MarkChanged()` must be called after mutating `Level`. It assigns a new state id, flags the open transaction as changed, and bumps `Version`.
- `Commit()` pushes `(label, before-snapshot, before-state-id)` onto the undo list, but **only if `MarkChanged` was called**. A drag that changed nothing leaves no empty undo step. It also clears redo, as any new edit does.
- `Cancel()` restores the "before" snapshot if anything changed. It's used when a drag is aborted with Escape or interrupted by a tool switch.
- `Execute(label, level => { ...; return changed; })` is the one-shot form: Begin → edit → MarkChanged if it returned true → Commit. **The lambda must return `true` only if it really changed something**, otherwise you get a no-op undo step (or a missed one).

A whole brush stroke is a single transaction. The brush tool calls `Begin` on mouse down, paints cells every frame while dragging, and calls `Commit` on mouse up.

### Snapshots, undo and redo

Undo is **memento-based**: the stack holds whole `LevelData` clones. That's cheap because a clone only copies references to immutable specs and placements (about 3,000 references for a 211-column level). It's also robust: every operation, however complex (column deletes, pastes, resizes), undoes correctly without per-operation undo code.

- The undo list is capped at **200** steps (oldest dropped first).
- `Undo()` commits any open transaction, pops the last snapshot, pushes the *current* level onto redo, and makes the snapshot the live level. No extra copy is needed: the popped snapshot isn't referenced anywhere else.
- `Redo()` mirrors it.

### The dirty flag

Each distinct content state gets an id (`_stateId`). Saving records the current id as `_savedStateId`. Snapshots carry the id of the state they represent, so **undoing back to the saved state makes the document clean again**, and redoing makes it dirty again. Two more entry points:

- `MarkSaved()`: after a successful save.
- `MarkUnsaved()`: makes the document dirty without an undo step. It's used after opening a legacy file, because it "needs saving" in the new format.

## 4.2 `EditorSelection` (`EditorSelection.cs`)

The selection can hold any combination of:

| Part | Type | Selected by |
|------|------|-------------|
| `Entities` | set of placement **ids** | clicking or box-selecting entities |
| `Spawn` | bool (the player start) | clicking Mario |
| `Tile` | `(Col, Row)?` | clicking a non-empty cell with the Select tool |
| `Region` | `TileRect?` | box-selecting (drag) |

Ids rather than object references are what make selections survive undo/redo. After an undo the placement objects are different objects, but the ids are the same.

Rules the methods enforce:
- `SelectEntity(id)` replaces the selection unless `additive`. `ToggleEntity` and `ToggleSpawn` are the Shift+click forms.
- `SelectTile` clears everything else. A tile selection is always alone.
- `SetRegion` keeps the entity part, so a box selection = the region + the entities inside it.
- `Prune(level)` drops ids that no longer exist and clamps or drops a region that no longer fits. It's called after undo/redo and after width changes.

`TileRect` (same file) is a small value type: `Col, Row, Width, Height`, `Right`/`Bottom` (exclusive), `FromCorners` (inclusive corners in any order), `Contains`, `ClampTo(level)`, `ToWorld()`.

## 4.3 `EditorPreview`: what the viewport draws (`EditorPreview.cs`)

The preview mirrors the document as drawable objects:

- **Tiles:** a `TileSpec[14, width]` of *resolved* specs (autotiles already turned into concrete pieces), plus a cache of one runtime `Tile` per distinct spec (`TileFor`). Hundreds of grass-top cells share one `SimpleTile` instance. That works because the editor only ever *draws* tiles, and `Tile.Draw(col, row, opacity)` takes the position as an argument.
- **Entities:** a private `World` created with `IsEditorPreview = true`, holding real entity instances built through `LevelBuilder.Spawn`. That's the same code the game uses, so a Firebar looks like a Firebar and a platform has its real size. The world is **never updated**: no physics, no AI, no timers.

### `Sync(level, version)`

Called twice per frame (chapter 1). It returns immediately if neither the level object nor the version changed.

1. If there's no world yet or the width changed → `Reset`: a fresh preview world, a new resolved array, and the entity maps cleared.
2. If the background changed → `World.SetBackground` (the viewport draws its parallax layers).
3. **Re-resolve every cell.** It's a few thousand cheap lookups, simpler and safer than tracking which neighbours a change affects.
4. **Entities, incrementally:** for each placement, if the preview already has an entity for that id *and* the placement record is unchanged (record equality), keep it. Otherwise destroy and recreate it. Entities whose ids disappeared are removed.
5. Rebuild `_ordered` (placements sorted by `LayerDepth`, stable by level order). It's used for hit-testing.
6. Put the preview's `Player` at the level's spawn.

### Entities that spawn other entities

A `Firebar` spawns six or twelve `FirebarChild` entities into its world when created. `Create` records the world's entity count before spawning and treats everything added after it as belonging to that placement (`PreviewEntity.Spawned`). Removing the placement removes all of them, so dragging a Firebar never leaves orphaned fireballs behind. Every spawned entity is `Activate()`d, because `Entity.Draw` skips inactive entities.

### Queries

- `HitTest(worldPoint)` → the **topmost** placement id whose root entity's `Rect` contains the point (searches `_ordered` backwards).
- `PlacementsInside(worldRect)` → ids whose rect intersects (box select).
- `SpawnHit(worldPoint)` → is the point on the player start?
- `RootOf(id)` → the entity instance, for drawing outlines.
- `ResolvedAt(col, row)` → the resolved spec.
- `UpdateAnimations()` → advances every preview entity's sprite and the background's animated parallax layers (lanterns, waterfall palette cycling). It's called each frame when *View → Animate Sprites* is on. Only `Sprite.Update()` runs, never `Entity.Update()`, so nothing moves.

## 4.4 `GhostWorld` (`GhostWorld.cs`)

**`GhostWorld`** is a second scratch world for **brush previews**: the translucent Goomba under your cursor, or the entities inside a pasted stamp.

- `Set(placements)` rebuilds the ghost entities only if the requested set differs (compared by key, position and props reference), so hovering doesn't recreate entities every frame.
- `Draw(opacity)` temporarily sets each ghost's `Sprite.Opacity` while drawing. `DrawEditor` icons ignore opacity, so a ghost warp pipe's arrow is drawn opaque.
- `Clear()` is called when the brush isn't an entity or stamp.

The editor never converts between feet and positions. Level data stores feet (chapter 2), and `EntityDef.Create` turns them into a position when the preview or ghost entity is built.

## 4.5 `EditorCamera` (`EditorCamera.cs`)

- `Viewport`: the screen rectangle the level is drawn into (the space the UI leaves free). It's set every frame from the Gum layout.
- `Target`: the **world point shown at the viewport's top-left corner**. This differs from raylib's usual "target = centre" convention. `AsCamera2D` builds a `Camera2D` whose `Offset` is the viewport's top-left, so `BeginMode2D` maps `Target` there.
- `Zoom`: screen pixels per world unit, clamped to **0.05–4**. The status bar shows `Zoom × 100%`. 100% means "the size it has in the game", where one world unit is one pixel of the 1024×896 game screen.

| Method | What it does |
|--------|--------------|
| `ScreenToWorld` / `WorldToScreen` | `(screen − viewportPos) / zoom + target`, and the inverse |
| `Pan(screenDelta)` | `Target -= delta / Zoom`, so dragging moves the world with the mouse |
| `ZoomAt(screenPoint, factor)` | zooms while keeping the world point under the cursor fixed |
| `ZoomCentered(factor)` | `ZoomAt` the middle of the viewport (the `Cmd/Ctrl` +/- shortcuts) |
| `FitZoom` | the zoom at which the level's 14 rows fill the viewport height (with a 10% margin) |
| `FitHeight(centerX)` | zoom to `FitZoom`, then centre |
| `ToggleSmartZoom(screenPoint)` | the trackpad's two-finger double tap: zoom to `FitZoom`, or to twice that if it's already there, around the cursor |
| `CenterOn(worldPoint)` | puts a world point in the middle of the viewport |
| `Clamp(levelWidth)` | keeps at least two tiles of the level visible, so you can't scroll it out of sight |

`LevelEditor.FrameSpawn` (Home key, `Cmd/Ctrl+0`, also on opening a level) fits the height and puts the player start about 30% from the left edge. On open it waits until the viewport has a real size. Gum only computes the layout on its first update, hence `_frameOnNextUpdate`.

## 4.6 `EditorViewport`: input and drawing (`EditorViewport.cs`)

### Input: `Update(area, mouseBlocked, keyboardFree, scroll)`

1. `Camera.Viewport = area`.
2. `IsHovered` = the mouse is inside `area` **and** not blocked. It's blocked when a dialog is open, when the mouse is over an interactive Gum element, or when a menu is open (chapter 7).
3. `Hover` = a `ToolInput` for the mouse: world position, tile `Col/Row` (`floor(world / 64)`, so negative outside the level), and the Shift/Alt state.
4. **Panning:** middle mouse, or Space + left mouse, starts a pan when hovered and no tool gesture is running. While held, the camera moves by the mouse delta.
5. **Scrolling and pinching** (`HandleScroll`). `scroll` is this frame's `ScrollInput`. Its scrolling part is already empty unless the gesture belongs to the level (chapter 7.3 explains the routing).
   - With `Cmd/Ctrl` held: zoom around the cursor by `1.15^clicks × 1.005^points`.
   - Without: smooth scrolling pans (`Camera.Pan(points)`), a mouse wheel zooms by `1.15^clicks`, and a sideways wheel (or `Shift`+wheel) pans 64 px per click.
   - While hovered, a pinch zooms around the cursor and a smart-zoom tap calls `ToggleSmartZoom`.

   Scrolling still pans during a tool drag, so a long stroke can scroll the view along.
6. **Tool gestures:** a left or right press while hovered and idle captures that button, clears UI keyboard focus, and calls `Tool.OnPress`. While it's held, `Tool.OnDrag` runs every frame. On release, `Tool.OnRelease`. **A captured gesture continues even if the mouse leaves the viewport** or passes over UI, so a drag can't get stuck half-done.
7. `Camera.Clamp`.

`CancelGesture()` calls `Tool.Cancel()` (which undoes the partial edit) and stops panning. It runs when switching tools, opening dialogs, undoing, loading a document and starting a playtest. `IsBusy` is true during any gesture or pan.

### Drawing: `Draw()`

Everything is drawn straight to the window, clipped with `BeginScissorMode(viewport)`, inside `BeginMode2D(camera)`. In order:

1. The viewport filled with the "outside" colour (dark grey).
2. The level rectangle filled with the background's clear colour (sky blue, black…).
3. **Background** (View → Background): each parallax layer is tiled across the level width, world-aligned (no parallax scrolling in the editor). It uses the same vertical placement the game gets with its camera offset. Whatever the layers draw outside the level is then covered with the outside colour.
4. Entities on the **background layer**, then **tiles**, then all **other entities**: the same order as `World.Draw`. No placeable entity uses the background layer today; in the game it's piranha plants, which are spawned at runtime. Only visible columns are drawn. Hidden blocks are drawn at 35% opacity. Everything else is drawn as the game draws it (the old editor dimmed passable tiles; now there's a collision overlay instead).
5. **Tile overlays:** the collision tint (View → Collision Overlay) on solid cells, and the item a `?` block or brick contains, drawn **over the block** at full size and 80% opacity, so the block still shows through (`DrawItemOverlay`). Tile brush ghosts use the same helper, so a "? (Star)" brush previews its star.
6. **Grid** (G): 1-pixel lines over the visible columns.
7. **Screen guides** (View → Screen Guides): a line every 16 columns, the width of one game screen.
8. Level border, then the **player start marker**: a green triangle under Mario's feet, with "START" when zoomed in far enough.
9. **Selection:** selected entities filled and outlined, plus outlines for the player start, the selected tile, or the selected region.
10. **Tool overlay:** `Tool.DrawOverlay(hover, isHovered)`, e.g. the brush ghost, the rectangle being dragged, the marquee, hover outlines.

Helpers used by the tools:
- `DrawOutline(rect, color)`: a rectangle outline that's always 2 *screen* pixels thick (`2 / Zoom` world units).
- `DrawBrushGhost(brush, hover)`: tile brushes draw the tile translucent (autotiles resolved for that spot with `AutoTiles.ResolveFor`), entity brushes draw a ghost entity at the snapped feet position, stamps draw every cell they would write plus their entities.
- `DrawTileGhostArea(spec, rect)`: the rectangle tool's fill preview.

All colours (selection blue, hover white, erase red, grid, guides, spawn green...) come from `Theme` (chapter 6.6), so tools and the palette use the same ones.

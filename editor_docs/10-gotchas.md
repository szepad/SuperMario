# 10. Gotchas & known limitations

## Things that will bite you when changing code

- **Level coordinates are feet, not `Position`.** Files and `LevelData` store each entity's (and the player start's) bottom-centre. Changing an entity class's `Size`/`SizeOffset` keeps placed entities standing where they were: their feet stay put and the box grows around them. If you need an entity to sit differently (a sprite that should overlap the ground, say), change its `Size`/`SizeOffset`, not the level files. Never store an `Entity.Position` in a `LevelData`. `EntityDef.Create` expects feet.
- **Keys and enum names are part of the file format.** Entity keys (`goomba`), property keys (`palette`), autotile keys (`grass`) and the names of enum members used in properties or meta (`Palette.Underground`, `MusicName.Overworld`…) are written into `.lvl` files. Renaming any of them breaks existing levels unless the reader keeps accepting the old name. *Reordering* enum members is safe; the old integer-based format wasn't.
- **`Execute` lambdas must return whether they changed something.** Returning `true` for a no-op creates an empty undo step and marks the level dirty. Returning `false` after a real change loses the undo step and the dirty flag.
- **Never mutate a `LevelData` outside a transaction** in editor code. Undo snapshots are taken in `Document.Begin`, so a change made outside one has no undo step of its own. Worse, undoing the step *before* it silently rolls it back too, because that step's snapshot predates it.
- **Always call `MarkChanged()` after mutating inside a transaction.** Otherwise the preview won't update (it watches `Document.Version`) and the step won't be recorded.
- **Don't keep references to `EntityPlacement` objects across frames.** They're replaced on every edit. Keep the `Id` and look it up (`Level.FindEntity(id)`), as the inspector and selection do.
- **Never switch scenes in the middle of `LevelEditor.Update`.** `Game.LoadScene` unloads the editor immediately, and the rest of the update would then run on a disposed editor. Set a flag and act on it as the last statement of `Update`, the way `ExitToTitle` uses `_exitRequested`. Starting a playtest mid-update is fine because `LoadSubScene` doesn't unload the editor.
- **Gum layout is lazy-ish.** Absolute positions of Gum elements are only reliable after Gum's update has run. That's why camera framing on open waits a frame (`_frameOnNextUpdate`).
- **Gum quirks** (popup roots, button widths, toggle styling): see chapter 6.5 before touching UI code.
- **Gum visuals read their colours when they're created.** `Theme.ConfigureGum` must run before any control exists, and changing a `Theme` colour later won't repaint existing controls. Never put a literal `new Color(...)` in editor code: add a named colour to `Theme`.
- **Don't let Gum scroll the editor's panels.** Gum rounds the wheel to whole clicks, which breaks trackpad scrolling. `EditorUi.ScrollViewerUnderCursor` sets `MouseWheelScrollSpeed = 0` on any `ScrollViewer` under the cursor, and the editor scrolls it itself (chapter 7.3). That routing has to run *before* `GumService.Update`. A new scrolling panel needs no extra work, but don't give it a non-zero `MouseWheelScrollSpeed`.
- **`MacTrackpad` patches GLFW's `GLFWContentView` class through the Objective-C runtime.** It replaces `scrollWheel:` (and still calls GLFW's version) and adds `magnifyWithEvent:` / `smartMagnifyWithEvent:`, which GLFW doesn't implement, so pinches used to be dropped. It runs once per process, on macOS only. If a raylib update renames the class, `Install()` returns false and the editor quietly falls back to raylib's wheel value: two-finger scrolling still pans, but pinch stops working. Its callbacks run in native code, so they must never throw.

## Behaviour that might surprise you

- **Delete with a box selection removes entities, not tiles** (if any entities are selected). Use *Clear Tiles* or Cut to remove the ground too.
- **Paste overwrites tiles, but empty cells in the copy are transparent.**
- **Shrinking the width in the inspector keeps entities beyond the new edge.** They're still saved and spawned, off the end of the level. *Level → Delete Column/Screen* removes them properly.
- **Sublevels load from disk during playtests.** Edits to a bonus room only show up when you pipe into it after saving it. (The level being edited always uses your in-memory version, including when a bonus room returns to it.)
- **1-1 gets piranha plants when playtested** (chapter 8.4). The game skips them only when the campaign's current level is 1. A `piranha_plants` level setting (recipe 9.7) would fix this properly.
- **Dying in a playtest waits out the 5-second death timer** before returning to the editor. Press Escape to leave immediately.
- **Edit the files you mean to edit.** The build copies `levels/` next to the executable (`bin/.../levels`). Opening and saving those copies won't change the repo's `levels/` folder, and a rebuild only overwrites them if the repo file is newer. Open levels from the repo folder; the editor reopens whatever you last opened.
- **Only the recent files are remembered between launches** (up to 10, File → Open Recent; the editor reopens the first). Grid, snap and the View toggles reset each launch.

## Known limitations

- **Text values can't contain line breaks.** The format is line-based and the writer doesn't escape newlines. The editor's text boxes are single-line, so this only matters for hand-edited files.
- **The level height is fixed at 14 rows**, as it is everywhere in the game (`World.Rows`).
- **Undo history is capped at 200 steps** and lives in memory only. It's cleared when you open or create a level.
- **`EditorPreview.Sync` re-resolves every cell on every change.** That's instant for these levels (about 3,000 cells); a 4096-wide level is ~57,000 lookups per change, still fine but the first thing to optimize if needed.
- **Button widths are estimated from character count** (chapter 6.5), so very unusual labels may look slightly loose or tight.
- **The title HUD shows "WORLD 1-0" in playtests** (chapter 8.4).

## Testing checklist after changes

1. `dotnet build`: no new warnings.
2. Start the campaign (title → Play) and enter a pipe to a bonus room and back.
3. In the editor:
   - open a level, paint ground and check the autotile edges;
   - undo/redo a stroke;
   - place and drag an entity;
   - edit a property in the inspector;
   - copy/paste an area;
   - F5 and Shift+F5, then return;
   - on a laptop: two-finger scroll pans the level and scrolls the palette, pinch zooms.
4. Save, reopen, and check that the file's text looks as expected.
5. If you changed the format or catalog, load every file in `levels/` (start the campaign or open each in the editor) and watch the console for warnings.

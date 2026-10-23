# 8. Playtesting

The editor's core promise: **press F5 and play exactly what's on screen**, saved or not. When the playtest ends, you're back in the editor with everything as you left it.

## 8.1 What happens when you press F5 (`LevelEditor.Playtest`, in `LevelEditor.Files.cs`)

1. If a playtest is already running (`Scene.SubScene` isn't null) → do nothing. This guards against double-starting from a button click plus a key in the same frame.
2. **Clone the level.** The playtest gets its own copy, so the game can never modify the document.
3. **From the cursor (Shift+F5):** the hovered cell must be inside the level. The spawn (which is Mario's **feet**) is set to the bottom-centre of that cell, and **the pipe intro is turned off** for this run.
4. Cancel any viewport gesture and commit any open transaction.
5. **Choose the level's identity:** the saved file's normalized path (`LevelPaths.Key(FilePath)`), or `"unsaved://level"` if it has never been saved (see 8.3).
6. `LevelBuilder.Build(clone, identity)`, the same builder the campaign uses.
7. `SessionManager.Reset()`: lives, score and coins back to defaults, midway and "reloaded" flags cleared.
8. `Scene.LoadSubScene(new GameScene(world))`. `EditorScene` stops updating and drawing, and the game takes over the window.

## 8.2 `GameScene` in playtest mode (`Source/Scenes/GameScene.cs`)

`GameScene` has two constructors:

- `GameScene()`: the campaign. Loads `01.lvl`, `02.lvl`… through `SessionManager` (which now uses `LevelLoader.Load`).
- `GameScene(World playtestWorld)`: a playtest. `IsPlaytest` is true.

In playtest mode:
- `OnLoad` → `StartPlaytest`: registers the world in the scene's sublevel cache under its identity key, hooks up the world's events, and starts gameplay immediately (no "WORLD 1-1" loading screen).
- **Leaving** (`LeavePlaytest`) happens on:
  - **reaching the end of the level** (flagpole or axe → `World_OnLevelEnd`). This used to load campaign level 1;
  - **dying**: after the usual 5-second death timer (`OnPlayerDeath`);
  - **Escape**: the existing "has a parent scene" path in `UpdateGameplayState`.

  `LeavePlaytest` unloads the sub-scene, returning to the editor. If there's somehow no parent, it goes to the title screen.
- Back in the editor, `EditorScene.OnSubSceneUnload` → `LevelEditor.OnReturnFromPlaytest` shows the mouse cursor again (the game hides it), stops the music, and forces a window title refresh.

Rendering switches automatically. `Game.Draw` asks `ActiveScene.ActiveLeaf.DrawsToWindow`: during the playtest the leaf is `GameScene` (virtual screen, letterboxed), afterwards it's `EditorScene` again (window resolution).

## 8.3 Level identity, sublevels and pipes

Levels connect through warp pipes: `dest=levels/bonus01.lvl` loads another file, `return_to_parent=true` goes back to the level you came from, and an empty `dest` means "a warp exit in this same level".

`GameScene` keeps a cache, `_loadedSubWorlds`, keyed by `LevelPaths.Key(path)`. When a pipe needs a level, it's looked up **in the cache first**, then loaded from disk with `LevelLoader.Load`. Player state and remaining time carry over, and the new world's `ParentWorldPath` remembers where you came from.

The playtest world is put in that cache **under its identity key** before anything happens. That's what makes these cases work:

| Situation | What happens |
|-----------|--------------|
| A pipe in the level leads back into the **same** level | `World.HandlePipeTransition` checks `LevelPaths.Same(dest, LoadedPath)` and looks for the matching warp exit in the current world |
| A bonus room's `return_to_parent` pipe | `Destination = ParentWorldPath` = the playtest identity → **found in the cache**, i.e. your in-memory, possibly unsaved level, not the old file on disk |
| A pipe to a **different** file (a bonus room) | loaded **from disk**. Unsaved edits to *that* level aren't included; save it first. |
| An unsaved level | identity is `"unsaved://level"`, which is never a real file, so only the cache can provide it |

### Walkthrough: playtesting 1-2 (pipe intro)

`02.lvl` has `pipe_intro true`.

1. `StartPlaytest` → the world is cached under its identity → `StartWorld()`.
2. `IsPipeIntro && !SessionManager.Reloaded` → remember `dest = world.LoadedPath` (the identity), then `LoadSublevel("levels/pipe_intro.lvl")` from disk.
3. The intro level's warp pipe gets `Destination = dest`, and the `PipeIntroCutscene` walks Mario into it.
4. The pipe triggers `OnPipeLoadSublevel` → `LoadSublevel(dest)` → **cache hit**: the in-memory 1-2 → play.

This works whether 1-2 is saved, dirty, or never saved.

## 8.4 Things that differ from the campaign

- The HUD shows **WORLD 1-0**: `SessionManager.Reset()` sets the current level to 0, and the campaign increments it as it loads levels.
- **Piranha plants** are spawned on pipe tiles at level start *unless* `SessionManager.CurrentLevel == 1` (that's how 1-1 avoids them in the campaign). In a playtest the current level is 0, so **1-1 gets piranha plants when playtested**. The old editor behaved the same way. A per-level setting would be the proper fix (see chapter 10).
- Lives and score start fresh every playtest. Dying doesn't count down lives; the playtest just ends.
- The midway checkpoint and "reloaded after death" logic don't apply. Every playtest starts at the spawn, or at the cursor.

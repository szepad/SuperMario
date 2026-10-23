# 2. Level data (`Source/Game/Level`)

Everything in this folder is in the `Mario2` namespace and knows nothing about the editor. It's the shared vocabulary of the game, the file format and the editor.

## 2.1 `TileSpec`: what a cell *is* (`TileSpec.cs`)

A `TileSpec` is an **immutable record** describing one grid cell. It isn't a gameplay object. It knows how to *create* one (`CreateTile()`) and whether it's solid (`IsSolid`).

| Record | Fields | Solid | `CreateTile()` returns |
|--------|--------|-------|------------------------|
| `AirSpec` (`TileSpec.Air`) | — | no | the shared `SimpleTile.Air` |
| `SimpleSpec` | `Id` (tileset index), `Solid` | `Solid` | `new SimpleTile(Id, Solid)` |
| `AutoSpec` | `Group` (an `AutoTileGroup`) | the group's `IsSolid` | **throws**: must be resolved first (see 2.3) |
| `QuestionSpec` | `Item`, `Hidden` | yes | `QuestionTile` + `SetItem` (+ `SetHidden`) |
| `BrickSpec` | `Item`, `Hidden`, `Palette` | yes | `BrickTile` + `SetItem` (+ `SetHidden`) + `SetPalette` |
| `AnimSpec` | `Kind` (`Coin`, `GrassLeft/Middle/Right`, `Lava`, `LavaBottom`) | no | `CoinTile`, `GrassDecorTile*`, `LavaTile`, `LavaTileBottom` |

Things worth knowing:

- **Value equality.** Because they're records, `new SimpleSpec(16, true) == new SimpleSpec(16, true)` is `true`. This matters in three places:
  - `LevelData.Set` returns `false` when the new spec equals the old one, so painting over identical tiles creates no undo step and no "dirty" flag.
  - The file writer builds its legend with a `Dictionary<TileSpec, string>`.
  - The legacy parser maps autotile outputs back to their groups.
- `AutoSpec` holds a reference to its group, and groups are compared **by reference**. Each group caches one `AutoSpec` instance in `AutoTileGroup.Spec`, so use that rather than `new AutoSpec(...)`.
- `SimpleSpec(int id)` (one-argument constructor) takes the default solidity from `TilesetInfo`.
- `CreateTile()` always returns a **new** runtime tile (except air), because runtime tiles have per-cell state such as bounce animation, `Col`/`Row` and `World`.

## 2.2 `TilesetInfo`: facts about the tileset image (`TilesetInfo.cs`)

- The atlas is `res/tiles/tileset.png`: 8 columns of 16×16 tiles. Tile id = `row * 8 + col`. `SourceRect(id)` gives the pixel rectangle.
- `IsSolidByDefault(id)` is `false` for decorative ids (castle walls, bushes, flagpole parts, the chain…). The list is the old raw-tile window's list plus id `110` (the chain), which matches how every shipped level used those ids.
- Defaults matter in two places: raw tiles picked from the Tileset palette, and the one-argument `SimpleSpec(id)`. Files always store solidity explicitly, so changing this table never changes existing levels.

## 2.3 Autotiles (`AutoTiles.cs`)

An autotile cell stores *which group it belongs to* (e.g. "grass") rather than a concrete tile. Its look is computed from its neighbours every time it's needed.

### The model

Every autotile in the game follows the same rule. Its look depends on:

1. **horizontal connections**: is there a connecting neighbour to the **east** and/or **west**? That picks one of four pieces of an `AutoTileStrip`:

   | east | west | piece |
   |------|------|-------|
   | ✓ | ✓ | `Middle` |
   | ✓ | – | `Left` (the left end: only something to its right) |
   | – | ✓ | `Right` |
   | – | – | `Single` |

2. **north connection** (optional): if the group has a `Body` strip and the cell has a connecting neighbour **above**, the `Body` strip is used instead of `Top`. This is how ground shows grass on the top row and dirt below. The south neighbour is never consulted.

"Connecting" (`Connects`) means:
- the neighbour is outside the level (**map edges always connect**, so ground runs cleanly off the screen edge), or
- the neighbour is an `AutoSpec` of the **same group**, or
- the group has `ConnectToSolid` and the neighbour's spec `IsSolid` (bushes use this, so a bush next to a pipe or block gets a proper end).

Neighbour solidity comes from the neighbour's *spec*, not its resolved tile, so resolution never recurses.

### The groups

| Key | Name | Solid | Top strip (L, M, R, single) | Body strip | ConnectToSolid |
|-----|------|-------|------------------------------|------------|----------------|
| `grass` | Grass Ground | ✓ | 0, 1, 2, 3 | 8, 9, 10, 11 | – |
| `underground` | Underground Ground | ✓ | 4, 5, 6, 7 | 12, 13, 14, 15 | – |
| `tree` | Treetop | ✓ | 41, 42, 43, 42 | – | – |
| `tree_trunk` | Tree Trunk | – | 44, 45, 46, 47 | – | – |
| `castle_floor` | Castle Floor | ✓ | 105, 106, 107, 106 | 113, 114, 115, 114 | – |
| `grass_decor` | Bush | – | GrassLeft, GrassMiddle, GrassRight, GrassMiddle (animated) | – | ✓ |

These tables reproduce the old editor's 16-entry rule tables exactly. The old tables had the same structure, just written out longhand. Migration verified this tile by tile (chapter 3).

### Worked example

Grass at the left edge of a level (`G` = `AutoSpec(grass)`, `.` = air):

```
col:      0  1  2  3  4
row 10:   G  G  G  .  .
row 11:   G  G  G  G  .
```

| Cell | North | West | East | Strip | Piece | Tile |
|------|-------|------|------|-------|-------|------|
| (0,10) | air ✗ | map edge ✓ | G ✓ | Top | Middle | 1 |
| (2,10) | air ✗ | G ✓ | air ✗ | Top | Right | 2 |
| (0,11) | G ✓ | map edge ✓ | G ✓ | Body | Middle | 9 |
| (2,11) | G ✓ | G ✓ | G ✓ | Body | Middle | 9 |
| (3,11) | air ✗ | G ✓ | air ✗ | Top | Right | 2 |

Cell (3,11) shows that a cell in the lower row still gets a grass top when nothing is above it. That's exactly the step shape in 1-1.

### API

- `AutoTiles.ResolveAt(level, col, row)`: the concrete spec of a cell. Non-autotile specs are returned unchanged.
- `AutoTiles.ResolveFor(level, group, col, row)`: what a cell of `group` *would* look like there. The editor uses it to draw brush previews before you click.
- `AutoTiles.Get(key)` and `AutoTiles.All` are used by the file format and the palette.

Resolution is a pure function of the grid, so there's no stored "rule reference" to keep in sync. Erase a tile and its neighbours re-resolve the next time anything draws or builds them.

## 2.4 Entity catalog & property schema (`EntityCatalog.cs`, `PropertyDef.cs`)

Every placeable entity type is registered once in `EntityCatalog`'s static constructor:

```csharp
Add("goomba", "Goomba", EntityCategory.Enemy, w => new Goomba(w))
    .Enum("palette", "Palette", Palette.Overworld, (e, v) => e.SetPalette(v));
```

That single declaration drives:
- the **file format**: `entity goomba 1320 64 palette=Overworld`;
- **loading**: the factory plus applying each property to the new entity;
- the **inspector**: one combo box labelled "Palette" with the enum's names;
- **validation and normalization**: `palette=underground` normalizes to `Underground`, `palette=Blue` is rejected with a warning.

### `EntityDef`

| Member | Meaning |
|--------|---------|
| `Key` | stable id stored in files (`goomba`, `warp_pipe`…). **Never rename one** without a migration. |
| `Name`, `Category` | shown in the editor |
| `EntityType` | the C# type (informational) |
| `IsUnique` | only one may exist per level (the Midway). Placing another *moves* the existing one. |
| `Properties` | the schema, in declaration order (also the order in files and the inspector) |
| `GetValue(placement, property)` | the placement's stored value, or the property default |
| `Create(world, placement)` | `factory(world)` → `SetFeetPos(X*4, Y*4)` → apply **every** property in order → `SetFeetPos` again. Does **not** spawn the entity. |
| `DefaultProps()` | all properties at their defaults (palette presets start from this) |

Apply order is deliberate:
1. **Position first**, because some property setters use it: `Firebar.SetLength` builds its fire children from `Position`.
2. **Then the properties.**
3. **Then the feet are set again**, because properties can change the entity's size. A platform's `size` does, and since the stored point is the feet, the position has to be recomputed for the new size.
4. **Finally** (in `LevelBuilder`), `World.SpawnEntity`. Some entities use their position or properties in `OnSpawn`: `PingPongPlatform` and `RedFlyingKoopa` set up their movement path from `Position` + `TileOffset`, and `Firebar` rebuilds its children.

All properties are applied, including ones at their default. That keeps creation deterministic and is harmless: setting a Goomba's palette to `Overworld` is a no-op visually.

### The registered entities

| Key | Type | Properties (key: type = default) |
|-----|------|-----------------------------------|
| `goomba` | `Goomba` | `palette`: `Palette` = Overworld |
| `koopa` | `Koopa` | `palette`: `Palette` = Overworld |
| `red_koopa` | `RedKoopa` | – |
| `red_paratroopa` | `RedFlyingKoopa` | `tile_offset`: float = 0 (fly distance in tiles) |
| `bowser` | `Bowser` | – |
| `firebar` | `Firebar` | `length`: `FirebarLength` = Short, `clockwise`: bool = false (`IsGoingRight`) |
| `platform_constant` | `ConstantPlatform` | `size`: `PlatformType` = Long, `direction`: `PlatformDirection` = Up |
| `platform_pingpong` | `PingPongPlatform` | `size` = Long, `tile_offset`: float = 0, `horizontal`: bool = false |
| `axe` | `Axe` | – |
| `toad` | `Toad` | – |
| `warp_pipe` | `WarpPipe` | `dest`: path = "", `tag`: string = "", `dir`: `PipeDirection` = Down, `return_to_parent`: bool = false |
| `warp_exit` | `WarpExit` | `tag`: string = "" |
| `midway` | `Midway` | – (unique) |
| `no_plant` | `NoPlant` | – |
| `flagpole` | `Flagpole` | – |
| `castle_entrance` | `CastleEntrance` | – |

Not in the catalog on purpose: `Player` (its start position is level metadata), `PiranhaPlant` (spawned at runtime on top of pipe tiles), `FirebarChild` (spawned by its `Firebar`), items, effects, score text.

### `PropertyDef` and codecs

`PropertyDef` is the untyped base the editor and file format work with. Values always travel as **strings**: that's what files store, and what text boxes hold.

| Member | Purpose |
|--------|---------|
| `Key`, `Label`, `Kind` | file key, inspector label, which widget to use (`Bool`, `Int`, `Float`, `String`, `Path`, `Enum`) |
| `DefaultText` | the default, already formatted |
| `Choices` | enum names (empty otherwise) |
| `TryNormalize(text, out normalized)` | validate and canonicalize (e.g. `"3.50"` → `"3.5"`, `"TRUE"` → `"true"`) |
| `Apply(entity, text)` | parse and call the setter. Unparseable text falls back to the default. |
| `IsEnabled` | optional predicate over the placement's props. The editor greys the field out. `warp_pipe.dest` is disabled while `return_to_parent` is true. |

`PropertyDef<TEntity, TValue>` is the typed implementation. It holds a `ValueCodec<TValue>` (parse + format + choices) from `ValueCodecs`:

| Codec | Parse | Format |
|-------|-------|--------|
| `Bool` | `bool.TryParse` (case-insensitive) | `"true"` / `"false"` |
| `Int` | invariant culture integer | invariant |
| `Float` | invariant culture, must be finite | `"0.###"` (at most 3 decimals) |
| `String` | anything | as is |
| `Enum<T>` | name, case-insensitive, must be a defined value | `ToString()` (the name) |

Enums are stored **by name**, so reordering an enum never breaks levels. Renaming a member does.

## 2.5 `LevelData`: the level itself (`LevelData.cs`)

```csharp
public sealed record LevelMeta(int Width, WorldBackground Background, MusicName Music,
    int Time, bool PipeIntro, int SpawnX, int SpawnY);

public sealed record EntityPlacement(int Id, string Key, int X, int Y,
    ImmutableSortedDictionary<string, string> Props);
```

- **`LevelMeta`**: level-wide settings. `SpawnX/SpawnY` is where the player's **feet** start, in texture pixels. `LevelBuilder` turns it into `World.PlayerSpawnPosition` with `Player.SetFeetPos`. `LevelMeta.Default` = 100 wide, Sky, Overworld music, 400 time, no pipe intro, spawn (40, 192), the bottom-centre of tile (2, 11).
- **`EntityPlacement`**: one entity. `X/Y` is the entity's **feet**: the bottom-centre of its box (`Entity.Feet`), in texture pixels. Feet don't depend on the entity's `Size`/`SizeOffset`, so the numbers read naturally: a 16 px enemy standing on the ground at the top of row 12, in column 20, is at `(20·16+8, 12·16)` = `(328, 192)`, whatever its sprite or hitbox. `Cell` gives the tile it stands in, `(floor(X/16), floor((Y−1)/16))`. Placements are immutable. Edits create a modified copy (`WithProp`, `MovedTo`, `with { ... }`) and swap it in with `ReplaceEntity`.
- **`Id`** is assigned by `AddEntity` from a per-level counter (`_nextEntityId`). It's only meaningful within one `LevelData` and its clones. It's **not saved**, and loading assigns fresh ids. The editor uses ids so selections survive undo.

### The grid

- `TileSpec[Height, Width]` with `Height` = `World.Rows` = **14** (fixed; the game assumes it in many places).
- `Get(col, row)` returns `Air` outside the level (never throws). `Set` returns whether anything changed.
- `InBounds(col, row)`.

### Width rules

- `MinWidth` = 16 (one screen; the camera clamp needs at least that), `MaxWidth` = 4096.
- Setting `Meta` with a new `Width` resizes the grid. The left part is kept and new columns are air. It's **clamped** to `[16, 4096]` both in the constructor and in the `Meta` setter.
- Entities are **not** removed when shrinking via `Meta`. They just end up outside. Use `DeleteColumns` to remove content.

### Column operations

- `InsertColumns(at, count)`: shifts cells right, shifts entities with `X >= at*16` right by `count*16`, and moves the spawn too if it's at or right of the insertion point.
- `DeleteColumns(at, count)`: removes cells, **deletes entities whose `X` falls inside the removed columns**, shifts entities right of them left, and moves the spawn (into the gap's start if it was inside the deleted range). Never shrinks below 16 columns.

### `Clone()`

Copies the grid array (references to immutable specs, so it's cheap), the entity list (references to immutable placements) and the id counter. A 211-column level is about 3,000 references. That makes snapshot-based undo practical (chapter 4).

## 2.6 Building a playable world (`LevelBuilder.cs`)

`LevelBuilder.Build(level, sourcePath, warnings)`:

1. `new World { LoadedPath = sourcePath }`. `LoadedPath` is the level's identity for pipes (see 2.8).
2. Meta: `SetCols`, `SetBackground`, `SetMusic`, `IsPipeIntro`, `TimeLeft`, `PlayerSpawnPosition = spawn * 4`, and the player is placed there.
3. Tiles: for every cell, `AutoTiles.ResolveAt` → `CreateTile()` → `World.SetTile`. Air is skipped (the world starts all air).
4. Entities, in list order: `LevelBuilder.Spawn` → `def.Create(world, placement)` → `world.SpawnEntity`. Unknown keys are skipped with a warning.

`Spawn` is also used by the editor's preview and ghost worlds, so editor visuals use exactly the same construction path as the game.

## 2.7 Loading files (`LevelLoader.cs`)

| Method | Accepts | Used by |
|--------|---------|---------|
| `Load(path, out error)` → `World?` | **v2 only** (a legacy file returns null with a "migrate this level" message) | campaign (`SessionManager`), `TitleScene` backdrop, `GameScene` sublevels |
| `ReadData(path, out wasLegacy, warnings)` → `LevelData` | v2 **and** legacy | the editor's Open (legacy is converted in memory) |
| `MigrateFolder(folder, backup)` | converts every legacy `.lvl` in a folder in place | `--migrate-levels` |

`Load` catches I/O and format errors, prints them, and returns `null`. All callers already handled a null world. Warnings (unknown records, invalid values) are printed to the console and the level still loads.

## 2.8 Level identity (`LevelPaths.cs`)

Several things compare "is this the same level?": a warp pipe leading back into its own level, `GameScene`'s cache of loaded sublevels, and "return to parent". Comparing raw strings fails (`levels/02.lvl` vs `/abs/path/levels/02.lvl`), and an unsaved editor level has no path at all.

- `LevelPaths.Key(path)` → `Path.GetFullPath(path)`, or the sentinel **`"unsaved://level"`** for an empty path (or the sentinel itself). `GetFullPath("")` would throw, and `<`/`>` in a fake path would throw on Windows. That's why it's a URL-like sentinel.
- `LevelPaths.Same(a, b)` compares keys.

Level-relative paths like `levels/bonus01.lvl` resolve against the game folder, because `Program` sets the current directory to `AppContext.BaseDirectory` at startup.

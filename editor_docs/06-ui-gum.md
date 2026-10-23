# 6. The UI (Gum)

The editor's interface is built **in code** (no Gum project file) with [Gum](https://github.com/vchelaru/Gum) for raylib (`Gum.raylib`) and its editor theme (`Gum.Themes.Editor.Raylib`). All UI code is in `Source/Editor/UI`.

## 6.1 Gum primer (just what this project uses)

### Two layers: visuals and controls

- **Visuals** (`GraphicalUiElement`, "GUE") form a retained tree of positioned rectangles. Runtime types used here:
  - `ContainerRuntime`: a box that holds and lays out children;
  - `RectangleRuntime`: a filled or outlined rectangle (`IsFilled`, `FillColor`, `StrokeColor`, `StrokeWidth`);
  - `SpriteRuntime`: a texture or part of one (`Texture`, `TextureAddress = Custom` + `TextureLeft/Top/Width/Height`);
  - `TextRuntime`: text.
- **Controls** (Forms, `FrameworkElement`) are behaviour wrappers around a visual (`.Visual`): `Button`, `ToggleButton`, `CheckBox`, `ComboBox`, `TextBox`, `Label`, `StackPanel`, `ScrollViewer`, `Expander`, `Menu`/`MenuItem`. The theme decides what their visuals look like.

Both can be children of either, through `AddChild`.

### Layout

| Property | Values used here |
|----------|------------------|
| `X`, `Y` | offsets in pixels |
| `Width`/`Height` + `WidthUnits`/`HeightUnits` | `Absolute` (pixels). `RelativeToParent`: **0 = same as the parent**, −16 = 16 px smaller. `Ratio`: take a share of the space a stacking parent has left. `RelativeToChildren`: size to content plus the value as padding. |
| `ChildrenLayout` | `Regular` (children positioned freely), `TopToBottomStack`, `LeftToRightStack`, plus `WrapsChildren` and `StackSpacing` |
| `Dock(...)`, `Anchor(...)` | helpers that set the position and size units for common cases (`Dock.Fill`, `Anchor.Right`…) |

### Roots, update and draw

- `GumService.Default.Root` is the main tree. `PopupRoot` holds menus and dropdown lists, `ModalRoot` holds modal dialogs. Children of `ModalRoot` block input to everything else.
- `GumService.Default.Update(time)` reads raylib's mouse and keyboard, updates hover, focus and pushed state, and **raises events** (`Click`, `SelectionChanged`, `LostFocus`…). Our event handlers run *inside* this call.
- `GumService.Default.Draw()` renders all roots.

### Input concepts

- **`Cursor.VisualOver`**: the topmost *interactive* element under the mouse. A container only counts as interactive if `HasEvents` is true. **All of our layout containers set `HasEvents = false`** (`UiKit.Container`), so `VisualOver` is `null` over empty panel space and over the viewport. That's how the editor knows the mouse belongs to the level and not the UI.
- **`InteractiveGue.CurrentInputReceiver`**: the control with **keyboard focus** (a focused `TextBox`, `ComboBox`, `Button`…), or `null`. The editor reads it to decide whether keys belong to the UI (chapter 7) and sets it to `null` to take focus away (clicking the level, pressing a toolbar button, closing a menu).

### Lifecycle in this project (`GumHost` in `UiKit.cs`)

- `GumHost.EnsureInitialized()`: the first time the editor opens, `Initialize()`, then `Theme.ConfigureGum(EditorTheme.Apply)` (our colours, the theme, then the rest of our colours; see 6.6), then `EnableExpandToWindow()` (the canvas follows the window size 1:1). Gum then lives until the game exits. It's only updated and drawn while the editor scene runs.
- Each editor session builds a fresh `EditorUi` and adds it to `Root`. `EditorUi.Dispose()` (on leaving the editor) closes dialogs, clears focus, removes our root and clears `PopupRoot`.

## 6.2 Layout tree (`EditorUi.cs`)

```
GumService.Root
└── _root  (TopToBottomStack, Dock.Fill, HasEvents=false)
    ├── MenuBar.Menu                       File · Edit · View · Level · Help
    ├── Toolbar.Root  (40 px tall)         tool toggles │ Grid │ Snap │ Play buttons
    ├── middle  (LeftToRightStack, height = Ratio 1 → everything left)
    │   ├── Palette.Root      (286 px wide)
    │   ├── _viewportHost     (width = Ratio 1, HasEvents=false)  ← empty; the level is drawn here by raylib
    │   └── Inspector.Root    (340 px wide)
    └── StatusBar.Root  (26 px tall)
ModalRoot
└── Dialogs overlay (only while a dialog is open)
```

**The level view isn't a Gum element.** `_viewportHost` is an empty, non-interactive container whose only job is to *reserve* the space. Every frame `EditorUi.ViewportArea` reads its absolute rectangle, and `EditorViewport` draws the level into exactly that rectangle with raylib (scissored) *before* Gum draws the panels on top. That's why the viewport resizes with the window and never fights the UI for input.

`EditorUi` also exposes:
- `IsMouseOverUi` → `VisualOver != null || MenuBar.IsOpen`;
- `IsTextInputFocused` (static) → `CurrentInputReceiver is TextBoxBase`;
- `Update()` (input work right after Gum's update: dialog keys, the atlas drag, text field Enter/Escape);
- `Refresh()` (pull editor state into every view).

## 6.3 The refresh model

The UI is retained (Gum keeps the controls), but it's **driven like immediate-mode UI**. Every frame, `EditorUi.Refresh()` asks every view to read the current editor state and update its controls: which tool button is lit, menu item labels and enabled state, the palette highlight, every inspector value, the status text.

This means there's **no event wiring from the model to the UI**. Undo, a drag in the viewport, or a property change from anywhere shows up in the UI on the next frame automatically. To keep it cheap, every refresh writes a property **only when the value actually differs** (`UiKit.SetText`, `SetColor`, and the `if (x != y)` checks throughout). Unconditional writes would trigger Gum layout work every frame.

UI → model goes the other way through plain event handlers that call `LevelEditor` methods (`editor.SetTool(...)`, `editor.SetEntityProperty(...)`).

## 6.4 The views

### `MenuBarView`
- `Top(header)` creates a top-level menu (headers are padded with spaces so they don't touch).
- `Item(parent, label, shortcut, action, isEnabled)`: a leaf item. Clicking it closes all menus (`CloseAll`) and then runs the action. Each item also registers a **refresher** that recomputes its header (e.g. "Undo Paint Brick") and enabled state every frame. `label` can be a `Func<string>` for dynamic text.
- `Check(...)`: an item that shows `[x]`/`[  ]` and toggles a bool (the View menu).
- **Two columns.** Every row of a popup is built by `Row(parent, label, right)`: the header holds only the label, and the shortcut is a separate muted label anchored to the row's right edge. Gum sizes each menu item to its own text, so `AlignColumns` (run every refresh) gives all rows of a popup one width: widest label + gap + widest shortcut, using `UiKit.TextWidth`'s estimate. The shortcuts end up right-aligned in one column, and a row's highlight spans the whole popup. The width follows labels that change, such as "Undo Paint Brick".
- `Submenu(parent, header)`: an item with its own list. Gum draws the `>` arrow right after the text, so `Submenu` moves it to the right-hand column. Gum opens the list **on click**, not on hover. There are two submenus:
  - **View → Palette** lists the categories with their `Cmd/Ctrl+1–8` shortcuts.
  - **File → Open Recent** lists `EditorPrefs.RecentFiles` as "file name (folder)", greys out files that no longer exist, and ends with "Clear Recent" (or shows "No recent files"). `RefreshRecent` rebuilds it only when `EditorPrefs.RecentVersion` changes. Its items aren't in the per-frame refresher list, because they come and go. Clicking one calls `LevelEditor.OpenRecent`, which asks to save unsaved changes first.
- `IsOpen`: any menu popup visible. It's used to block the viewport and to make Escape close menus first.
- `ShowControls(editor)`: the F1 / Help → Controls dialog.

### `ToolbarView`
Three groups, separated by thin vertical lines:
- **Tools:** one `ToggleButton` per tool, showing the tool's `Icon` and `Name`. The shortcut is in the **tooltip** ("Select (V)"), which keeps the bar narrow enough for the default 1024 px window. The theme doesn't visually distinguish a checked toggle, so `SetActive` tints the active button's `ToggleButtonVisual.BackgroundColor` Mario blue (`Theme.Selection`). The original colour is captured from the first button.
- **Grid** toggle, then a magnet icon and the **Snap** combo box → `EditorPrefs.Snap`.
- **Play (F5)** and **Play Here** (from the cursor) with green ▶ icons and tooltips that name their shortcuts.
- `_updating` guards against feedback loops. Setting `SelectedIndex` during refresh raises `SelectionChanged`, which must not be treated as user input.

### `PaletteView`
- A category `ComboBox` → `Show(category)` clears the scroll viewer and builds one **cell** per item: a 44×44 interactive container with a background rectangle, the icon sprite (integer-scaled when it fits, for crisp pixel art), and an optional badge sprite in the corner.
  - `Click` → `editor.SetBrush(item.CreateBrush())`;
  - `RollOn`/`RollOff` track the hovered item.
- `Refresh()` colours each cell's background: the selection colour if it's the current brush's `Source`, a faint highlight if hovered, transparent otherwise. The labels under the grid show the hovered item's name, or the current brush's.
- **Tileset view:** instead of cells, the whole atlas as one sprite at 2× inside a single interactive container, plus two outline rectangles (hover and selection). Gum has no drag-select event for this, so `Update()` **polls the cursor**:
  - `PrimaryPush` while `VisualOver` is the atlas → remember the start cell;
  - while held, the selection outline follows `TileRect.FromCorners(start, current)`;
  - on release → `editor.SetBrush(PaletteCatalog.TilesetBrush(rect))`.

### `InspectorView` and its sections

Three collapsible `Expander` sections inside a `ScrollViewer`: **Selection**, **Brush**, **Level**.

Each section is a `Section` (a private class) whose content is rebuilt **only when its key changes**:

```csharp
(string key, Action<SectionBuilder> build) = DescribeSelection();  // e.g. ("entity:12:goomba", BuildEntity...)
_selection.Rebuild(key, build);   // no-op if the key is the same as last frame
```

Keys encode *what kind* of thing is shown, not its values. Examples:

| Selection | Key |
|-----------|-----|
| Nothing | `none` |
| Player start | `spawn` |
| One entity | `entity:<id>:<key>` |
| Several objects | `multi:<count>` |
| A region | `region:<TileRect>` |
| A tile | `tile:<col>,<row>:<SpecType>` |

Brush keys look like `tile:<name>:<SpecType>`, `entity:<name>:<key>` and `stamp:<name>`.

So selecting another Goomba rebuilds the fields, but changing the selected Goomba's palette doesn't. The existing field just shows the new value on the next refresh. Including the spec type in tile keys means turning a `?` block into a brick (via undo, say) rebuilds the section with brick fields.

`SectionBuilder` builds the content:
- `Text`, `Check`, `Choice`, `Path`, `Info` add a labelled row to the current `PropertyGridVisual` (the theme's two-column label/control grid);
- `Heading`, `Note`, `Buttons` add full-width content and start a new grid for any rows after them;
- `Heading(Func<string>)` is live (e.g. "? Block (Coin)" → "? Block (Star)" when you change the item).

**Entity fields are generated from the schema** (`AddProperties`): `Bool` → checkbox, `Enum` → combo of `Choices`, `Path` → text box + "..." file picker, everything else → text box. Each field's enabled state comes from `PropertyDef.IsEnabled`.

What each section shows, top to bottom (the level rarely changes, the selection all the time):
- **Level:** width, background, music (only the world themes, `None` through `UndergroundIntro`), time (1–999), pipe intro, player start, file name.
- **Brush:** the same kinds of fields as Selection, but bound to the current brush. Editing them changes what you'll paint next (e.g. make the ? block brush contain a Star).
- **Selection** (hidden while nothing is selected): an entity's name, feet X/Y and schema properties with Duplicate/Delete. The player start's feet X/Y. A tile's name and cell, plus:
  - ? blocks: Item and Hidden;
  - bricks: Item, Hidden and Palette;
  - plain tiles: the tileset id and a **Solid** toggle;
  - autotiles: a **Detach** button that freezes the current resolved look into a plain tile.

  An area shows its size, entity count, Copy/Cut/Clear Tiles. Several objects show Copy/Duplicate/Delete.

The order is simply the order the sections are created in `InspectorView`'s constructor.

### Fields: how controls bind to data (`Fields.cs`)

Every field is a **getter/setter pair** around one control:

| Field | Control | Commits when | Notes |
|-------|---------|--------------|-------|
| `TextField` | `TextBox` | **Enter** or **focus lost** | Escape reverts. If the setter returns `false` (invalid input), the text snaps back to the real value. |
| `CheckField` | `CheckBox` | immediately (`Checked`/`Unchecked`) | |
| `ChoiceField` | `ComboBox` | immediately (`SelectionChanged`) | matches values case-insensitively |
| `PathField` | `TextBox` + "..." `Button` | like `TextField`; the button opens a file dialog | stores paths relative to the game folder when possible (`levels/bonus01.lvl`) |
| `InfoField` | `Label` | read-only | |

`Field.Refresh()` (every frame) updates the enabled state, then **pulls** the value from the getter. A `TextField` skips the pull while it has focus, so it never overwrites what you're typing. A `ChoiceField` skips it while its dropdown is open. Programmatic updates are wrapped in an `_updating` guard so they don't fire the setter.

Setters always go through `LevelEditor` methods, which validate and make one undo step each (`SetEntityProperty`, `SetLevelMeta`, `SetTileSpec`, `SetBrushProperty`…).

### `StatusBarView`
The bar is a Mario-blue strip (`Theme.StatusBar`), so it doesn't blend into the panels above it.
- **Left:** the latest `editor.Notify(...)` message for 4 seconds ("Saved 04.lvl.", "Undid Move."), otherwise the current tool's hint (in `Theme.StatusBarMuted`, a light blue that stays readable on the strip).
- **Right:** what's under the cursor (entity name, "Player Start", or the tile's name), the tile and pixel coordinates, and the zoom. This label spans the whole bar with right-aligned text rather than sizing to its content (see 6.5 for why).

### `Dialogs`
- `Show(title, message, params (string Label, Action? Action)[] buttons)` builds a full-window dimmer (an interactive container, so it swallows clicks) with a centred panel inside, and adds it to **`ModalRoot`**.
- Clicking a button closes the dialog and then runs its action (`null` = just close).
- The panel has a blue stripe along its top, and the first button (the one Enter presses) is filled blue (`UiKit.MakePrimary`).
- **Enter** presses the first button and **Escape** the last. For "Save / Don't Save / Cancel", Escape means Cancel. Keys are ignored during the frame the dialog opened, so the key that opened it can't also answer it.
- While a dialog is open the editor ignores shortcuts and viewport input. The rest of the UI is blocked by Gum's modal handling.

## 6.5 Gum workarounds (read before changing UI code)

| Problem | Workaround | Where |
|---------|-----------|-------|
| `PopupRoot` and `ModalRoot` are never sized (stay 0×0). Gum clamps popups "into the screen" using those roots' bounds, so **every menu and dropdown appeared in the top-left corner**. Dialogs docked to `ModalRoot` would have been 0×0. | Every frame, before `GumService.Update`, size both roots to the canvas | `GumHost.SyncOverlayRoots` |
| The themed `Button` treats `Width` as padding around its text unless the units are absolute, so buttons came out far too wide (and `RelativeToChildren` collapsed toggle buttons) | Force `WidthUnits = Absolute` and estimate the width from the text length (7.6 px per character + 22 px) | `UiKit.Button`, `UiKit.Toggle`, `UiKit.WidthFor` |
| A checked `ToggleButton` looks identical to an unchecked one in this theme | Tint `ToggleButtonVisual.BackgroundColor` | `ToolbarView.SetActive` |
| `Cursor.WindowOver` and `ColoredRectangleRuntime` are deprecated in 2026.10 | Use `VisualOver` and `RectangleRuntime` with `FillColor` | everywhere |
| `Label` has no colour or size property | Cast `Label.TextComponent` to `TextRuntime` | `UiKit.Label`, `UiKit.SetColor` |
| `Items` on `ComboBox`/`MenuItem` is annotated nullable but is always set | `Items!.Add(...)` | views |
| A **right-anchored label that auto-sizes to its text** is positioned from its *previous* width on the frame its text changes. The status bar's info text jumped to the middle whenever the cursor moved. | Give such labels a fixed box and let the text right-align inside it (`Anchor(Anchor.Right)` already sets the alignment; `UiKit.FillWidth(label, margin)` fixes the box). Left-anchored labels aren't affected. | `StatusBarView` |
| The themed `Expander` hard-codes its header background to grey (50, 50, 50), and shows a ►/▼ arrow | `Section.StyleHeader` finds the header's parts by name (`HeaderContainer`, `HeaderContent`, `ArrowIndicator`, `TextInstance`), recolours the background, hides the arrow, and adds the stripe and icon | `InspectorView.Section` |
| Menu items size to their own text, so shortcuts can't line up | Fixed, shared row widths per popup and right-anchored shortcut labels (see `MenuBarView` above) | `MenuBarView.Row`, `AlignColumns` |
| `PropertyGridVisual` stripes its rows in fixed greys every time a row is added | `UiKit.StripeRows(grid)` right after each `AddRow` recolours them with `Theme.RowEven`/`RowOdd` (the grid only re-stripes on add or show/hide, and the editor never hides rows) | `InspectorView.SectionBuilder` |
| A `ToggleButton`'s text spans the whole button (the themed `Button`'s hugs its text), so an icon placed "left of the text" landed left of the button | `UiKit.AddIcon` makes the text hug its content and centres it, then hangs the icon off its left edge (`IgnoredByParentSize`) and shifts the text right by half the icon | `UiKit.AddIcon` |
| `ScrollViewer` scrolls by `(int)` of the wheel value × 30 px, so trackpad scrolling (small fractions) does nothing or jumps | The editor zeroes `MouseWheelScrollSpeed` on scroll viewers under the cursor and scrolls them itself, point for point (chapter 7.3) | `EditorUi.ScrollViewerUnderCursor`, `EditorUi.Scroll` |
| A button keeps keyboard focus after a click, so a later Enter/Space would press it again (and Enter also triggers Play) | Every `UiKit` button and toggle clears `CurrentInputReceiver` when clicked | `UiKit` |

If a future Gum version fixes the overlay root sizing, `SyncOverlayRoots` becomes a harmless no-op. It only writes when the size differs.

## 6.6 Theme and icons

### Colours (`Theme.cs`)

Every colour in the editor is a named field on `Theme`. That covers the panels, text and meaning colours, plus the overlays drawn in the level view (selection, hover, grid, guides, spawn, erase). The theme is **"Mario night"**:

| | |
|---|---|
| Surfaces | blue-slate, darkest to lightest: `Outside`, `Window`, `Recessed` (inputs), `Panel`, `PanelRaised` (section headers), `Control` (buttons) |
| `Selection` | Mario blue: the active tool, the selected palette cell, list highlights, a dialog's default button |
| `Accent` | coin gold: focus, the Level section, screen guides |
| `Success` | Luigi green: Play, the Brush section |
| `Danger` / `DangerText` | Mario red: Delete, Clear Tiles, erase previews |
| `Info` | sky blue: the Selection section and selection outlines in the level |
| `StatusBar` | the blue status strip |

**How it reaches Gum.** Gum's visuals read their colours *when they're created*, from two places:
- `EditorStyling.ActiveStyle.Colors` is the editor theme's own palette. `Theme.ConfigureGum` fills it *before* `EditorTheme.Apply()`.
- `Styling.ActiveStyle.Colors` is Gum's base palette, used by toggle buttons, menus, tooltips and the focus outline. `EditorTheme.Apply()` copies only four colours into it (text, muted text, primary, accent), so `ConfigureGum` sets the rest *after* Apply. Otherwise they'd stay Gum's default teal and purple.

Changing a colour while the editor runs only affects controls created afterwards. To support several themes, make `Theme` an instance, pick one before `ConfigureGum`, and rebuild `EditorUi` after switching (see recipe 9.12).

**Colour that carries meaning:**
- Each inspector section has its own colour: the header title, a 3 px stripe on the left, and an icon. Level is gold with a flag, Brush is green with a pencil, Selection is blue with the cursor.
- Destructive buttons (Delete, Clear Tiles) have red text and the red bin icon (`UiAction.Danger`).
- The section headers have no expand arrow: Gum's `ArrowIndicator` is hidden. Clicking a header still collapses or expands its section.

### Icons (`EditorIcons.cs`, `res/editor/editor_icons.png`)

- 16×16 pixel-art icons on one sheet, 8 per row, in the order of the `EditorIcon` enum: Select, Brush, Rectangle, Erase (grey bin), Pick, Grid, Snap, Play, PlayHere, Level, Copy, Cut, Duplicate, Delete (red bin), Detach. `Assets` loads every PNG under `res/` by file name, so the sheet is `Assets.GetTex("editor_icons")`.
- `EditorIcons.Sprite(icon)` returns a 16×16 `SpriteRuntime` showing one icon at native size, crisp because textures use point filtering.
- `UiKit.Button(text, action, icon)` / `UiKit.Toggle(text, action, icon)` put the icon to the left of the label and widen the button by 21 px (16 px icon + 5 px gap). Inspector buttons are described by `UiAction(label, action, icon, danger)` records.
- Tools have an `Icon` property, which the toolbar uses.
- Menu items have no icons: Gum's `MenuItem` visual would have to be rebuilt to hold one.


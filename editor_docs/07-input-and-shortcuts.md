# 7. Input & shortcuts

Three things want the mouse and keyboard every frame: **Gum** (panels, text boxes, menus), the **shortcut handler** (`LevelEditor.Shortcuts.cs`) and the **viewport** (camera and tools). This chapter is the rulebook for who wins.

## 7.1 The decisions made each frame

In `LevelEditor.Update`, in this order:

1. **Before Gum runs**, remember:
   - `typing`: a `TextBox` has keyboard focus (`EditorUi.IsTextInputFocused`).
   - `modal`: a dialog is open.

   Then **route scrolling** (`RouteScrolling`, see 7.3). Panels are scrolled here, before Gum gets a chance to scroll them its own way.
2. Gum updates. Clicks, typing and menu events are handled here. A text field might commit on Enter and lose focus, and a dialog might be answered and close.
3. `modal |= dialog is open now`.
4. **Shortcuts** run only if there's no modal and nobody was typing, either before or after Gum ran. Checking both moments is what stops one Enter from both committing a text field (or answering a dialog) *and* starting a playtest.
5. If modal → any viewport gesture is cancelled.
6. **Viewport** input runs with:
   - `mouseBlocked = modal || VisualOver != null || a menu is open`. The mouse is over real UI, so the level shouldn't react.
   - `keyboardFree = !modal && !typing`. Space+drag panning must not trigger while you type a space.

## 7.2 Mouse ownership

- The viewport reacts only if the mouse is **inside the viewport rectangle and not blocked** (`IsHovered`).
- Once the viewport **captures** a button (a tool press or a pan), it keeps it until that button is released, even if the mouse travels over the UI or out of the window.
- Pressing in the viewport **clears UI keyboard focus** (`CurrentInputReceiver = null`). Clicking the level after typing in the inspector commits the field (it loses focus) and gives the keyboard back to the shortcuts.
- Scrolling and pinching follow their own routing rules, covered next.

## 7.3 Scrolling, pinching and the touchpad

The editor is meant to be comfortable on a laptop touchpad, so scrolling means different things depending on the device:

| Input | Over the level | Over the palette / inspector / a dropdown |
|---|---|---|
| Two-finger scroll (trackpad, Magic Mouse) | **pan**, 1:1, following your fingers (momentum included) | smooth scroll, 1:1 |
| Pinch | **zoom** around the cursor | ignored |
| Two-finger double tap (macOS smart zoom) | fit the level's height ↔ twice that, around the cursor | ignored |
| `Cmd/Ctrl` + any scroll | zoom around the cursor (Windows sends a pinch this way) | scroll |
| Mouse wheel | zoom around the cursor (`1.15^clicks`) | 30 px per click (Gum's speed) |
| `Shift` + mouse wheel, or a sideways wheel | pan sideways, 64 px per click | scroll sideways |

**Where the numbers come from.** `ScrollInput.Read()` produces one `ScrollInput` per frame:
- `Points`: smooth scrolling in screen points, in the direction the content should move.
- `Notches`: mouse-wheel clicks.
- `Magnify`: the pinch factor.
- `SmartMagnify`: the double tap.

How it gets them depends on the platform:
- **macOS:** `MacTrackpad` hooks GLFW's window view through the Objective-C runtime (see [gotchas](10-gotchas.md)). It reads every scroll event's `hasPreciseScrollingDeltas` and exact deltas, and receives the pinch events GLFW would drop. raylib still gets every scroll event afterwards.
- **Elsewhere** (or if the hook can't install): the frame's raylib wheel value is used. Fractional values count as smooth scrolling, whole values as wheel clicks. A whole value that arrives within a smooth swipe counts as smooth too. This path is untested on Windows and Linux.

**Who gets the scroll.** `ScrollLatch` gives the whole gesture to whatever was under the cursor when it started: a panel's `ScrollViewer`, the level, or nothing (the toolbar, say). A gesture lasts while events keep coming less than `GestureGap` (0.3 s) apart. Without this, momentum from flicking the palette would start panning the level the moment the pointer drifted onto it. Pinch gestures don't latch: they apply when the level is hovered.

**Why the editor scrolls Gum's panels itself.** Gum turns the wheel into whole clicks (`(int)` of raylib's value) and moves 30 px per click. A trackpad reports small fractions, so slow swipes did nothing and fast ones jumped. `EditorUi.ScrollViewerUnderCursor()` sets `MouseWheelScrollSpeed = 0` on every `ScrollViewer` under the cursor, which switches Gum's own handling off. `EditorUi.Scroll` then moves the innermost one by whole points and carries the leftover fraction into the next frame, so text stays crisp and slow swipes still move. This has to happen *before* Gum's update, so it uses the previous frame's `VisualOver`. Dropdown lists are `ListBox : ItemsControl : ScrollViewer`, so they're covered too, from the first time they're scrolled.

Gestures made during a playtest are thrown away on return (`MacTrackpad.Discard()` in `OnReturnFromPlaytest`), so the camera stays where you left it.

## 7.4 Keyboard focus rules

| Situation | What happens to keys |
|-----------|----------------------|
| A **text box** is focused | No editor shortcuts at all. Enter commits, Escape reverts (`TextField.Update`). Everything else is typing. |
| Another control is focused (combo box, button…) | Enter does **not** start a playtest (F5 still does), and Delete/Backspace and the arrow keys are left to the control. Tool keys, G, Home, F1, Escape and every Cmd/Ctrl shortcut still work. |
| A **dialog** is open | No editor shortcuts. Enter = first button, Escape = last button (`Dialogs.Update`). |
| A viewport drag is in progress (`Viewport.IsBusy`) | Tool switching, G, Home, Delete and arrows are ignored until release. Cmd/Ctrl shortcuts, F5, Escape (cancels the drag) and F1 still work. |

**The command modifier** (`Shortcuts.IsModDown`) is Ctrl *or* Cmd (raylib's "Super" key) on every platform. Menus display "Cmd" on macOS and "Ctrl" elsewhere (`Shortcuts.Mod`).

## 7.5 All shortcuts

| Keys | Action | Notes |
|------|--------|-------|
| `V` `B` `R` `E` `I` | Select / Brush / Rectangle / Erase / Pick tool | from `Tool.Shortcut` |
| `F5`, `Enter` | Playtest | Enter only when no control has focus |
| `Shift+F5`, `Shift+Enter` | Playtest from the cursor | needs the mouse over the level |
| `Esc` | the **Escape cascade** (below) | |
| `F1` | Controls dialog | |
| `G` | toggle grid | |
| `Home`, `Cmd/Ctrl+0` | frame the player start | |
| `Cmd/Ctrl+1` … `8` | palette category: Terrain, Blocks, Structures, Enemies, Platforms, Objects, Level Flow, Tileset | same as View → Palette |
| `Cmd/Ctrl+=`, `Cmd/Ctrl+-` (or keypad `+`/`-`) | zoom in / out around the middle of the view, ×1.25 per step | auto-repeat |
| `Del`, `Backspace` | delete the selection | see chapter 5 for what gets deleted |
| Arrows | nudge the selection by the snap size (`Shift` = 16 px) with key repeat, or pan the view if nothing is selected (`Shift` = 3× faster) | |
| `Cmd/Ctrl+Z`, `Cmd/Ctrl+Shift+Z`, `Cmd/Ctrl+Y` | undo / redo / redo | Z and Y auto-repeat |
| `Cmd/Ctrl+C`, `X`, `V` | copy / cut / paste | paste turns the clipboard into the brush |
| `Cmd/Ctrl+D` | duplicate selected entities one tile to the right | unique entities are skipped |
| `Cmd/Ctrl+A` | select all entities | |
| `Cmd/Ctrl+N`, `O`, `S`, `Shift+S` | new / open / save / save as | |
| Two-finger scroll, middle drag, `Space`+left drag | pan | |
| Pinch, `Cmd/Ctrl`+scroll, mouse wheel | zoom around the cursor | see 7.3 |
| `Shift`+wheel | pan sideways | |
| `Alt`+click (Brush tool) | pick | |
| `Alt` while dragging a selection | move in 1 px steps | |
| `Shift`+click (Select tool) | add to / remove from the selection | |

## 7.6 The Escape cascade (`HandleEscape`)

Escape always does the *least destructive* thing still available:

1. A menu is open → close it.
2. A drag is in progress → cancel it (and undo what it did).
3. A UI control has focus → drop focus.
4. Something is selected → deselect.
5. The brush is a pasted clipboard (paste mode) → back to the default brush.
6. Otherwise → **exit to the title screen**, asking to save first if there are unsaved changes.

## 7.7 A note on raylib key polling

Shortcuts use `Raylib.IsKeyPressed`, which compares key state between frames. A real keypress spans many frames, so this is reliable. *Synthetic* taps (UI automation tools that send key-down and key-up instantly) can be missed when both land in the same frame. If you ever script the editor, hold keys for at least ~50 ms. Text input isn't affected: Gum reads characters from raylib's character queue.

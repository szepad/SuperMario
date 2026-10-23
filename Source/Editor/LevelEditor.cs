using Gum;
using Gum.Forms.Controls;

namespace Mario2.Editor
{
    /// <summary>
    /// The level editor: owns the document, selection, tools and UI, and runs inside
    /// <see cref="EditorScene"/>. Split into partial files by concern (edits, files, shortcuts).
    /// </summary>
    public sealed partial class LevelEditor
    {
        private const float StatusDuration = 4f;

        public Scene Scene { get; }

        public EditorDocument Document { get; private set; }
        public EditorSelection Selection { get; } = new();
        public EditorPreview Preview { get; } = new();
        public EditorCamera Camera { get; } = new();
        public EditorViewport Viewport { get; }
        public GhostWorld Ghost { get; } = new();
        public EditorUi Ui { get; private set; } = null!;

        public SelectTool SelectTool { get; }
        public BrushTool BrushTool { get; }
        public RectTool RectTool { get; }
        public EraseTool EraseTool { get; }
        public PickTool PickTool { get; }
        public IReadOnlyList<Tool> Tools { get; }
        public Tool Tool { get; private set; }

        public Brush Brush { get; private set; }
        public Stamp? Clipboard { get; private set; }

        public string StatusMessage { get; private set; } = string.Empty;

        private float _statusTimer;
        private bool _frameOnNextUpdate = true;
        private bool _exitRequested;
        private string _windowTitle = string.Empty;
        private readonly ScrollLatch _scrollLatch = new();

        public LevelEditor(Scene scene)
        {
            Scene = scene;
            Document = new EditorDocument(new LevelData(), string.Empty);
            Viewport = new EditorViewport(this);

            SelectTool = new SelectTool(this);
            BrushTool = new BrushTool(this);
            RectTool = new RectTool(this);
            EraseTool = new EraseTool(this);
            PickTool = new PickTool(this);
            Tools = [SelectTool, BrushTool, RectTool, EraseTool, PickTool];
            Tool = BrushTool;

            Brush = PaletteCatalog.Default.CreateBrush();
        }

        public void Load()
        {
            GumHost.EnsureInitialized();
            Ui = new EditorUi(this);

            MacTrackpad.Install();
            MacTrackpad.Discard();

            string recent = EditorPrefs.RecentlyOpened;
            if (recent == string.Empty || !File.Exists(recent) || !OpenPath(recent, quiet: true))
                LoadDocument(new LevelData(), string.Empty);
        }

        public void Unload()
        {
            Viewport.CancelGesture();
            Document.Commit();
            Ui.Dispose();
            Raylib.SetWindowTitle(Game.Title);
        }

        public void OnReturnFromPlaytest()
        {
            Raylib.ShowCursor();
            SoundManager.PlayMusic(MusicName.None);
            _windowTitle = string.Empty;

            // Scrolling and pinching during the playtest shouldn't move the editor's camera
            MacTrackpad.Discard();
            _scrollLatch.Reset();
        }

        public void Update()
        {
            // Read before Gum processes input: a text field committed with Enter this frame
            // must not also trigger the Enter shortcut
            bool typing = EditorUi.IsTextInputFocused;

            // Same for a dialog answered with Enter or Escape this frame
            bool modal = Ui.Dialogs.IsOpen;

            // Panels are scrolled before Gum's update, which would otherwise scroll them too
            ScrollInput levelScroll = RouteScrolling(modal);

            GumHost.SyncOverlayRoots();
            GumService.Default.Update(Raylib.GetTime());
            Ui.Update();

            modal |= Ui.Dialogs.IsOpen;
            if (modal)
                Viewport.CancelGesture();
            else if (!typing && !EditorUi.IsTextInputFocused)
                HandleShortcuts();

            Preview.Sync(Document.Level, Document.Version);
            Viewport.Update(Ui.ViewportArea, mouseBlocked: modal || Ui.IsMouseOverUi,
                keyboardFree: !modal && !EditorUi.IsTextInputFocused, levelScroll);
            Preview.Sync(Document.Level, Document.Version);

            if (_frameOnNextUpdate && Camera.Viewport.Width > 0)
            {
                FrameSpawn();
                _frameOnNextUpdate = false;
            }

            if (EditorPrefs.AnimateSprites)
                Preview.UpdateAnimations();

            if (_statusTimer > 0f)
            {
                _statusTimer -= Time.DeltaTime;
                if (_statusTimer <= 0f)
                    StatusMessage = string.Empty;
            }

            UpdateWindowTitle();
            Ui.Refresh();

            // Must stay last: this unloads the editor (and resets the window title)
            if (_exitRequested)
                Game.Instance.LoadScene(new TitleScene());
        }

        /// <summary>
        /// Sends this frame's scrolling to the panel or the level it started over, and returns
        /// what the level gets (pinch gestures always go there; the viewport checks hovering).
        /// </summary>
        private ScrollInput RouteScrolling(bool modal)
        {
            // Read even under a dialog, so nothing piles up for later
            ScrollInput scroll = ScrollInput.Read();
            if (modal)
                return ScrollInput.None;

            if (!scroll.HasScroll)
                return scroll;

            object? hovered = (object?)EditorUi.ScrollViewerUnderCursor() ?? (Viewport.IsHovered ? Viewport : null);
            object? target = _scrollLatch.Resolve(scrolling: true, hovered);

            if (target is ScrollViewer viewer)
                Ui.Scroll(viewer, scroll);

            return ReferenceEquals(target, Viewport) ? scroll : scroll.WithoutScroll();
        }

        public void Draw()
        {
            Raylib.BeginDrawing();
            Raylib.ClearBackground(Theme.Window);

            Viewport.Draw();
            GumService.Default.Draw();

            Raylib.EndDrawing();
        }

        public void SetTool(Tool tool)
        {
            if (tool == Tool)
                return;

            Viewport.CancelGesture();
            Tool = tool;
        }

        public void SetBrush(Brush brush)
        {
            Brush = brush;

            if (Tool == SelectTool || Tool == EraseTool || Tool == PickTool || (Tool == RectTool && !brush.PaintsTiles))
                SetTool(BrushTool);
        }

        /// <summary>Changes the current brush in place (e.g. editing its properties) without switching tools.</summary>
        public void UpdateBrush(Brush brush) => Brush = brush;

        public void Notify(string message)
        {
            StatusMessage = message;
            _statusTimer = StatusDuration;
        }

        /// <summary>Fits the level's height in the view, around the player start.</summary>
        public void FrameSpawn()
        {
            float spawnX = Document.Level.Meta.SpawnX * Game.TextureScale;
            Camera.FitHeight(spawnX);

            // Keep the start near the left of the view, with the level ahead of it
            Camera.CenterOn(new Vector2(spawnX + Camera.Viewport.Width / Camera.Zoom * 0.3f, LevelData.Height * Tile.Size / 2f));
        }

        /// <summary>
        /// Where the feet of an entity placed at the cursor go, in texture pixels. With snapping
        /// they stand on the bottom of the hovered tile (16 px: its bottom-center).
        /// </summary>
        public (int X, int Y) FeetAt(ToolInput input)
        {
            Vector2 px = input.Pixels;
            if (EditorPrefs.Snap != SnapMode.Pixel)
                px.Y = (MathF.Floor(px.Y / TilesetInfo.TileSizePx) + 1f) * TilesetInfo.TileSizePx;

            return SnapFeet(px);
        }

        /// <summary>
        /// Snaps feet (texture pixels) to the nearest spot allowed by the snap setting. 16 px:
        /// the bottom-center of a tile. 8 px: half-tile steps sideways, still on a tile's bottom.
        /// </summary>
        public static (int X, int Y) SnapFeet(Vector2 feet)
        {
            const float Px = TilesetInfo.TileSizePx;
            float tileBottom = MathF.Round(feet.Y / Px) * Px;

            return EditorPrefs.Snap switch
            {
                SnapMode.Tile => ((int)((MathF.Floor(feet.X / Px) + 0.5f) * Px), (int)tileBottom),
                SnapMode.HalfTile => ((int)(MathF.Round(feet.X / (Px / 2f)) * (Px / 2f)), (int)tileBottom),
                _ => ((int)MathF.Round(feet.X), (int)MathF.Round(feet.Y))
            };
        }

        private void UpdateWindowTitle()
        {
            string name = Document.FilePath == string.Empty ? "Untitled" : Path.GetFileName(Document.FilePath);
            string title = $"{Game.Title} - Level Editor - {name}{(Document.IsDirty ? " *" : string.Empty)}";

            if (title != _windowTitle)
            {
                _windowTitle = title;
                Raylib.SetWindowTitle(title);
            }
        }
    }
}

using Gum;
using Gum.Forms.Controls;
using Gum.GueDeriving;
using Gum.Managers;
using Gum.Wireframe;

namespace Mario2.Editor
{
    /// <summary>
    /// The editor's Gum interface: menu bar and toolbar on top, palette on the left, inspector on
    /// the right, status bar at the bottom. The space left in the middle is where the level is drawn.
    /// </summary>
    public sealed class EditorUi
    {
        private const float PaletteWidth = 286f;
        private const float InspectorWidth = 340f;

        public MenuBarView MenuBar { get; }
        public ToolbarView Toolbar { get; }
        public PaletteView Palette { get; }
        public InspectorView Inspector { get; }
        public StatusBarView StatusBar { get; }
        public Dialogs Dialogs { get; }

        private readonly ContainerRuntime _root;
        private readonly ContainerRuntime _viewportHost;

        private ScrollViewer? _scrolled;
        private Vector2 _scrollRemainder;

        public Rectangle ViewportArea => new(_viewportHost.AbsoluteLeft, _viewportHost.AbsoluteTop,
            _viewportHost.AbsoluteWidth, _viewportHost.AbsoluteHeight);

        /// <summary>The cursor is over an interactive part of the UI (or a menu is open).</summary>
        public bool IsMouseOverUi => GumService.Default.Cursor.VisualOver is not null || MenuBar.IsOpen;

        public static bool IsTextInputFocused => InteractiveGue.CurrentInputReceiver is TextBoxBase;

        public EditorUi(LevelEditor editor)
        {
            _root = UiKit.Container(ChildrenLayout.TopToBottomStack);
            _root.Dock(Gum.Wireframe.Dock.Fill);
            _root.AddToRoot();

            MenuBar = new MenuBarView(editor);
            _root.AddChild(MenuBar.Menu);
            MenuBar.Menu.Dock(Gum.Wireframe.Dock.FillHorizontally);

            Toolbar = new ToolbarView(editor);
            _root.AddChild(Toolbar.Root);

            ContainerRuntime middle = UiKit.Container(ChildrenLayout.LeftToRightStack);
            UiKit.FillWidth(middle);
            UiKit.Grow(middle, horizontal: false);
            _root.AddChild(middle);

            Palette = new PaletteView(editor, PaletteWidth);
            middle.AddChild(Palette.Root);

            _viewportHost = UiKit.Container();
            UiKit.Grow(_viewportHost, horizontal: true);
            UiKit.FillHeight(_viewportHost);
            middle.AddChild(_viewportHost);

            Inspector = new InspectorView(editor, InspectorWidth);
            middle.AddChild(Inspector.Root);

            StatusBar = new StatusBarView(editor);
            _root.AddChild(StatusBar.Root);

            Dialogs = new Dialogs();
        }

        /// <summary>
        /// The scroll viewer under the cursor (palette, inspector, a dropdown list), if any.
        /// Every scroll viewer under the cursor also stops handling the wheel itself: Gum rounds
        /// scrolling to whole wheel clicks, which makes trackpad scrolling stutter or do nothing,
        /// so the editor scrolls them instead (<see cref="Scroll"/>). Uses last frame's hover,
        /// because it has to run before Gum's update.
        /// </summary>
        public static ScrollViewer? ScrollViewerUnderCursor()
        {
            ScrollViewer? innermost = null;
            for (GraphicalUiElement? element = GumService.Default.Cursor.VisualOver; element is not null; element = element.Parent as GraphicalUiElement)
            {
                if (element is InteractiveGue { FormsControlAsObject: ScrollViewer viewer })
                {
                    viewer.MouseWheelScrollSpeed = 0;
                    innermost ??= viewer;
                }
            }

            return innermost;
        }

        /// <summary>Scrolls a panel: trackpads point for point, mouse wheels like Gum would (Shift = sideways).</summary>
        public void Scroll(ScrollViewer viewer, ScrollInput input)
        {
            // Gum's default MouseWheelScrollSpeed
            const float NotchPixels = 30f;

            Vector2 notches = input.Notches;
            if (Shortcuts.IsShiftDown())
                notches = new Vector2(notches.X + notches.Y, 0f);

            if (viewer != _scrolled)
            {
                _scrolled = viewer;
                _scrollRemainder = Vector2.Zero;
            }

            // Whole points keep the panel's text crisp; the fraction carries over, so slow swipes still move
            Vector2 exact = input.Points + notches * NotchPixels + _scrollRemainder;
            Vector2 delta = new(MathF.Round(exact.X), MathF.Round(exact.Y));
            _scrollRemainder = exact - delta;

            if (delta.Y != 0f)
                viewer.VerticalScrollBarValue -= delta.Y;

            if (delta.X != 0f && viewer.HorizontalScrollBarMaximum > 0)
                viewer.HorizontalScrollBarValue -= delta.X;
        }

        /// <summary>Per-frame input work that must happen right after Gum's update.</summary>
        public void Update()
        {
            Dialogs.Update();
            Palette.Update();
            Inspector.Update();
        }

        /// <summary>Pulls the current editor state into the controls.</summary>
        public void Refresh()
        {
            MenuBar.Refresh();
            Toolbar.Refresh();
            Palette.Refresh();
            Inspector.Refresh();
            StatusBar.Refresh();
        }

        public void Dispose()
        {
            Dialogs.Close();
            InteractiveGue.CurrentInputReceiver = null;
            _root.RemoveFromRoot();
            GumService.Default.PopupRoot.Children.Clear();
        }
    }
}

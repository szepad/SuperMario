using Gum;
using Gum.Converters;
using Gum.DataTypes;
using Gum.Forms.Controls;
using Gum.Forms.Controls.Primitives;
using Gum.GueDeriving;
using Gum.Managers;
using Gum.Themes.Editor;
using Gum.Wireframe;
using RenderingLibrary.Graphics;

namespace Mario2.Editor
{
    /// <summary>A button's label, action and look.</summary>
    internal sealed record UiAction(string Label, Action Run, EditorIcon? Icon = null, bool Danger = false);

    internal static class GumHost
    {
        private static bool _initialized;

        /// <summary>Gum lives for the rest of the session once the editor is opened.</summary>
        public static void EnsureInitialized()
        {
            if (_initialized)
                return;

            GumService.Default.Initialize();
            Theme.ConfigureGum(EditorTheme.Apply);
            GumService.Default.EnableExpandToWindow();
            _initialized = true;
        }

        /// <summary>
        /// Gum only sizes the main root to the window. The popup and modal roots stay 0x0, and
        /// popups (menus, combo box lists) get clamped into that empty area, i.e. to the top-left corner.
        /// </summary>
        public static void SyncOverlayRoots()
        {
            float width = GumService.Default.CanvasWidth;
            float height = GumService.Default.CanvasHeight;

            foreach (InteractiveGue root in new[] { GumService.Default.PopupRoot, GumService.Default.ModalRoot })
            {
                if (root.Width != width || root.Height != height)
                {
                    root.WidthUnits = DimensionUnitType.Absolute;
                    root.HeightUnits = DimensionUnitType.Absolute;
                    root.Width = width;
                    root.Height = height;
                }
            }
        }
    }

    /// <summary>Small helpers for building the editor's Gum layout in code.</summary>
    internal static class UiKit
    {
        /// <summary>A layout-only container. It doesn't take mouse input, so clicks reach what's behind it.</summary>
        public static ContainerRuntime Container(ChildrenLayout layout = ChildrenLayout.Regular)
        {
            ContainerRuntime container = new()
            {
                HasEvents = false,
                ChildrenLayout = layout
            };

            return container;
        }

        public static RectangleRuntime Filled(Color color)
        {
            RectangleRuntime rect = new(true, null)
            {
                IsFilled = true,
                FillColor = color,
                StrokeColor = color,
                StrokeWidth = 0
            };
            return rect;
        }

        public static RectangleRuntime Background(GraphicalUiElement parent, Color color)
        {
            RectangleRuntime rect = Filled(color);
            rect.Dock(Dock.Fill);
            parent.AddChild(rect);
            return rect;
        }

        public static RectangleRuntime Line(GraphicalUiElement parent, Anchor side)
        {
            RectangleRuntime rect = Filled(Theme.Border);
            rect.Anchor(side);

            if (side is Anchor.Left or Anchor.Right)
            {
                rect.Width = 1;
                rect.HeightUnits = DimensionUnitType.RelativeToParent;
                rect.Height = 0;
            }
            else
            {
                rect.Height = 1;
                rect.WidthUnits = DimensionUnitType.RelativeToParent;
                rect.Width = 0;
            }

            parent.AddChild(rect);
            return rect;
        }

        public static void FillWidth(GraphicalUiElement element, float padding = 0f)
        {
            element.WidthUnits = DimensionUnitType.RelativeToParent;
            element.Width = -padding;
        }

        public static void FillWidth(FrameworkElement element, float padding = 0f) => FillWidth(element.Visual, padding);

        public static void FillHeight(GraphicalUiElement element, float padding = 0f)
        {
            element.HeightUnits = DimensionUnitType.RelativeToParent;
            element.Height = -padding;
        }

        /// <summary>Takes the remaining space of a stacking parent.</summary>
        public static void Grow(GraphicalUiElement element, bool horizontal)
        {
            if (horizontal)
            {
                element.WidthUnits = DimensionUnitType.Ratio;
                element.Width = 1;
            }
            else
            {
                element.HeightUnits = DimensionUnitType.Ratio;
                element.Height = 1;
            }
        }

        public static Label Label(string text, Color? color = null, float? fontSize = null)
        {
            Label label = new() { Text = text };
            if (label.TextComponent is TextRuntime runtime)
            {
                if (color is { } c)
                    runtime.Color = c;

                if (fontSize is { } size)
                    runtime.FontSize = size;
            }

            return label;
        }

        public static void SetColor(Label label, Color color)
        {
            if (label.TextComponent is TextRuntime runtime && !runtime.Color.Equals(color))
                runtime.Color = color;
        }

        public static void SetText(Label label, string text)
        {
            if (label.Text != text)
                label.Text = text;
        }

        public static Button Button(string text, Action onClick, EditorIcon? icon = null, float? width = null)
        {
            Button button = new() { Text = text };
            Setup(button, text, onClick, icon, width);
            return button;
        }

        public static Button Button(UiAction action)
        {
            Button button = Button(action.Label, action.Run, action.Icon);
            if (action.Danger)
                SetForeground(button, Theme.DangerText);

            return button;
        }

        public static ToggleButton Toggle(string text, Action onClick, EditorIcon? icon = null)
        {
            ToggleButton button = new() { Text = text };
            Setup(button, text, onClick, icon, null);
            return button;
        }

        private static void Setup(FrameworkElement button, string text, Action onClick, EditorIcon? icon, float? width)
        {
            button.WidthUnits = DimensionUnitType.Absolute;
            button.Width = width ?? WidthFor(text) + (icon is null ? 0f : IconGap + EditorIcons.Size);

            if (icon is { } shown)
                AddIcon(button, shown);

            ((ButtonBase)button).Click += (_, _) =>
            {
                // Don't let the button keep keyboard focus, or Enter/Space would press it again
                InteractiveGue.CurrentInputReceiver = null;
                onClick();
            };
        }

        private const float IconGap = 5f;

        /// <summary>
        /// Puts an icon just left of a button's (centered) text and shifts the text right by half
        /// of that, so icon and text stay centered together whatever the text's real width.
        /// </summary>
        private static void AddIcon(FrameworkElement button, EditorIcon icon)
        {
            if (button.Visual.GetGraphicalUiElementByName("TextInstance") is not { } text)
                return;

            // Toggle buttons stretch their text across the whole button; make it hug the text instead,
            // centered, like the themed Button does, so "left of the text" really is next to the text
            text.WidthUnits = DimensionUnitType.RelativeToChildren;
            text.Width = 0;
            text.XOrigin = HorizontalAlignment.Center;
            text.XUnits = GeneralUnitType.PixelsFromMiddle;
            text.X = 0;

            SpriteRuntime sprite = EditorIcons.Sprite(icon);
            sprite.XOrigin = HorizontalAlignment.Right;
            sprite.XUnits = GeneralUnitType.PixelsFromSmall;
            sprite.X = -IconGap;
            sprite.YOrigin = VerticalAlignment.Center;
            sprite.YUnits = GeneralUnitType.PixelsFromMiddle;
            sprite.Y = 0;

            // The text sizes itself to its content; the icon hangs outside of it
            sprite.IgnoredByParentSize = true;
            text.AddChild(sprite);
            text.X += (EditorIcons.Size + IconGap) / 2f;
        }

        /// <summary>
        /// Gives a property grid's rows the theme's alternating colours. The grid stripes itself in fixed
        /// greys whenever a row is added (or shown/hidden, which the editor never does), so call this after.
        /// </summary>
        public static void StripeRows(GraphicalUiElement grid)
        {
            int index = 0;
            foreach (GraphicalUiElement row in grid.Children)
            {
                if (row.Visible && row.Children.Count > 0 && row.Children[0] is RectangleRuntime background)
                    background.FillColor = index++ % 2 == 0 ? Theme.RowEven : Theme.RowOdd;
            }
        }

        /// <summary>The text colour of a button in every state (normal, hover, pressed).</summary>
        public static void SetForeground(FrameworkElement button, Color color)
        {
            if (button.Visual is Gum.Forms.DefaultVisuals.V3.ButtonVisual visual)
                visual.ForegroundColor = color;
        }

        /// <summary>Marks the button Enter presses (a dialog's first button).</summary>
        public static void MakePrimary(Button button)
        {
            if (button.Visual is Gum.Forms.DefaultVisuals.V3.ButtonVisual visual)
                visual.BackgroundColor = Theme.Selection;
        }

        /// <summary>A button width that fits its label in the theme's font.</summary>
        public static float WidthFor(string text)
        {
            const float Margin = 22f;
            return MathF.Ceiling(TextWidth(text) + Margin);
        }

        /// <summary>Roughly how wide a text is in the theme's font (an average character is 7.6 px).</summary>
        public static float TextWidth(string text) => text.Length * 7.6f;

        public static StackPanel Row(float spacing = 6f)
        {
            StackPanel row = new()
            {
                Orientation = Orientation.Horizontal,
                Spacing = spacing
            };
            return row;
        }
    }
}

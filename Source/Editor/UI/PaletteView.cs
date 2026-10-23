using Gum;
using Gum.DataTypes;
using Gum.Forms.Controls;
using Gum.GueDeriving;
using Gum.Input;
using Gum.Managers;
using Gum.Wireframe;

namespace Mario2.Editor
{
    /// <summary>
    /// The brush palette: a category picker over a grid of thumbnails. The Tileset category shows
    /// the whole tileset, where a click picks one tile and a drag picks a block of tiles as a stamp.
    /// </summary>
    public sealed class PaletteView
    {
        private const float Padding = 8f;
        private const float CellSize = 44f;
        private const float IconSize = 34f;
        private const int AtlasScale = 2;


        public ContainerRuntime Root { get; }

        private readonly LevelEditor _editor;
        private readonly ComboBox _category;
        private readonly ScrollViewer _scroll;
        private readonly Label _nameLabel;
        private readonly Label _hintLabel;
        private readonly List<(PaletteItem Item, ContainerRuntime Cell, RectangleRuntime Fill)> _cells = [];
        private readonly PaletteCategory[] _categories = Enum.GetValues<PaletteCategory>();

        private PaletteCategory _shown = (PaletteCategory)(-1);
        private PaletteItem? _hovered;

        // Tileset view
        private ContainerRuntime? _atlasHost;
        private RectangleRuntime? _atlasSelection;
        private RectangleRuntime? _atlasHover;
        private (int Col, int Row)? _atlasDragStart;
        private TileRect? _atlasPicked;

        public PaletteView(LevelEditor editor, float width)
        {
            _editor = editor;

            Root = UiKit.Container();
            Root.Width = width;
            UiKit.FillHeight(Root);
            UiKit.Background(Root, Theme.Panel);
            UiKit.Line(Root, Anchor.Right);

            ContainerRuntime content = UiKit.Container(ChildrenLayout.TopToBottomStack);
            content.X = Padding;
            content.Y = Padding;
            content.StackSpacing = 6;
            UiKit.FillWidth(content, Padding * 2);
            UiKit.FillHeight(content, Padding * 2);
            Root.AddChild(content);

            content.AddChild(UiKit.Label("PALETTE", Theme.TextMuted));

            _category = new ComboBox();
            UiKit.FillWidth(_category);
            foreach (PaletteCategory category in _categories)
                _category.Items!.Add(PaletteCatalog.DisplayName(category));

            _category.SelectionChanged += (_, _) =>
            {
                if (_category.SelectedIndex >= 0)
                    Show(_categories[_category.SelectedIndex]);
            };
            content.AddChild(_category);

            _scroll = new ScrollViewer();
            UiKit.FillWidth(_scroll);
            UiKit.Grow(_scroll.Visual, horizontal: false);
            _scroll.InnerPanel.ChildrenLayout = ChildrenLayout.LeftToRightStack;
            _scroll.InnerPanel.WrapsChildren = true;
            content.AddChild(_scroll);

            _nameLabel = UiKit.Label(string.Empty, Theme.Text);
            content.AddChild(_nameLabel);

            _hintLabel = UiKit.Label(string.Empty, Theme.TextMuted);
            content.AddChild(_hintLabel);

            _category.SelectedIndex = 0;
        }

        /// <summary>Switches the palette to a category, as if it was picked in the drop-down.</summary>
        public void ShowCategory(PaletteCategory category)
        {
            int index = Array.IndexOf(_categories, category);
            if (_category.SelectedIndex != index)
                _category.SelectedIndex = index;
        }

        private void Show(PaletteCategory category)
        {
            if (category == _shown)
                return;

            _shown = category;
            _scroll.InnerPanel.Children.Clear();
            _cells.Clear();
            _atlasHost = null;
            _hovered = null;
            _scroll.VerticalScrollBarValue = 0;

            if (category == PaletteCategory.Tileset)
            {
                BuildAtlas();
                return;
            }

            _scroll.InnerPanel.ChildrenLayout = ChildrenLayout.LeftToRightStack;
            foreach (PaletteItem item in PaletteCatalog.In(category))
            {
                _scroll.InnerPanel.AddChild(BuildCell(item));
            }
        }

        private ContainerRuntime BuildCell(PaletteItem item)
        {
            ContainerRuntime cell = new()
            {
                Width = CellSize,
                Height = CellSize,
                HasEvents = true
            };

            RectangleRuntime fill = UiKit.Background(cell, Theme.Transparent);
            cell.AddChild(Sprite(item.Icon, IconSize, Anchor.Center));

            if (item.Badge is { } badge)
            {
                SpriteRuntime badgeSprite = Sprite(badge, 18f, Anchor.BottomRight);
                cell.AddChild(badgeSprite);
            }

            cell.Click += (_, _) =>
            {
                InteractiveGue.CurrentInputReceiver = null;
                _editor.SetBrush(item.CreateBrush());
            };
            cell.RollOn += (_, _) => _hovered = item;
            cell.RollOff += (_, _) =>
            {
                if (_hovered == item)
                    _hovered = null;
            };

            _cells.Add((item, cell, fill));
            return cell;
        }

        private void BuildAtlas()
        {
            Texture2D tileset = Assets.GetTex(TilesetInfo.TextureName);
            _scroll.InnerPanel.ChildrenLayout = ChildrenLayout.Regular;

            _atlasHost = new ContainerRuntime
            {
                Width = tileset.Width * AtlasScale,
                Height = tileset.Height * AtlasScale,
                HasEvents = true
            };

            SpriteRuntime sprite = new(true, false)
            {
                Texture = tileset,
                WidthUnits = DimensionUnitType.Absolute,
                HeightUnits = DimensionUnitType.Absolute,
                Width = tileset.Width * AtlasScale,
                Height = tileset.Height * AtlasScale
            };
            _atlasHost.AddChild(sprite);

            _atlasHover = Outline(Theme.Hover);
            _atlasSelection = Outline(Theme.Info);
            _atlasHost.AddChild(_atlasHover);
            _atlasHost.AddChild(_atlasSelection);

            _scroll.InnerPanel.AddChild(_atlasHost);
        }

        private static RectangleRuntime Outline(Color color)
        {
            RectangleRuntime rect = new(true, null)
            {
                IsFilled = false,
                StrokeWidth = 2,
                StrokeColor = color,
                Visible = false
            };
            return rect;
        }

        private static SpriteRuntime Sprite(Icon icon, float maxSize, Anchor anchor)
        {
            Texture2D texture = Assets.GetTex(icon.Texture);
            Rectangle src = icon.Source;

            // Integer scaling keeps pixel art crisp when the icon fits
            float fit = Math.Min(maxSize / src.Width, maxSize / src.Height);
            float scale = fit >= 1f ? MathF.Floor(fit) : fit;

            SpriteRuntime sprite = new(true, false)
            {
                Texture = texture,
                TextureAddress = TextureAddress.Custom,
                TextureLeft = (int)src.X,
                TextureTop = (int)src.Y,
                TextureWidth = (int)src.Width,
                TextureHeight = (int)src.Height,
                WidthUnits = DimensionUnitType.Absolute,
                HeightUnits = DimensionUnitType.Absolute,
                Width = src.Width * scale,
                Height = src.Height * scale
            };
            sprite.Anchor(anchor);
            return sprite;
        }

        public void Update()
        {
            if (_atlasHost is null)
                return;

            Cursor cursor = GumService.Default.Cursor;
            (int Col, int Row) cell = AtlasCellAt(cursor);
            bool over = cursor.VisualOver == _atlasHost;

            if (over && cursor.PrimaryPush)
                _atlasDragStart = cell;

            if (_atlasDragStart is { } start)
            {
                TileRect rect = TileRect.FromCorners(start.Col, start.Row, cell.Col, cell.Row);
                ShowAtlasRect(_atlasSelection!, rect);

                if (!cursor.PrimaryDown)
                {
                    _atlasDragStart = null;
                    _atlasPicked = rect;
                    InteractiveGue.CurrentInputReceiver = null;
                    _editor.SetBrush(PaletteCatalog.TilesetBrush(rect));
                }
            }

            if (over)
                ShowAtlasRect(_atlasHover!, new TileRect(cell.Col, cell.Row, 1, 1));
            else
                _atlasHover!.Visible = false;
        }

        private (int Col, int Row) AtlasCellAt(Cursor cursor)
        {
            Texture2D tileset = Assets.GetTex(TilesetInfo.TextureName);
            float cellPx = TilesetInfo.TileSizePx * AtlasScale;
            int col = (int)MathF.Floor((cursor.X - _atlasHost!.AbsoluteLeft) / cellPx);
            int row = (int)MathF.Floor((cursor.Y - _atlasHost.AbsoluteTop) / cellPx);
            return (Math.Clamp(col, 0, tileset.Width / TilesetInfo.TileSizePx - 1),
                Math.Clamp(row, 0, tileset.Height / TilesetInfo.TileSizePx - 1));
        }

        private static void ShowAtlasRect(RectangleRuntime outline, TileRect rect)
        {
            float cellPx = TilesetInfo.TileSizePx * AtlasScale;
            outline.X = rect.Col * cellPx;
            outline.Y = rect.Row * cellPx;
            outline.Width = rect.Width * cellPx;
            outline.Height = rect.Height * cellPx;
            outline.Visible = true;
        }

        public void Refresh()
        {
            Brush brush = _editor.Brush;

            foreach ((PaletteItem item, _, RectangleRuntime fill) in _cells)
            {
                Color color = brush.Source == item ? Theme.Selection
                    : _hovered == item ? Theme.HoverFill
                    : Theme.Transparent;

                if (!fill.FillColor.Equals(color))
                {
                    fill.FillColor = color;
                    fill.StrokeColor = color;
                }
            }

            if (_atlasSelection is not null && _atlasDragStart is null)
                _atlasSelection.Visible = _atlasPicked is not null && brush.Source == PaletteCatalog.TilesetSource;

            UiKit.SetText(_nameLabel, _hovered?.Name ?? brush.Name);
            UiKit.SetText(_hintLabel, _hovered is not null ? "Click to paint with it" : "Current brush");
        }
    }
}

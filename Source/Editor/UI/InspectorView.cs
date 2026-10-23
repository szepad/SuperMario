using Gum.Converters;
using Gum.DataTypes;
using Gum.Forms.Controls;
using Gum.GueDeriving;
using Gum.Managers;
using Gum.Themes.Editor;
using Gum.Wireframe;
using RenderingLibrary.Graphics;

namespace Mario2.Editor
{
    /// <summary>
    /// Level settings, the brush's properties and the selection's properties. Entity fields are
    /// generated from the entity's property schema.
    /// </summary>
    public sealed class InspectorView
    {
        private const float Padding = 8f;

        // Only world music is meaningful for a level
        private static readonly string[] LevelMusic =
            [.. Enum.GetNames<MusicName>().Take((int)MusicName.UndergroundIntro + 1)];

        private const string NothingSelected = "none";

        public ContainerRuntime Root { get; }

        private readonly LevelEditor _editor;
        private readonly Section _level;
        private readonly Section _selection;
        private readonly Section _brush;

        public InspectorView(LevelEditor editor, float width)
        {
            _editor = editor;

            Root = UiKit.Container();
            Root.Width = width;
            UiKit.FillHeight(Root);
            UiKit.Background(Root, Theme.Panel);
            UiKit.Line(Root, Anchor.Left);

            ScrollViewer scroll = new();
            scroll.X = Padding;
            scroll.Y = Padding;
            UiKit.FillWidth(scroll, Padding * 2);
            UiKit.FillHeight(scroll.Visual, Padding * 2);
            scroll.InnerPanel.ChildrenLayout = ChildrenLayout.TopToBottomStack;
            scroll.InnerPanel.StackSpacing = 10;
            Root.AddChild(scroll);

            // Top to bottom: the level rarely changes, the selection changes all the time
            _level = new Section("Level", scroll.InnerPanel, Theme.Accent, EditorIcon.Level);
            _level.Rebuild("level", BuildLevel);
            _brush = new Section("Brush", scroll.InnerPanel, Theme.Success, EditorIcon.Brush);
            _selection = new Section("Selection", scroll.InnerPanel, Theme.Info, EditorIcon.Select);
        }

        public void Update()
        {
            _level.Update();
            _selection.Update();
            _brush.Update();
        }

        public void Refresh()
        {
            (string selectionKey, Action<SectionBuilder> buildSelection) = DescribeSelection();
            _selection.Rebuild(selectionKey, buildSelection);
            _selection.IsVisible = selectionKey != NothingSelected;

            (string brushKey, Action<SectionBuilder> buildBrush) = DescribeBrush();
            _brush.Rebuild(brushKey, buildBrush);

            _level.Refresh();
            _selection.Refresh();
            _brush.Refresh();
        }

        #region Level

        private void BuildLevel(SectionBuilder b)
        {
            LevelData Level() => _editor.Document.Level;

            b.Text("Width (tiles)", () => Level().Width.ToString(), _editor.SetLevelWidth);
            b.Choice("Background", Enum.GetNames<WorldBackground>(), () => Level().Meta.Background.ToString(),
                v => _editor.SetLevelMeta(m => m with { Background = Enum.Parse<WorldBackground>(v) }, "Change Background"));
            b.Choice("Music", LevelMusic, () => Level().Meta.Music.ToString(),
                v => _editor.SetLevelMeta(m => m with { Music = Enum.Parse<MusicName>(v) }, "Change Music"));
            b.Text("Time", () => Level().Meta.Time.ToString(), v =>
            {
                if (!int.TryParse(v, out int time) || time < 1 || time > 999)
                    return false;

                _editor.SetLevelMeta(m => m with { Time = time }, "Change Time");
                return true;
            });
            b.Check("Pipe Intro", () => Level().Meta.PipeIntro,
                v => _editor.SetLevelMeta(m => m with { PipeIntro = v }, "Toggle Pipe Intro"));
            b.Info("Player Start", () => $"{Level().Meta.SpawnX}, {Level().Meta.SpawnY}");
            b.Info("File", () => _editor.Document.FilePath == string.Empty ? "Not saved yet" : Path.GetFileName(_editor.Document.FilePath));
        }

        #endregion

        #region Selection

        private (string Key, Action<SectionBuilder> Build) DescribeSelection()
        {
            EditorSelection selection = _editor.Selection;
            LevelData level = _editor.Document.Level;
            int count = selection.Entities.Count + (selection.Spawn ? 1 : 0);

            if (selection.Region is { } region)
                return ($"region:{region}", b => BuildRegion(b, region));

            if (count > 1)
                return ($"multi:{count}", b => BuildMultiple(b, count));

            if (selection.Spawn)
                return ("spawn", BuildSpawn);

            if (selection.Entities.Count == 1 && level.FindEntity(selection.Entities.First()) is { Def: { } def } placement)
                return ($"entity:{placement.Id}:{def.Key}", b => BuildEntity(b, placement.Id, def));

            if (selection.Tile is { } tile)
            {
                TileSpec spec = level.Get(tile.Col, tile.Row);
                return ($"tile:{tile.Col},{tile.Row}:{spec.GetType().Name}", b => BuildTile(b, tile.Col, tile.Row));
            }

            return (NothingSelected, _ => { });
        }

        private UiAction CopyAction => new("Copy", _editor.CopySelection, EditorIcon.Copy);
        private UiAction DuplicateAction => new("Duplicate", _editor.DuplicateSelection, EditorIcon.Duplicate);
        private UiAction DeleteAction => new("Delete", _editor.DeleteSelection, EditorIcon.Delete, Danger: true);

        private void BuildEntity(SectionBuilder b, int id, EntityDef def)
        {
            EntityPlacement? Current() => _editor.Document.Level.FindEntity(id);

            b.Heading(def.Name);
            b.Text("Feet X", () => Current()?.X.ToString() ?? string.Empty,
                v => _editor.SetEntityPosition(id, v, Current()?.Y.ToString() ?? "0"));
            b.Text("Feet Y", () => Current()?.Y.ToString() ?? string.Empty,
                v => _editor.SetEntityPosition(id, Current()?.X.ToString() ?? "0", v));

            AddProperties(b, def,
                property => Current() is { } placement ? def.GetValue(placement, property) : property.DefaultText,
                (property, value) => _editor.SetEntityProperty(id, property, value),
                property => Current() is { } placement && property.EnabledFor(placement.Props));

            b.Buttons(DuplicateAction, DeleteAction);
        }

        private void BuildSpawn(SectionBuilder b)
        {
            LevelMeta Meta() => _editor.Document.Level.Meta;

            b.Heading("Player Start");
            b.Text("Feet X", () => Meta().SpawnX.ToString(), v => SetSpawn(v, null));
            b.Text("Feet Y", () => Meta().SpawnY.ToString(), v => SetSpawn(null, v));
            b.Note("Where Mario starts the level. Drag it to move it.");
        }

        private bool SetSpawn(string? x, string? y)
        {
            LevelMeta meta = _editor.Document.Level.Meta;
            int spawnX = meta.SpawnX, spawnY = meta.SpawnY;
            if ((x is not null && !int.TryParse(x, out spawnX)) || (y is not null && !int.TryParse(y, out spawnY)))
                return false;

            _editor.SetLevelMeta(m => m with { SpawnX = spawnX, SpawnY = spawnY }, "Move Player Start");
            return true;
        }

        private void BuildMultiple(SectionBuilder b, int count)
        {
            b.Heading($"{count} objects selected");
            b.Note("Drag or use the arrow keys to move them together.");
            b.Buttons(CopyAction, DuplicateAction, DeleteAction);
        }

        private void BuildRegion(SectionBuilder b, TileRect region)
        {
            b.Heading($"Area {region.Width} x {region.Height}");
            b.Info("From", () => $"{region.Col}, {region.Row}");
            b.Info("Entities", () => _editor.Selection.Entities.Count.ToString());
            b.Buttons(CopyAction, new UiAction("Cut", _editor.CutSelection, EditorIcon.Cut),
                new UiAction("Clear Tiles", () => _editor.FillRect(region, TileSpec.Air, "Clear Area"), EditorIcon.Delete, Danger: true));
        }

        private void BuildTile(SectionBuilder b, int col, int row)
        {
            TileSpec Spec() => _editor.Document.Level.Get(col, row);
            void Set(TileSpec spec, string label) => _editor.SetTileSpec(col, row, spec, label);

            b.Heading(() => TileNames.Describe(Spec()));
            b.Info("Cell", () => $"{col}, {row}");

            switch (Spec())
            {
                case QuestionSpec:
                    b.Choice("Item", Enum.GetNames<ItemType>(), () => (Spec() as QuestionSpec)?.Item.ToString() ?? string.Empty,
                        v => Set((QuestionSpec)Spec() with { Item = Enum.Parse<ItemType>(v) }, "Change Item"));
                    b.Check("Hidden", () => Spec() is QuestionSpec { Hidden: true },
                        v => Set((QuestionSpec)Spec() with { Hidden = v }, "Toggle Hidden"));
                    break;

                case BrickSpec:
                    b.Choice("Item", Enum.GetNames<ItemType>(), () => (Spec() as BrickSpec)?.Item.ToString() ?? string.Empty,
                        v => Set((BrickSpec)Spec() with { Item = Enum.Parse<ItemType>(v) }, "Change Item"));
                    b.Check("Hidden", () => Spec() is BrickSpec { Hidden: true },
                        v => Set((BrickSpec)Spec() with { Hidden = v }, "Toggle Hidden"));
                    b.Choice("Palette", Enum.GetNames<TilePalette>(), () => (Spec() as BrickSpec)?.Palette.ToString() ?? string.Empty,
                        v => Set((BrickSpec)Spec() with { Palette = Enum.Parse<TilePalette>(v) }, "Change Palette"));
                    break;

                case SimpleSpec:
                    b.Info("Tileset Id", () => (Spec() as SimpleSpec)?.Id.ToString() ?? string.Empty);
                    b.Check("Solid", () => Spec().IsSolid, v => Set((SimpleSpec)Spec() with { Solid = v }, "Toggle Solid"));
                    break;

                case AutoSpec:
                    b.Note("Autotile: its edges are picked from its neighbours.");
                    b.Buttons(new UiAction("Detach (keep this look)", () => Set(_editor.Preview.ResolvedAt(col, row), "Detach Autotile"), EditorIcon.Detach));
                    break;
            }

            b.Buttons(DeleteAction);
        }

        #endregion

        #region Brush

        private (string Key, Action<SectionBuilder> Build) DescribeBrush()
        {
            Brush brush = _editor.Brush;
            return brush switch
            {
                TileBrush tile => ($"tile:{tile.Name}:{tile.Spec.GetType().Name}", BuildTileBrush),
                EntityBrush entity => ($"entity:{entity.Name}:{entity.Def.Key}", b => BuildEntityBrush(b, entity.Def)),
                StampBrush stamp => ($"stamp:{stamp.Name}", b => BuildStampBrush(b, stamp)),
                _ => ("none", _ => { })
            };
        }

        private void BuildTileBrush(SectionBuilder b)
        {
            TileBrush? Current() => _editor.Brush as TileBrush;
            void Set(TileSpec spec)
            {
                if (Current() is { } brush)
                    _editor.UpdateBrush(brush.With(spec));
            }

            b.Heading(() => Current() is { } brush ? TileNames.Describe(brush.Spec) : string.Empty);

            switch (Current()?.Spec)
            {
                case QuestionSpec:
                    b.Choice("Item", Enum.GetNames<ItemType>(), () => (Current()?.Spec as QuestionSpec)?.Item.ToString() ?? string.Empty,
                        v => Set((QuestionSpec)Current()!.Spec with { Item = Enum.Parse<ItemType>(v) }));
                    b.Check("Hidden", () => Current()?.Spec is QuestionSpec { Hidden: true },
                        v => Set((QuestionSpec)Current()!.Spec with { Hidden = v }));
                    break;

                case BrickSpec:
                    b.Choice("Item", Enum.GetNames<ItemType>(), () => (Current()?.Spec as BrickSpec)?.Item.ToString() ?? string.Empty,
                        v => Set((BrickSpec)Current()!.Spec with { Item = Enum.Parse<ItemType>(v) }));
                    b.Check("Hidden", () => Current()?.Spec is BrickSpec { Hidden: true },
                        v => Set((BrickSpec)Current()!.Spec with { Hidden = v }));
                    b.Choice("Palette", Enum.GetNames<TilePalette>(), () => (Current()?.Spec as BrickSpec)?.Palette.ToString() ?? string.Empty,
                        v => Set((BrickSpec)Current()!.Spec with { Palette = Enum.Parse<TilePalette>(v) }));
                    break;

                case SimpleSpec:
                    b.Check("Solid", () => Current()?.Spec.IsSolid ?? false, v => Set((SimpleSpec)Current()!.Spec with { Solid = v }));
                    break;

                case AutoSpec:
                    b.Note("Autotile: edges are picked automatically as you paint.");
                    break;
            }
        }

        private void BuildEntityBrush(SectionBuilder b, EntityDef def)
        {
            EntityBrush? Current() => _editor.Brush as EntityBrush;

            b.Heading(Current()?.Name ?? def.Name);
            AddProperties(b, def,
                property => Current()?.Props.GetValueOrDefault(property.Key) ?? property.DefaultText,
                _editor.SetBrushProperty,
                property => Current() is { } brush && property.EnabledFor(brush.Props));

            if (def.IsUnique)
                b.Note("Only one per level: placing it again moves it.");
        }

        private static void BuildStampBrush(SectionBuilder b, StampBrush brush)
        {
            b.Heading(brush.Name);
            b.Info("Size", () => $"{brush.Stamp.Width} x {brush.Stamp.Height}");
            if (brush.Stamp.Entities.Count > 0)
                b.Info("Entities", () => brush.Stamp.Entities.Count.ToString());

            b.Note("Click in the level to place it.");
        }

        #endregion

        private static void AddProperties(SectionBuilder b, EntityDef def, Func<PropertyDef, string> get,
            Func<PropertyDef, string, bool> set, Func<PropertyDef, bool> isEnabled)
        {
            foreach (PropertyDef property in def.Properties)
            {
                Func<bool> enabled = () => isEnabled(property);
                switch (property.Kind)
                {
                    case PropertyKind.Bool:
                        b.Check(property.Label, () => get(property) == "true", v => set(property, v ? "true" : "false"), enabled);
                        break;

                    case PropertyKind.Enum:
                        b.Choice(property.Label, property.Choices, () => get(property), v => set(property, v), enabled);
                        break;

                    case PropertyKind.Path:
                        b.Path(property.Label, () => get(property), v => set(property, v), enabled);
                        break;

                    default:
                        b.Text(property.Label, () => get(property), v => set(property, v), enabled);
                        break;
                }
            }
        }

        /// <summary>A collapsible inspector section whose content is rebuilt when its key changes.</summary>
        private sealed class Section
        {
            private readonly Expander _expander;
            private readonly ContainerRuntime _body;
            private readonly List<Field> _fields = [];
            private string? _key;

            public Section(string header, GraphicalUiElement parent, Color accent, EditorIcon icon)
            {
                _expander = new Expander { Header = header, IsExpanded = true };
                UiKit.FillWidth(_expander);
                parent.AddChild(_expander.Visual);
                StyleHeader(accent, icon);

                _body = UiKit.Container(ChildrenLayout.TopToBottomStack);
                _body.StackSpacing = 6;
                UiKit.FillWidth(_body);
                _body.HeightUnits = DimensionUnitType.RelativeToChildren;
                _body.Height = 4;
                _expander.AddContent(_body);
            }

            /// <summary>
            /// The theme draws every expander header in the same hard-coded grey. Each section gets its
            /// own colour (title and a stripe on the left) and an icon, so they're easy to tell apart.
            /// </summary>
            private void StyleHeader(Color accent, EditorIcon icon)
            {
                if (_expander.Visual.GetGraphicalUiElementByName("HeaderContainer") is not { } header)
                    return;

                if (header.Children.OfType<RectangleRuntime>().FirstOrDefault() is { } background)
                    background.FillColor = Theme.PanelRaised;

                RectangleRuntime stripe = UiKit.Filled(accent);
                stripe.Width = 3;
                UiKit.FillHeight(stripe);
                header.AddChild(stripe);

                if (header.GetGraphicalUiElementByName("HeaderContent") is not { } content)
                    return;

                // No expand/collapse arrow (the header still toggles the section when clicked)
                if (content.GetGraphicalUiElementByName("ArrowIndicator") is { } arrow)
                    arrow.Visible = false;

                SpriteRuntime sprite = EditorIcons.Sprite(icon);
                sprite.X = 10;
                sprite.YOrigin = VerticalAlignment.Center;
                sprite.YUnits = GeneralUnitType.PixelsFromMiddle;
                content.Children.Insert(1, sprite);

                if (content.GetGraphicalUiElementByName("TextInstance") is TextRuntime title)
                {
                    title.Color = accent;
                    title.X = 6;
                }
            }

            public bool IsVisible
            {
                get => _expander.IsVisible;
                set
                {
                    if (_expander.IsVisible != value)
                        _expander.IsVisible = value;
                }
            }

            public void Rebuild(string key, Action<SectionBuilder> build)
            {
                if (key == _key)
                    return;

                _key = key;
                _fields.Clear();
                _body.Children.Clear();
                build(new SectionBuilder(_body, _fields));
            }

            public void Update()
            {
                foreach (Field field in _fields)
                    field.Update();
            }

            public void Refresh()
            {
                foreach (Field field in _fields)
                    field.Refresh();
            }
        }

        private sealed class SectionBuilder(ContainerRuntime body, List<Field> fields)
        {
            private PropertyGridVisual? _grid;

            public void Heading(string text) => Heading(() => text);

            public void Heading(Func<string> text)
            {
                _grid = null;
                InfoField field = new(text);
                UiKit.SetColor((Label)field.Control, Theme.Text);
                body.AddChild(field.Control);
                fields.Add(field);
            }

            public void Note(string text)
            {
                _grid = null;
                Label label = UiKit.Label(text, Theme.TextMuted);
                UiKit.FillWidth(label);
                body.AddChild(label);
            }

            public void Text(string label, Func<string> get, Func<string, bool> set, Func<bool>? enabled = null)
                => Row(label, new TextField(get, set) { IsEnabled = enabled });

            public void Check(string label, Func<bool> get, Action<bool> set, Func<bool>? enabled = null)
                => Row(label, new CheckField(get, set) { IsEnabled = enabled });

            public void Choice(string label, IReadOnlyList<string> choices, Func<string> get, Action<string> set, Func<bool>? enabled = null)
                => Row(label, new ChoiceField(choices, get, set) { IsEnabled = enabled });

            public void Path(string label, Func<string> get, Func<string, bool> set, Func<bool>? enabled = null)
                => Row(label, new PathField(get, set) { IsEnabled = enabled });

            public void Info(string label, Func<string> get) => Row(label, new InfoField(get));

            public void Buttons(params UiAction[] buttons)
            {
                _grid = null;
                StackPanel row = UiKit.Row(6f);
                foreach (UiAction action in buttons)
                    row.AddChild(UiKit.Button(action));

                body.AddChild(row);
            }

            private void Row(string label, Field field)
            {
                if (_grid is null)
                {
                    _grid = new PropertyGridVisual();
                    UiKit.FillWidth(_grid);
                    body.AddChild(_grid);
                }

                _grid.AddRow(label, field.Control);
                UiKit.StripeRows(_grid);
                fields.Add(field);
                field.Refresh();
            }
        }
    }
}

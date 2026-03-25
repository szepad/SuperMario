namespace Mario2.Editor
{
    public class PropertyWindow : EditorWindow
    {
        private int _colsInputVal = 0;

        public ItemType ContainerTileItem => (ItemType)_ctItemIndex;
        private int _ctItemIndex;

        public bool ContainerTileHidden => _ctHidden;
        private bool _ctHidden;

        public TilePalette BrickTilePalette => (TilePalette)_brickTilePaletteIndex;
        private int _brickTilePaletteIndex;

        private readonly EditPropertyGroup<World> _worldProps;
        private readonly Dictionary<Type, IEditPropertyGroup> _entityPropsByType;

        public PropertyWindow(EditorScene editor)
            : base(editor, "Properties")
        {
            _worldProps = new EditPropertyGroup<World>(string.Empty, Editor);

            _entityPropsByType = new()
            {
                [typeof(WarpPipe)] = new EditPropertyGroup<WarpPipe>("Warp Pipe", Editor),
                [typeof(WarpExit)] = new EditPropertyGroup<WarpExit>("Warp Exit", Editor),
                [typeof(ConstantPlatform)] = new EditPropertyGroup<ConstantPlatform>("Platform - Constant", Editor),
                [typeof(PingPongPlatform)] = new EditPropertyGroup<PingPongPlatform>("Platform - Ping Pong", Editor),
                [typeof(RedFlyingKoopa)] = new EditPropertyGroup<RedFlyingKoopa>("Red Koopa", Editor),
                [typeof(Firebar)] = new EditPropertyGroup<Firebar>("Firebar", Editor),
            };

            AddWorldProps();
            AddWarpPipeProps();
            AddWarpExitProps();
            AddConstantPlatformProps();
            AddPingPongPlatformProps();
            AddRedFlyingKoopaProps();
            AddFirebarProps();
        }

        private void AddWorldProps()
        {
            _worldProps.Add(new ComboEditProperty<World, WorldBackground>("Background",
                world => world.Background,
                (world, val) => world.SetBackground(val)));

            _worldProps.Add(new ComboEditProperty<World, MusicName>("Music",
                world => world.Music,
                (world, val) => world.SetMusic(val), 7));

            _worldProps.Add(new CheckboxEditProperty<World>("Pipe Intro",
                world => world.IsPipeIntro,
                (world, val) => world.IsPipeIntro = val));
        }

        private void AddWarpPipeProps()
        {
            IEditPropertyGroup pipeProps = _entityPropsByType[typeof(WarpPipe)];
            pipeProps.Add(new InputTextEditProperty<WarpPipe>("Dest",
                pipe => pipe.Destination,
                (pipe, val) => pipe.Destination = val, 30)
            {
                IsDisabled = pipe => pipe.ReturnToSubworld
            });

            pipeProps.Add(new InputTextEditProperty<WarpPipe>("Tag",
                pipe => pipe.Tag,
                (pipe, val) => pipe.Tag = val, 10));

            pipeProps.Add(new ComboEditProperty<WarpPipe, PipeDirection>("Dir",
                pipe => pipe.Dir,
                (pipe, val) => pipe.Dir = val));

            pipeProps.Add(new CheckboxEditProperty<WarpPipe>("Return to Sub-world",
                pipe => pipe.ReturnToSubworld,
                (pipe, val) => pipe.ReturnToSubworld = val));
        }

        private void AddWarpExitProps()
        {
            _entityPropsByType[typeof(WarpExit)].Add(new InputTextEditProperty<WarpExit>("Tag",
                pipe => pipe.Tag,
                (pipe, val) => pipe.Tag = val, 10));
        }

        private void AddConstantPlatformProps()
        {
            IEditPropertyGroup plat = _entityPropsByType[typeof(ConstantPlatform)];
            plat.Add(new ComboEditProperty<ConstantPlatform, PlatformType>("Type",
                p => p.Type, (p, t) => p.Resize(t)));

            plat.Add(new ComboEditProperty<ConstantPlatform, PlatformDirection>("Direction",
                p => p.Dir, (p, d) => p.Dir = d));
        }

        private void AddPingPongPlatformProps()
        {
            IEditPropertyGroup plat = _entityPropsByType[typeof(PingPongPlatform)];
            plat.Add(new ComboEditProperty<PingPongPlatform, PlatformType>("Type",
                p => p.Type, (p, t) => p.Resize(t)));

            plat.Add(new InputFloatEditProperty<PingPongPlatform>("Tile Offset",
                p => p.TileOffset, (p, o) => p.TileOffset = o));

            plat.Add(new CheckboxEditProperty<PingPongPlatform>("Is Horizontal",
                p => p.IsHorizontal, (p, val) => p.IsHorizontal = val));
        }

        private void AddRedFlyingKoopaProps()
        {
            _entityPropsByType[typeof(RedFlyingKoopa)].Add(
                new InputFloatEditProperty<RedFlyingKoopa>("Tile Offset",
                k => k.TileOffset, (k, o) => k.TileOffset = o));
        }

        private void AddFirebarProps()
        {
            IEditPropertyGroup bar = _entityPropsByType[typeof(Firebar)];
            bar.Add(new ComboEditProperty<Firebar, FirebarLength>("Length",
                b => b.Length,
                (b, len) => b.SetLength(len)));

            bar.Add(new CheckboxEditProperty<Firebar>("Go Right",
                b => b.IsGoingRight,
                (b, val) => b.IsGoingRight = val));
        }

        public override void Update()
        {
            
        }

        protected override void OnRender()
        {
            WorldProperties();
            TileProperties();
            EntityProperties();
        }

        private void WorldProperties()
        {
            Gui.SeparatorText("World");
            Gui.Text("File: " + (Editor.FilePath == string.Empty ? "unnamed" : Path.GetFileName(Editor.FilePath)));
            Gui.Dummy(new Vector2(0f, 2f));
            
            Gui.Text($"Cols: {Editor.World.Cols}".PadRight(10));
            Gui.SameLine();
            if (Gui.Button("Change"))
            {
                _colsInputVal = Editor.World.Cols;
                Gui.OpenPopup("Change Columns");
            }

            HandleChangeColPopup();

            Gui.Text($"Rows: {World.Rows}".PadRight(10));
            Gui.SameLine();
            Helper.GuiDisabledButton("Change##2", true);

            _worldProps.Render(Editor.World);

            Gui.Dummy(new Vector2(0f, 20f));
        }

        private void HandleChangeColPopup()
        {
            Vector2 center = Gui.GetMainViewport().GetCenter();
            Gui.SetNextWindowPos(center, ImGuiCond.Appearing, new Vector2(0.5f));
            if (Gui.BeginPopupModal("Change Columns"))
            {
                Gui.InputInt("##1", ref _colsInputVal);
                if (Gui.Button("Apply"))
                {
                    Editor.World.SetCols(_colsInputVal);
                    Editor.Dirty();
                    Gui.CloseCurrentPopup();
                }

                Gui.SameLine();
                if (Gui.Button("Cancel")) Gui.CloseCurrentPopup();

                Gui.EndPopup();
            }
        }

        private void TileProperties()
        {
            if (Editor.ActiveSelector is not TileWindow tw)
                return;

            bool brick = tw.Selected is InstanceTile<BrickTile>;
            if (tw.Selected is InstanceTile<QuestionTile> || brick)
            {
                Gui.SeparatorText("Item Tile");

                Gui.Checkbox("Hidden", ref _ctHidden);

                string[] choices = Enum.GetNames<ItemType>();
                Gui.Combo("Item", ref _ctItemIndex, choices, choices.Length);

                if (brick)
                {
                    choices = Enum.GetNames<TilePalette>();
                    Gui.Combo("Palette", ref _brickTilePaletteIndex, choices, choices.Length);
                }

                Gui.Dummy(new Vector2(0f, 20f));
            }
        }

        private void EntityProperties()
        {
            Entity? selected = Editor.WorldWindow.SelectedEntity;
            if (selected is null)
                return;

            if (_entityPropsByType.TryGetValue(selected.GetType(), out IEditPropertyGroup? group))
                group.Render(selected);
        }
    }
}

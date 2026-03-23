namespace Mario2.Editor
{
    public class PropertyWindow : EditorWindow
    {
        private int _colsInputVal = 0;
        private int _bgVal;
        private int _musicVal;
        private bool _pipeIntro;

        public ItemType ContainerTileItem => (ItemType)_ctItemIndex;
        private int _ctItemIndex;

        public bool ContainerTileHidden => _ctHidden;
        private bool _ctHidden;

        public TilePalette BrickTilePalette => (TilePalette)_brickTilePaletteIndex;
        private int _brickTilePaletteIndex;

        private string _warpPipeDest = string.Empty;
        private string _warpPipeTag = string.Empty;
        private int _warpPipeDir;
        private bool _warpPipeReturn;

        private int _platformType;
        private int _platformDir;
        private bool _platformAxis;
        private float _platformOffset;

        private readonly EditPropertyGroup<World> _worldProps;
        private readonly EditPropertyGroup<object> _tileProps;
        private readonly Dictionary<Type, IEditPropertyGroup> _entityPropsByType;

        public PropertyWindow(EditorScene editor)
            : base(editor, "Properties")
        {
            _worldProps = new EditPropertyGroup<World>(string.Empty, Editor);
            _tileProps = new EditPropertyGroup<object>(string.Empty, Editor);

            _worldProps.Add(new ComboEditProperty<World, WorldBackground>("Background",
                world => world.Background,
                (world, val) => world.SetBackground(val)));

            _worldProps.Add(new ComboEditProperty<World, MusicName>("Music",
                world => world.Music,
                (world, val) => world.SetMusic(val), 7));

            _worldProps.Add(new CheckboxEditProperty<World>("Pipe Intro", 
                world => world.IsPipeIntro,
                (world, val) => world.IsPipeIntro = val));

            _entityPropsByType = new()
            {
                [typeof(WarpPipe)] = new EditPropertyGroup<WarpPipe>("Warp Pipe", Editor)
            };

            _entityPropsByType[typeof(WarpPipe)].Add(new InputTextEditProperty<WarpPipe>("Dest",
                pipe => pipe.Destination,
                (pipe, val) => pipe.Destination = val, 30));

            _entityPropsByType[typeof(WarpPipe)].Add(new InputTextEditProperty<WarpPipe>("Tag",
                pipe => pipe.Tag,
                (pipe, val) => pipe.Tag = val, 10));

            _entityPropsByType[typeof(WarpPipe)].Add(new ComboEditProperty<WarpPipe, PipeDirection>("Dir",
                pipe => pipe.Dir,
                (pipe, val) => pipe.Dir = val));

            _entityPropsByType[typeof(WarpPipe)].Add(new CheckboxEditProperty<WarpPipe>("Return to Sub-world",
                pipe => pipe.ReturnToSubworld,
                (pipe, val) => pipe.ReturnToSubworld = val));
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
            }

            Gui.Dummy(new Vector2(0f, 20f));
        }

        private void EntityProperties()
        {
            Entity? sel = Editor.WorldWindow.SelectedEntity;
            if (sel is null)
                return;

            if (_entityPropsByType.TryGetValue(sel.GetType(), out IEditPropertyGroup? group))
                group.Render(sel);

            //if (sel is WarpPipe pipe)
            //{
            //    Gui.SeparatorText("Warp Pipe");

            //    _warpPipeDest = pipe.Destination;
            //    if (pipe.ReturnToSubworld)
            //        Gui.BeginDisabled();

            //    if (Gui.InputText("Dest", ref _warpPipeDest, 30))
            //    {
            //        pipe.Destination = _warpPipeDest;
            //        Editor.Dirty();
            //    }

            //    if (pipe.ReturnToSubworld)
            //        Gui.EndDisabled();

            //    _warpPipeTag = pipe.Tag;
            //    if (Gui.InputText("Tag", ref _warpPipeTag, 10))
            //    {
            //        pipe.Tag = _warpPipeTag;
            //        Editor.Dirty();
            //    }

            //    _warpPipeDir = (int)pipe.Dir;
            //    string[] choices = Enum.GetNames<PipeDirection>();
            //    if (Gui.Combo("Dir", ref _warpPipeDir, choices, choices.Length))
            //    {
            //        pipe.Dir = (PipeDirection)_warpPipeDir;
            //        Editor.Dirty();
            //    }

            //    _warpPipeReturn = pipe.ReturnToSubworld;
            //    if (Gui.Checkbox("Return to Sub-world", ref _warpPipeReturn))
            //    {
            //        pipe.ReturnToSubworld = _warpPipeReturn;
            //        Editor.Dirty();
            //    }
            //}
            //else if (sel is WarpExit exit)
            //{
            //    Gui.SeparatorText("Pipe Exit");

            //    _warpPipeTag = exit.Tag;
            //    if (Gui.InputText("Tag", ref _warpPipeTag, 10))
            //    {
            //        exit.Tag = _warpPipeTag;
            //        Editor.Dirty();
            //    }
            //}
            //else if (sel is ConstantPlatform platform)
            //{
            //    Gui.SeparatorText("Platform - Constant");

            //    _platformType = (int)platform.Type;
            //    string[] choices = Enum.GetNames<PlatformType>();
            //    if (Gui.Combo("Size", ref _platformType, choices, choices.Length))
            //    {
            //        platform.Resize((PlatformType)_platformType);
            //        Editor.Dirty();
            //    }

            //    _platformDir = (int)platform.Dir;
            //    choices = Enum.GetNames<PlatformDirection>();
            //    if (Gui.Combo("Direction", ref _platformDir, choices, choices.Length))
            //    {
            //        platform.Dir = (PlatformDirection)_platformDir;
            //        Editor.Dirty();
            //    }
            //}
            //else if (sel is PingPongPlatform platform2)
            //{
            //    Gui.SeparatorText("Platform - PingPong");

            //    _platformType = (int)platform2.Type;
            //    string[] choices = Enum.GetNames<PlatformType>();
            //    if (Gui.Combo("Size", ref _platformType, choices, choices.Length))
            //    {
            //        platform2.Resize((PlatformType)_platformType);
            //        Editor.Dirty();
            //    }

            //    _platformOffset = platform2.TileOffset;
            //    if (Gui.InputFloat("Tile Offset", ref _platformOffset, 0.5f, 0.5f, "%.1f"))
            //    {
            //        platform2.TileOffset = _platformOffset;
            //        Editor.Dirty();
            //    }

            //    _platformAxis = platform2.IsHorizontal;
            //    if (Gui.Checkbox("Horizontal", ref _platformAxis))
            //    {
            //        platform2.IsHorizontal = _platformAxis;
            //        Editor.Dirty();
            //    }
            //}
        }
    }
}

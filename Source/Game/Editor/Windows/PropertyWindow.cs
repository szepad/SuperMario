namespace Mario2.Editor
{
    public class PropertyWindow(EditorScene editor)
        : EditorWindow(editor, "Properties")
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

        public override void Update()
        {
            
        }

        public void ReadSettings()
        {
            _bgVal = (int)Editor.World.Background;
            _musicVal = (int)Editor.World.Music;
        }

        public void ClearSettings()
        {
            _bgVal = 0;
            _musicVal = 0;
        }

        protected override void OnRender()
        {
            WorldProperties();
            TileProperties();
            EntityProperties();

            // Spacing
            Gui.Dummy(new Vector2(0f, 20f));
        }

        private void WorldProperties()
        {
            Gui.SeparatorText("World");
            Gui.Text(Editor.FilePath == string.Empty ? "unnamed" : Path.GetFileName(Editor.FilePath));
            Gui.Dummy(new Vector2(0f, 2f));
            
            // Cols
            Gui.Text($"Cols: {Editor.World.Cols}");
            Gui.SameLine();
            if (Gui.Button("Change"))
            {
                _colsInputVal = Editor.World.Cols;
                Gui.OpenPopup("Change Columns");
            }

            ChangeColPopup();

            // Rows
            Gui.Text($"Rows: {World.Rows}");
            Gui.SameLine();
            Gui.BeginDisabled();
            Gui.Button("Change##2");
            Gui.EndDisabled();

            // Background
            string[] bgChoices = Enum.GetNames<WorldBackground>();
            Gui.Text("Background");
            Gui.SetNextItemWidth(110f);
            Gui.SameLine();
            if (Gui.Combo("##combo_bg", ref _bgVal, bgChoices, bgChoices.Length))
            {
                Editor.World.SetBackground((WorldBackground)_bgVal);
                Editor.Dirty();
            }

            // Music
            string[] musicChoices = ["None", "Overworld", "Underground", "Castle", "Water", "Bonus", "Underground Intro"];
            Gui.Text("Music");
            Gui.SetNextItemWidth(110f);
            Gui.SameLine();
            if (Gui.Combo("##combo_music", ref _musicVal, musicChoices, musicChoices.Length))
            {
                Editor.World.SetMusic((MusicName)_musicVal);
                Editor.Dirty();
            }

            // Pipe Intro
            _pipeIntro = Editor.World.IsPipeIntro;
            if (Gui.Checkbox("Pipe Intro", ref _pipeIntro))
            {
                Editor.World.IsPipeIntro = _pipeIntro;
                Editor.Dirty();
            }

            // Spacing
            Gui.Dummy(new Vector2(0f, 20f));
        }

        private void ChangeColPopup()
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
        }

        private void EntityProperties()
        {
            Entity? sel = Editor.WorldWindow.SelectedEntity;
            if (sel is null)
                return;

            if (sel is WarpPipe pipe)
            {
                Gui.SeparatorText("Warp Pipe");

                _warpPipeDest = pipe.Destination;
                if (pipe.ReturnToSubworld)
                    Gui.BeginDisabled();

                if (Gui.InputText("Dest", ref _warpPipeDest, 30))
                {
                    pipe.Destination = _warpPipeDest;
                    Editor.Dirty();
                }

                if (pipe.ReturnToSubworld)
                    Gui.EndDisabled();

                _warpPipeTag = pipe.Tag;
                if (Gui.InputText("Tag", ref _warpPipeTag, 10))
                {
                    pipe.Tag = _warpPipeTag;
                    Editor.Dirty();
                }

                _warpPipeDir = (int)pipe.Dir;
                string[] choices = Enum.GetNames<PipeDirection>();
                if (Gui.Combo("Dir", ref _warpPipeDir, choices, choices.Length))
                {
                    pipe.Dir = (PipeDirection)_warpPipeDir;
                    Editor.Dirty();
                }

                _warpPipeReturn = pipe.ReturnToSubworld;
                if (Gui.Checkbox("Return to Sub-world", ref _warpPipeReturn))
                {
                    pipe.ReturnToSubworld = _warpPipeReturn;
                    Editor.Dirty();
                }
            }
            else if (sel is WarpExit exit)
            {
                Gui.SeparatorText("Pipe Exit");

                _warpPipeTag = exit.Tag;
                if (Gui.InputText("Tag", ref _warpPipeTag, 10))
                {
                    exit.Tag = _warpPipeTag;
                    Editor.Dirty();
                }
            }
            else if (sel is ConstantPlatform platform)
            {
                Gui.SeparatorText("Platform - Constant");

                _platformType = (int)platform.Type;
                string[] choices = Enum.GetNames<PlatformType>();
                if (Gui.Combo("Size", ref _platformType, choices, choices.Length))
                {
                    platform.Resize((PlatformType)_platformType);
                    Editor.Dirty();
                }

                _platformDir = (int)platform.Dir;
                choices = Enum.GetNames<PlatformDirection>();
                if (Gui.Combo("Direction", ref _platformDir, choices, choices.Length))
                {
                    platform.Dir = (PlatformDirection)_platformDir;
                    Editor.Dirty();
                }
            }
            else if (sel is PingPongPlatform platform2)
            {
                Gui.SeparatorText("Platform - PingPong");

                _platformType = (int)platform2.Type;
                string[] choices = Enum.GetNames<PlatformType>();
                if (Gui.Combo("Size", ref _platformType, choices, choices.Length))
                {
                    platform2.Resize((PlatformType)_platformType);
                    Editor.Dirty();
                }

                _platformOffset = platform2.TileOffset;
                if (Gui.InputFloat("Tile Offset", ref _platformOffset, 0.5f, 0.5f, "%.1f"))
                {
                    platform2.TileOffset = _platformOffset;
                    Editor.Dirty();
                }

                _platformAxis = platform2.IsHorizontal;
                if (Gui.Checkbox("Horizontal", ref _platformAxis))
                {
                    platform2.IsHorizontal = _platformAxis;
                    Editor.Dirty();
                }
            }
        }
    }
}

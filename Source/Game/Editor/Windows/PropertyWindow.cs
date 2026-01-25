namespace Mario2.Editor
{
    public class PropertyWindow(EditorScene editor) : EditorWindow(editor, "Properties")
    {
        private int _colsInputVal = 0;
        private int _bgVal;
        private int _musicVal;

        public ItemType ContainerTileItem => (ItemType)_ctItemIndex;
        private int _ctItemIndex;

        public bool ContainerTileHidden => _ctHidden;
        private bool _ctHidden;

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
            string[] bgChoices = ["Sky", "Underground", "Castle", "Water", "Night"];
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
            if (Editor.ActiveSelector is TileWindow tw
                && (tw.Selected is InstanceTile<QuestionTile> || tw.Selected is InstanceTile<BrickTile>))
            {
                Gui.SeparatorText("Tile");

                Gui.Checkbox("Hidden", ref _ctHidden);

                string[] choices = Enum.GetNames<ItemType>();
                Gui.Combo("Item", ref _ctItemIndex, choices, choices.Length);
                
                // Spacing
                Gui.Dummy(new Vector2(0f, 20f));
            }
        }
    }
}

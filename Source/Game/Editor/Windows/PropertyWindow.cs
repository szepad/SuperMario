namespace Mario2.Editor
{
    public class PropertyWindow(EditorScene editor) : EditorWindow(editor, "Properties")
    {
        public bool IsTileCollidable => _tileCollidableVal;

        private int _colsInputVal = 0;
        private int _bgVal = (int)editor.World.Background;
        private bool _tileCollidableVal = true;

        public override void Update()
        {
            
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
            string[] choices = ["Sky", "Underground", "Castle", "Water", "Night"];
            Gui.Text("Background");
            Gui.SetNextItemWidth(110f);
            Gui.SameLine();
            if (Gui.Combo("##combo_bg", ref _bgVal, choices, choices.Length))
            {
                Editor.World.Background = (WorldBackground)_bgVal;
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
            Gui.SeparatorText("Tile");
            Gui.Checkbox("Collidable", ref _tileCollidableVal);

            // Spacing
            Gui.Dummy(new Vector2(0f, 20f));
        }
    }
}

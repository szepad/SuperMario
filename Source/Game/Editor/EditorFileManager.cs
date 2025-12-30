using NativeFileDialogSharp;

namespace Mario2.Editor
{
    public class EditorFileManager(EditorScene editor)
    {
        public EditorScene Editor { get; } = editor;

        public void New()
        {
            if (Editor.IsFileDirty)
            {
                Editor.AskSave(DoNew);
            }
            else
            {
                DoNew();
            }
        }

        private void DoNew()
        {
            Editor.World.SetCols(100);
            Editor.World.Clear();
            Editor.World.Background = WorldBackground.Sky;
            Editor.IsFileDirty = false;
        }

        public void Open()
        {
            DialogResult result = Dialog.FileOpen("lvl");
            if (result.IsOk)
            {
                // Deserialize()
                Editor.WorldWindow.AssignRuleTileRefs();
            }
        }

        public void Save()
        {
            if (Editor.FileName == string.Empty)
            {
                SaveAs();
            }
            else
            {
                // Serialize()
                Editor.IsFileDirty = false;
            }
        }

        public void SaveAs()
        {
            DialogResult result = Dialog.FileSave("lvl");
            if (result.IsOk)
            {
                // Serialize()
                Editor.IsFileDirty = false;
            }
        }
    }
}

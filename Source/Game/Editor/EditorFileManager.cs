using NativeFileDialogSharp;

namespace Mario2.Editor
{
    public class EditorFileManager(EditorScene editor)
    {
        public EditorScene Editor { get; } = editor;

        public void New()
        {
            Editor.AskSave(DoNew);
        }

        private void DoNew()
        {
            Editor.World = new();
            Editor.FilePath = string.Empty;
            Editor.CleanDirty();
        }

        public void Open()
        {
            DialogResult result = Dialog.FileOpen("lvl");
            if (result.IsOk)
            {
                Editor.AskSave(() => DoOpen(result.Path));
            }
        }

        private void DoOpen(string path)
        {
            World? world = WorldLoader.Deserialize(path, out Exception? failEx);
            if (world is not null)
            {
                Editor.World = world;
                Editor.WorldWindow.AssignRuleTileRefs();
                Editor.FilePath = path;
                Editor.CleanDirty();
            }
            else if (failEx is not null)
            {
                Editor.ShowError(string.Join("\n", $"'{path}' is corrupted.", failEx.Message, failEx.StackTrace));
            }
        }

        public bool Save()
        {
            if (Editor.FilePath == string.Empty)
            {
                return SaveAs();
            }
            else
            {
                WorldLoader.Serialize(Editor.World, Editor.FilePath);
                Editor.CleanDirty();
                return true;
            }
        }

        public bool SaveAs()
        {
            DialogResult result = Dialog.FileSave("lvl");
            if (result.IsOk)
            {
                Editor.FilePath = result.Path;
                return Save();
            }

            return false;
        }
    }
}

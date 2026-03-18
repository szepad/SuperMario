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
            Editor.PropertyWindow.ClearSettings();
            Editor.CleanDirty();
            Editor.WorldWindow.FocusCameraOn(Editor.World.Player);
            Editor.WorldWindow.DeselectEntity();
        }

        public void AskOpen()
        {
            DialogResult result = Dialog.FileOpen("lvl");
            if (result.IsOk)
            {
                Editor.AskSave(() => Open(result.Path));
            }
        }

        public void Open(string path)
        {
            World? world = WorldLoader.Deserialize(path, out Exception? failEx);
            if (world is not null)
            {
                Editor.World = world;
                Editor.WorldWindow.AssignRuleTileRefs();
                Editor.PropertyWindow.ReadSettings();
                Editor.FilePath = path;
                EditorScene.RecentlyOpened = path;
                Editor.CleanDirty();
                Editor.WorldWindow.FocusCameraOn(world.Player);
                Editor.WorldWindow.DeselectEntity();
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

//using System.Windows.Forms;

namespace Mario2.Editor
{
    public class EditorMenuBarAction(EditorScene editor)
    {
        public EditorScene Editor { get; } = editor;

        private const uint InputTextMaxLen = 128;
        private string _newName = string.Empty;

        // TODO: Remake with winforms' native file dialog
        public void New(string id)
        {
            if (Popup(id, "Save", () =>
            {
                Gui.InputText("Name", ref _newName, InputTextMaxLen);
            }))
            {
                Editor.World.SetCols(100);
                Editor.World.Clear();
            }
        }

        public void Open(string id)
        {

        }

        public void Save(string id)
        {

        }

        public void SaveAs(string id)
        {

        }

        private static bool Popup(string id, string okText, Action logic)
        {
            bool ret = false;
            Vector2 center = Gui.GetMainViewport().GetCenter();
            Gui.SetNextWindowPos(center, ImGuiCond.Appearing, new Vector2(0.5f));
            if (Gui.BeginPopupModal(id))
            {
                logic();

                if (Gui.Button(okText))
                {
                    Gui.CloseCurrentPopup();
                    ret = true;
                }
                
                Gui.SameLine();
                if (Gui.Button("Cancel")) 
                    Gui.CloseCurrentPopup();

                Gui.EndPopup();
            }

            return ret;
        }
    }
}

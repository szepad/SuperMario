namespace Mario2.Editor
{
    public class EditorMenuBar(EditorScene editor)
    {
        public EditorScene Editor { get; } = editor;

        public void Render()
        {
            Gui.BeginMainMenuBar();

            Menu("File", FileMenu);
            Menu("View", ViewMenu);

            Gui.EndMainMenuBar();
        }

        private void FileMenu()
        {
            Item("New", "Ctrl+N", Editor.FileManager.New);
            Item("Open", "Ctrl+O", Editor.FileManager.AskOpen);
            Item("Save", "Ctrl+S", () => Editor.FileManager.Save());
            Item("Save As", "Ctrl+Shift+S", () => Editor.FileManager.SaveAs());

            Gui.Separator();
            Item("Exit", "Escape", Editor.QueueTitleScene);
        }

        private void ViewMenu()
        {
            Item("Show Grid", ref Editor.Options.ShowGrid);

            Gui.SeparatorText("Layout Presets");
            Item("Default", () => Editor.QueueLoadLayout(EditorScene.DefaultLayout));
        }

        private static void Menu(string label, Action items)
        {
            if (Gui.BeginMenu(label))
            {
                items?.Invoke();
                Gui.EndMenu();
            }
        }

        private static void Item(string label, Action? action) => Item(label, string.Empty, action);
        private static void Item(string label, string shortcut, Action? action)
        {
            if (Gui.MenuItem(label, shortcut))
            {
                action?.Invoke();
            }
        }

        private static void Item(string label, ref bool selected) => Item(label, string.Empty, ref selected);
        private static void Item(string label, string shortcut, ref bool selected)
        {
            Gui.MenuItem(label, shortcut, ref selected);
        }
    }
}

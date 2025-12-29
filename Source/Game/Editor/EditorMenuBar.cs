namespace Mario2.Editor
{
    public class EditorMenuBar(EditorScene editor)
    {
        public EditorScene Editor { get; } = editor;
        private readonly EditorMenuBarAction _actions = new(editor);

        private string _popupId = string.Empty;
        private bool _popupOpened;
        private Action? _popupLogic;

        public void Render()
        {
            Gui.BeginMainMenuBar();

            Menu("File", FileMenu);
            Menu("View", ViewMenu);

            Gui.EndMainMenuBar();

            HandlePopupLogic();
        }

        private void FileMenu()
        {
            Item("New", "Ctrl+N", () => Popup("New", _actions.New));
            Item("Open", "Ctrl+O", () => Popup("Open", _actions.Open));
            Item("Save", "Ctrl+S", () => Popup("Save", _actions.Save));
            Item("Save As", "Ctrl+Shift+S", () => Popup("Save As", _actions.SaveAs));

            Gui.Separator();
            Item("Back", "Escape", Editor.QueueTitleScene);
            Item("Exit", "Alt+F4", Game.Instance.Exit);
        }

        private void ViewMenu()
        {
            Item("Show Grid", ref Editor.Options.ShowGrid);

            Gui.SeparatorText("Layout Presets");
            Item("Default", () => Editor.QueueLoadLayout(EditorScene.DefaultLayout));
        }

        private void HandlePopupLogic()
        {
            if (_popupOpened)
            {
                Gui.OpenPopup(_popupId);
                _popupOpened = false;
            }

            _popupLogic?.Invoke();
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
        
        private void Popup(string id, Action<string> popupLogic)
        {
            _popupOpened = true;
            _popupId = id;
            _popupLogic = () => popupLogic(id);
        }
    }
}

namespace Mario2.Editor
{
    public abstract class EditorWindow(EditorScene editor, string name)
    {
        public EditorScene Editor { get; } = editor;
        public bool IsBeingHovered { get; private set; }

        protected string Name { get; set; } = name;
        protected ImGuiWindowFlags Flags { get; set; } = ImGuiWindowFlags.None;

        public abstract void Update();

        public void Render()
        {
            Gui.Begin(Name, Flags);
            IsBeingHovered = Gui.IsWindowHovered();
            OnRender();
            Gui.End();
        }

        protected abstract void OnRender();
    }
}

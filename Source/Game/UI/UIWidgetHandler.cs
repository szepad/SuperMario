namespace Mario2
{
    public class UIWidgetHandler
    {
        public IEnumerable<UIWidget> Widgets => _widgets;
        private readonly List<UIWidget> _widgets = [];

        public UIWidget? Selected => (_selectedIndex >= 0 && _selectedIndex < _widgets.Count)
                ? _widgets[_selectedIndex] : null;

        private int _selectedIndex;

        public UIWidget? this[Index index]
        {
            get => (index.Value >= 0 && index.Value < _widgets.Count)
                ? _widgets[index]
                : null;
        }

        public UIWidget Add(UIWidget button)
        {
            _widgets.Add(button);
            return button;
        }

        public void PlaceVertically(Vector2 start, float gap = UIWidget.DefaultGap)
        {
            if (_widgets.Count == 0)
                return;

            _widgets[0].Position = start;
            for (int i = 1; i < _widgets.Count; i++)
            {
                UIWidget button = _widgets[i];
                button.Position = new Vector2(start.X, 
                    start.Y + i * (gap + Text.FontSize));
            }
        }

        public void Update()
        {
            if (_widgets.Count == 0)
                return;

            if (Raylib.IsKeyPressed(KeyboardKey.Up))
            {
                MoveCursorUp();   
            }

            if (Raylib.IsKeyPressed(KeyboardKey.Down))
            {
                MoveCursorDown();
            }

            this[_selectedIndex]?.Update();
        }

        public void MoveCursorUp()
        {
            _selectedIndex--;
            if (_selectedIndex < 0)
                _selectedIndex = _widgets.Count - 1;

            if (Selected is not null && Selected.IsGreyedOut
                && _widgets.Any(w => !w.IsGreyedOut))
                MoveCursorUp();
        }

        public void MoveCursorDown()
        {
            _selectedIndex++;
            if (_selectedIndex >= _widgets.Count)
                _selectedIndex = 0;

            if (Selected is not null && Selected.IsGreyedOut
                && _widgets.Any(w => !w.IsGreyedOut))
                MoveCursorDown();
        }

        public void Draw()
        {
            for (int i = 0; i < _widgets.Count; i++)
            {
                UIWidget widget = _widgets[i];
                widget.Draw();
                if (_selectedIndex == i)
                {
                    widget.DrawCursor();
                }
            }
        }
    }
}

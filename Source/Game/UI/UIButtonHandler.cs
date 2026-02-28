namespace Mario2
{
    public class UIButtonHandler
    {
        public IEnumerable<UIButton> Buttons => _buttons;
        private readonly List<UIButton> _buttons = [];

        private int _selectedIndex;

        public void Add(UIButton button)
        {
            _buttons.Add(button);
        }

        public void PlaceVertically(Vector2 start, float gap)
        {
            if (_buttons.Count == 0)
                return;

            _buttons[0].Position = start;
            for (int i = 1; i < _buttons.Count; i++)
            {
                UIButton button = _buttons[i];
                button.Position = new Vector2(start.X, 
                    start.Y + i * (gap + Text.FontSize));
            }
        }

        public void Update()
        {
            if (Raylib.IsKeyPressed(KeyboardKey.Up))
            {
                _selectedIndex--;
                if (_selectedIndex < 0)
                    _selectedIndex = _buttons.Count - 1;
            }

            if (Raylib.IsKeyPressed(KeyboardKey.Down))
            {
                _selectedIndex++;
                if (_selectedIndex >= _buttons.Count)
                    _selectedIndex = 0;
            }

            if (Raylib.IsKeyPressed(KeyboardKey.Enter)
                && _selectedIndex >= 0 && _selectedIndex < _buttons.Count)
            {
                UIButton button = _buttons[_selectedIndex];
                button.OnSelect?.Invoke(button);
            }
        }

        public void Draw()
        {
            for (int i = 0; i < _buttons.Count; i++)
            {
                UIButton button = _buttons[i];
                Text.Draw(button.Label, button.Position, button.Pivot);
                if (_selectedIndex == i)
                {
                    const float MarkerSpacing = 7f * Game.TextureScale;
                    Vector2 offset = new(Text.GetWidth(button.Label) / 2f + MarkerSpacing, 0f);
                    Raylib.DrawTextureEx(Assets.GetTex("cursor"),
                        button.Position - offset - new Vector2(4f).Scale(),
                        0f, Game.TextureScale, Color.White);
                }
            }
        }
    }
}

namespace Mario2
{
    public sealed class UIRangeButton : UIWidget
    {
        public event Action<UIRangeButton>? OnValueChanged;

        public float Min { get; private set; }
        public float Max { get; private set; }
        public float Vel { get; private set; }
        public float Value { get; private set; }

        public UIRangeButton(string label, Action<UIRangeButton>? onValueChanged,
            float min = 0f, float max = 1f, float value = 0f, float vel = 0.5f) : base(label, Vector2.Zero)
        {
            Min = min;
            Max = max;
            Vel = vel;
            Value = value;

            OnValueChanged += onValueChanged;
        }

        public override void Update()
        {
            if (Raylib.IsKeyDown(KeyboardKey.Left))
            {
                Value -= Vel * Time.DeltaTime;
                if (Value < Min)
                    Value = Min;

                OnValueChanged?.Invoke(this);
            }

            if (Raylib.IsKeyDown(KeyboardKey.Right))
            {
                Value += Vel * Time.DeltaTime;
                if (Value > Max)
                    Value = Max;

                OnValueChanged?.Invoke(this);
            }
        }

        public override void DrawCursor()
        {
            Raylib.DrawTextureEx(Assets.GetTex("cursor"),
                Position - MarkerOffset - new Vector2(4f).Scale(),
                0f, Game.TextureScale, Color.White);

            Raylib.DrawTextureEx(Assets.GetTex("cursor"),
                Position + MarkerOffset - new Vector2(4f).Scale(),
                0f, Game.TextureScale, Color.White);
        }
    }
}

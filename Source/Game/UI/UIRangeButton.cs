namespace Mario2
{
    public sealed record UIRangeButton(string Label, Action<UIRangeButton>? OnValueChanged,
        float Min = 0f, float Max = 1f, float Curr = 0f, float Vel = 0.5f)
        : UIWidget(Label, Vector2.Zero)
    {
        public float Value { get; private set; } = Curr;

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
            Raylib.DrawCircleV(Position - MarkerOffset, 2f.Scale(), Color.Red);
            Raylib.DrawCircleV(Position + MarkerOffset, 2f.Scale(), Color.Red);
        }
    }
}

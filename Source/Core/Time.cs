namespace Mario2
{
    public static class Time
    {
        public static float TimeScale { get; set; } = 1f;

        public static float UnscaledDelta { get; private set; }
        public static float Delta { get; private set; }

        public static void Progress()
        {
            UnscaledDelta = Raylib.GetFrameTime();
            Delta = UnscaledDelta * TimeScale;
        }
    }
}

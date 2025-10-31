namespace Mario2
{
    public static class Time
    {
        public static float TimeScale { get; set; } = 1f;

        public static float UnscaledDeltaTime { get; private set; }
        public static float DeltaTime { get; private set; }

        public static void Progress()
        {
            UnscaledDeltaTime = Raylib.GetFrameTime();
            DeltaTime = UnscaledDeltaTime * TimeScale;
        }
    }
}

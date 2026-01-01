namespace Mario2
{
    public static class Time
    {
        public static float DebugTimeScale { get; set; } = 1f;
        public static float TimeScale { get; set; } = 1f;

        public static float UnscaledDeltaTime { get; private set; }
        public static float DeltaTime { get; private set; }

        // It won't go above this threshold anyway under normal circumstances,
        // but it can for example: if you grab the window scaler for some
        // time, it will go above and mess up the physics, that's why we cap it.
        private const float MaxDeltaTimePerFrame = 0.1f;

        public static void Progress()
        {
            UnscaledDeltaTime = Raylib.GetFrameTime() * DebugTimeScale;
            if (UnscaledDeltaTime > MaxDeltaTimePerFrame)
            {
                UnscaledDeltaTime = MaxDeltaTimePerFrame;
            }

            DeltaTime = UnscaledDeltaTime * TimeScale;
        }
    }
}

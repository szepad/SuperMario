namespace Mario2
{
    public enum SessionLoadResult
    {
        Success,
        ReachedEnd,
        Fail
    }

    public static class SessionManager
    {
        public static int CurrentLevel { get; private set; } = 0;
        public static int LevelCount { get; private set; } = 32;

        public static void Reset()
        {
            CurrentLevel = 0;
        }

        public static SessionLoadResult LoadNext(out World world)
        {
            CurrentLevel++;
            if (CurrentLevel > LevelCount)
            {
                world = new World();
                return SessionLoadResult.ReachedEnd;
            }

            return LoadCurrent(out world);
        }

        public static SessionLoadResult LoadCurrent(out World world)
        {
            world = new World();
            string name = (CurrentLevel < 10 ? "0" : "") + $"{CurrentLevel}.lvl";

            World? loaded = WorldLoader.Deserialize(Path.Combine("levels", name), out _);
            if (loaded is null)
            {
                return SessionLoadResult.Fail;
            }
            else
            {
                world = loaded;
                return SessionLoadResult.Success;
            }
        }
    }
}

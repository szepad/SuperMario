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
        // Campaign data
        public static int LevelCount { get; private set; } = 32;
        public static int LevelsPerWorld { get; private set; } = 4;

        // Session data
        public static int CurrentLevel { get; private set; } = 0;
        public static int Score { get; private set; }
        public static int Coins { get; private set; }
        
        public static int CurrentWorld => (CurrentLevel - 1) / LevelsPerWorld + 1;
        public static int CurrentLevelDisplay => ((CurrentLevel - 1) % LevelsPerWorld) + 1;

        public static void Reset()
        {
            CurrentLevel = 0;
            Score = 0;
            Coins = 0;
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
            string name = $"{CurrentLevel.ToString().PadLeft(2, '0')}.lvl";

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

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
        public static bool ReachedMidway { get; private set; }
        public static int CurrentLevel { get; private set; } = 0;
        public static int Lives { get; private set; }
        public static int Score { get; private set; }
        public static int Coins { get; private set; }
        public static bool Reloaded { get; private set; }
        
        public static int CurrentWorld => (CurrentLevel - 1) / LevelsPerWorld + 1;
        public static int CurrentLevelDisplay => ((CurrentLevel - 1) % LevelsPerWorld) + 1;

        private const int CoinCap = 99;

        public static void Reset()
        {
            CurrentLevel = 0;
            Lives = 5;
            Score = 0;
            Coins = 0;
            ReachedMidway = false;
            Reloaded = false;
        }

        public static SessionLoadResult LoadNext(out World world)
        {
            CurrentLevel++;
            Reloaded = false;
            ReachedMidway = false;
            if (CurrentLevel > LevelCount)
            {
                world = new World();
                return SessionLoadResult.ReachedEnd;
            }

            return LoadCurrent(out world);
        }

        public static SessionLoadResult Reload(out World world)
        {
            Reloaded = true;
            return LoadCurrent(out world);
        }

        private static SessionLoadResult LoadCurrent(out World world)
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

        public static void GetCoin()
        {
            Coins++;
            if (Coins > CoinCap)
            {
                Coins = 0;
                GetLife();
            }

            SoundManager.PlaySound(Sounds.CoinCollect);
        }

        public static void GetLife()
        {
            Lives++;
            SoundManager.PlaySound(Sounds.ExtraLife);
        }

        public static void LoseLife()
        {
            Lives--;
        }
        
        public static void GetScoreWithoutText(int points)
        {
            Score += points;
        }

        public static void GetScore(World world, int points, Vector2 pos, 
            float lifeTime = ScoreRewards.NormalLifeTime,
            float speed = ScoreRewards.NormalSpeed)
        {
            world.SpawnEntity(new ScoreText(world, points, pos, lifeTime, speed));
            if (points == ScoreRewards.OneUp)
            {
                GetLife();
            }
            else
            {
                Score += points;
            }
        }

        public static void ReachMid()
            => ReachedMidway = true;

        public static void ClearMid()
            => ReachedMidway = false;
    }
}

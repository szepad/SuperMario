namespace Mario2
{
    public static class ScoreRewards
    {
        public static int EnemyStompCount { get; set; } = 0;

        public const float NormalSpeed = Tile.Size * 1.75f;
        public const float FasterSpeed = Tile.Size * 3f;

        public const float NormalLifeTime = 0.75f;
        public const float ShorterLifeTime = 0.5f;

        public const int OneUp = 10000;
        public const int PowerUp = 1000;
        public const int BrickBreak = 50;
        public const int StarKill = 100;
        public const int TileBounceKill = 100;
        public const int Coin = 200;
        public const int Fireball = 200;
        public const int Kick = 400;
        public const int Time = 50;
        public const int Firework = 500;
        public const int Lakitu = 800;
        public const int Bowser = 5000;

        public static int GetEnemyStompScore() => EnemyStompCount switch
        {
            0 => 100,
            1 => 200,
            2 => 400,
            3 => 500,
            4 => 800,
            5 => 1000,
            6 => 2000,
            7 => 4000,
            8 => 5000,
            9 => 8000,
            _ => OneUp,
        };

        public static int GetShellKillScore(int count) => count switch
        {
            0 => 500,
            1 => 800,
            2 => 1000,
            3 => 2000,
            4 => 4000,
            5 => 5000,
            6 => 8000,
            _ => OneUp,
        };

        //public static int GetFlagpoleScore(Player player, Flagpole flagpole)
        //{
        //    float feetY = player.Position.Y + player.Size.Y;
        //    float flagpoleY = flagpole.Position.Y;
        //    float flagpoleHeight = flagpole.Size.Y;
        //    float normalizedY = feetY - flagpoleY;

        //    if (normalizedY <= flagpoleHeight / 6f)
        //    {
        //        return 5000;
        //    }
        //    else if (normalizedY <= flagpoleHeight / 5f * 2f)
        //    {
        //        return 2000;
        //    }
        //    else if (normalizedY <= flagpoleHeight / 5f * 3f)
        //    {
        //        return 800;
        //    }
        //    else if (normalizedY <= flagpoleHeight / 5f * 4f)
        //    {
        //        return 400;
        //    }
        //    else if (normalizedY <= flagpoleHeight)
        //    {
        //        return 100;
        //    }

        //    return 0;
        //}
    }
}

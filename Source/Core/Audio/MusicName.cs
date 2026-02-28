namespace Mario2
{
    public enum MusicName
    {
        None = 0,

        // World music (values matter as they're saved in file)
        Overworld,
        Underground,
        Castle,
        Water,
        Bonus,
        UndergroundIntro,

        OverworldHurry,
        UndergroundHurry,
        CastleHurry,
        WaterHurry,
        BonusHurry,

        // Event based
        Invincible,
        Bowser,
        BowserFinal,
        LevelClear,
        WorldClear,

        // Not gameplay
        Title,
        LostLevelsTitle,
        LostLevelsEnding,
        Ending,
        GameOver,
    }
}

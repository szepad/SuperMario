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

        // Event based
        Invincible,
        Warning,
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

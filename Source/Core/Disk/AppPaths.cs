namespace Mario2;

public static class AppPaths
{
    public static readonly string ResFolder = Path.Combine(AppContext.BaseDirectory, "res");
    public static readonly string MusicFolder = Path.Combine(ResFolder, "music");
    public static readonly string DefaultLevelsFolder = Path.Combine(AppContext.BaseDirectory, "levels");

    private static readonly string MacOSDataFolder = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
        "Library", "Application Support", "Mario2");

    public static readonly string DataFolder = OperatingSystem.IsMacOS()
        ? MacOSDataFolder
        : AppContext.BaseDirectory;

    public static readonly string SettingsFilePath = Path.Combine(DataFolder, "settings.cfg");

    static AppPaths()
    {
        if (OperatingSystem.IsMacOS())
        {
            if (!Directory.Exists(MacOSDataFolder))
                Directory.CreateDirectory(MacOSDataFolder);
        }
    }
}
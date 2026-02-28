namespace Mario2
{
    public static class GameSettingsLoader
    {
        public static string SettingsFilePath { get; set; } = "settings.cfg";
        public const string SettingsFileVersion = "ver 1.0";

        public static void Save(GameSettings settings)
        {
            try
            {
                using StreamWriter writer = new(SettingsFilePath);

                writer.WriteLine(SettingsFileVersion);
                writer.WriteLine(settings.IsFullscreen);
                writer.WriteLine(settings.IsVsync);
                writer.WriteLine(settings.TargetFps);
                writer.WriteLine((int)(Raylib.GetMasterVolume() * 100f));
                writer.WriteLine(settings.IsIntegerScale);
                writer.WriteLine((int)(settings.SoundVolume * 100f));
                writer.WriteLine((int)(settings.MusicVolume * 100f));
                writer.WriteLine(EditorScene.RecentlyOpened);
            }
            catch (Exception error)
            {
                Raylib.TraceLog(TraceLogLevel.Error, error.Message);
                Raylib.TraceLog(TraceLogLevel.Error, error.StackTrace);
            }
        }

        public static void Load(GameSettings settings)
        {
            if (!File.Exists(SettingsFilePath))
            {
                string path = Path.GetFullPath(SettingsFilePath);
                Raylib.TraceLog(TraceLogLevel.Warning, $"[Load] Settings file does not exist at '{path}'");

                UseDefaultSettings(settings);

                return;
            }

            try
            {
                using StreamReader reader = new(SettingsFilePath);

                string version = reader.ReadLine()!;
                if (version != SettingsFileVersion)
                {
                    throw new FileLoadException("Invalid version.");
                }

                settings.SetFullscreen(Convert.ToBoolean(reader.ReadLine()));
                settings.SetVsync(Convert.ToBoolean(reader.ReadLine()));
                settings.SetFps(Convert.ToInt32(reader.ReadLine()));
                Raylib.SetMasterVolume(Convert.ToInt32(reader.ReadLine()) / 100f);
                settings.SetIntegerScale(Convert.ToBoolean(reader.ReadLine()));
                settings.SetSoundVolume(Convert.ToInt32(reader.ReadLine()) / 100f);
                settings.SetMusicVolume(Convert.ToInt32(reader.ReadLine()) / 100f);
                EditorScene.RecentlyOpened = reader.ReadLine() ?? string.Empty;
            }
            catch (Exception error)
            {
                Raylib.TraceLog(TraceLogLevel.Error, "[Load] Settings file corrupted.");
                Raylib.TraceLog(TraceLogLevel.Error, error.Message);
                Raylib.TraceLog(TraceLogLevel.Error, error.StackTrace);

                UseDefaultSettings(settings);
            }
        }

        public static void UseDefaultSettings(GameSettings settings)
        {
            Raylib.TraceLog(TraceLogLevel.Trace, "[Load] Using default settings.");

            settings.SetFullscreen(false);
            settings.SetVsync(true);
            settings.SetFps(Raylib.GetMonitorRefreshRate(Raylib.GetCurrentMonitor()));
            Raylib.SetMasterVolume(1f);
            settings.SetIntegerScale(false);
            settings.SetSoundVolume(1f);
            settings.SetMusicVolume(1f);
        }
    }
}

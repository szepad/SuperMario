using Mario2.Editor;

namespace Mario2
{
    public static class GameSettingsLoader
    {
        public const string SettingsFileVersion = "ver 1.0";

        public static void Save(GameSettings settings)
        {
            try
            {
                using StreamWriter writer = new(AppPaths.SettingsFilePath);

                writer.WriteLine(SettingsFileVersion);
                writer.WriteLine(settings.IsFullscreen);
                writer.WriteLine(settings.IsVsync);
                writer.WriteLine(settings.TargetFps);
                writer.WriteLine((int)(Raylib.GetMasterVolume() * 100f));
                writer.WriteLine(settings.IsIntegerScale);
                writer.WriteLine((int)(settings.SoundVolume * 100f));
                writer.WriteLine((int)(settings.MusicVolume * 100f));
                // One path per line, most recent first. The first one is where older builds
                // kept their single "recently opened" level, so they still read it fine.
                foreach (string path in EditorPrefs.RecentFiles)
                    writer.WriteLine(path);
            }
            catch (Exception error)
            {
                Raylib.TraceLog(TraceLogLevel.Error, error.Message);
                Raylib.TraceLog(TraceLogLevel.Error, error.StackTrace);
            }
        }

        public static void Load(GameSettings settings)
        {
            if (!File.Exists(AppPaths.SettingsFilePath))
            {
                string path = Path.GetFullPath(AppPaths.SettingsFilePath);
                Raylib.TraceLog(TraceLogLevel.Warning, $"[Load] Settings file does not exist at '{path}'");

                UseDefaultSettings(settings);

                return;
            }

            try
            {
                using StreamReader reader = new(AppPaths.SettingsFilePath);

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
                List<string> recent = [];
                while (reader.ReadLine() is { } line)
                {
                    if (!string.IsNullOrWhiteSpace(line))
                        recent.Add(line);
                }

                EditorPrefs.LoadRecent(recent);
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
            settings.SetVsync(false);
            settings.SetFps(360);
            Raylib.SetMasterVolume(1f);
            settings.SetIntegerScale(false);
            settings.SetSoundVolume(1f);
            settings.SetMusicVolume(1f);
        }
    }
}

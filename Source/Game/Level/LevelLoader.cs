namespace Mario2
{
    public static class LevelLoader
    {
        /// <summary>
        /// Loads a v2 level file into a playable world. Returns null (with a reason) on failure.
        /// </summary>
        public static World? Load(string path, out string? error)
        {
            try
            {
                string text = File.ReadAllText(path);
                if (!LevelFormat.IsCurrentFormat(text))
                {
                    error = $"'{path}' uses the legacy level format. Open it in the editor and save it, or run with --migrate-levels.";
                    Console.WriteLine(error);
                    return null;
                }

                List<string> warnings = [];
                LevelData level = LevelFormat.Read(text, warnings);
                World world = LevelBuilder.Build(level, LevelPaths.Key(path), warnings);

                foreach (string warning in warnings)
                    Console.WriteLine($"'{path}': {warning}");

                error = null;
                return world;
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or LevelFormatException)
            {
                error = $"Failed to load '{path}': {ex.Message}";
                Console.WriteLine(error);
                return null;
            }
        }

        /// <summary>
        /// Reads a level file in either format. Legacy files are migrated in memory.
        /// </summary>
        public static LevelData ReadData(string path, out bool wasLegacy, List<string>? warnings = null)
        {
            string text = File.ReadAllText(path);
            wasLegacy = !LevelFormat.IsCurrentFormat(text);

            return wasLegacy
                ? LegacyLevelParser.Parse(text, warnings)
                : LevelFormat.Read(text, warnings);
        }

        /// <summary>
        /// Converts every legacy level in a folder to the current format, in place.
        /// </summary>
        public static void MigrateFolder(string folder, bool backup)
        {
            if (!Directory.Exists(folder))
            {
                Console.WriteLine($"Folder not found: '{folder}'");
                return;
            }

            int migrated = 0;
            foreach (string path in Directory.EnumerateFiles(folder, "*" + LevelFormat.Extension))
            {
                try
                {
                    string text = File.ReadAllText(path);
                    if (LevelFormat.IsCurrentFormat(text))
                    {
                        Console.WriteLine($"  up to date  {Path.GetFileName(path)}");
                        continue;
                    }

                    List<string> warnings = [];
                    LevelData level = LegacyLevelParser.Parse(text, warnings);

                    if (backup)
                        File.Copy(path, path + ".bak", overwrite: true);

                    LevelFormat.Save(level, path);
                    migrated++;

                    Console.WriteLine($"  migrated    {Path.GetFileName(path)}");
                    foreach (string warning in warnings)
                        Console.WriteLine($"              {warning}");
                }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or LevelFormatException
                    or FormatException or IndexOutOfRangeException)
                {
                    Console.WriteLine($"  FAILED      {Path.GetFileName(path)}: {ex.Message}");
                }
            }

            Console.WriteLine($"Migrated {migrated} level(s).");
        }
    }
}

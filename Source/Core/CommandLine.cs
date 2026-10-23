namespace Mario2
{
    /// <summary>
    /// Maintenance commands that run instead of the game, e.g.
    /// <c>Mario2 --migrate-levels levels [--no-backup]</c>.
    /// </summary>
    public static class CommandLine
    {
        public static bool HasCommand(string[] args) => args.Any(a => a.StartsWith("--"));

        /// <summary>
        /// Makes path arguments absolute. Must run before the working directory changes.
        /// </summary>
        public static string[] ResolvePaths(string[] args)
            => [.. args.Select(a => a.StartsWith("--") ? a : Path.GetFullPath(a))];

        public static bool TryRun(string[] args)
        {
            if (args.Length == 0)
                return false;

            switch (args[0])
            {
                case "--migrate-levels":
                    LevelLoader.MigrateFolder(args.Length > 1 && !args[1].StartsWith("--") ? args[1] : AppPaths.DefaultLevelsFolder,
                        backup: !args.Contains("--no-backup"));
                    return true;

                default:
                    Console.WriteLine($"Unknown command '{args[0]}'. Available: --migrate-levels [folder] [--no-backup]");
                    return true;
            }
        }
    }
}

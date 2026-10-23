namespace Mario2.Editor
{
    /// <summary>Grid snapping for entities, in texture pixels.</summary>
    public enum SnapMode
    {
        Tile = 16,
        HalfTile = 8,
        Pixel = 1
    }

    public static class EditorPrefs
    {
        public const int MaxRecentFiles = 10;

        private static readonly List<string> _recentFiles = [];

        /// <summary>Recently opened or saved levels, most recent first. Persisted in the settings file.</summary>
        public static IReadOnlyList<string> RecentFiles => _recentFiles;

        /// <summary>Changes whenever <see cref="RecentFiles"/> does, so menus know when to rebuild.</summary>
        public static int RecentVersion { get; private set; }

        /// <summary>Reopened when entering the editor.</summary>
        public static string RecentlyOpened => _recentFiles.FirstOrDefault() ?? string.Empty;

        /// <summary>Moves (or adds) a level to the top of the recent list.</summary>
        public static void AddRecent(string path)
        {
            if (string.IsNullOrWhiteSpace(path) || (_recentFiles.Count > 0 && _recentFiles[0] == path))
                return;

            _recentFiles.RemoveAll(p => LevelPaths.Same(p, path));
            _recentFiles.Insert(0, path);
            if (_recentFiles.Count > MaxRecentFiles)
                _recentFiles.RemoveRange(MaxRecentFiles, _recentFiles.Count - MaxRecentFiles);

            RecentVersion++;
        }

        public static void ClearRecent()
        {
            _recentFiles.Clear();
            RecentVersion++;
        }

        /// <summary>Replaces the list when the settings file is loaded (most recent first).</summary>
        public static void LoadRecent(IEnumerable<string> paths)
        {
            _recentFiles.Clear();
            foreach (string path in paths.Reverse())
                AddRecent(path);

            RecentVersion++;
        }

        public static bool ShowGrid { get; set; } = true;
        public static bool ShowScreenGuides { get; set; }
        public static bool ShowBackground { get; set; } = true;
        public static bool ShowCollision { get; set; }
        public static bool AnimateSprites { get; set; } = true;
        public static SnapMode Snap { get; set; } = SnapMode.HalfTile;
    }
}

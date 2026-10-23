namespace Mario2
{
    public static class LevelPaths
    {
        /// <summary>Identity of a level that only exists in memory (an unsaved editor playtest).</summary>
        public const string Unsaved = "unsaved://level";

        /// <summary>
        /// Normalizes a level path so it can be compared and used as a dictionary key.
        /// </summary>
        public static string Key(string path)
            => string.IsNullOrEmpty(path) || path == Unsaved ? Unsaved : Path.GetFullPath(path);

        public static bool Same(string a, string b) => Key(a) == Key(b);
    }
}

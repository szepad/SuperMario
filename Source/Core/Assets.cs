namespace Mario2
{
    public static class Assets
    {
        private static readonly Dictionary<string, Texture2D> _texByName = [];
        private static Texture2D _placeHolderTex;

        private static readonly string[] ImageFormats = [".png"];

        public static void Load(string root)
        {
            // Create placeholder texture
            Image img = Raylib.GenImageColor(16, 16, Color.Pink);
            _placeHolderTex = Raylib.LoadTextureFromImage(img);
            Raylib.UnloadImage(img);

            // Load entries in root
            if (!Directory.Exists(root))
            {
                Console.Error.WriteLine($"Asset loading failed. Root folder does not exist: '{Path.GetFullPath(root)}'");
                return;
            }

            List<string> assetFiles = SearchDir(root);

            foreach (string file in assetFiles)
            {
                string name = Path.GetFileNameWithoutExtension(file);
                string ext = Path.GetExtension(file);

                if (ImageFormats.Contains(ext))
                {
                    _texByName[name] = Raylib.LoadTexture(file);
                }
            }
        }

        private static List<string> SearchDir(string dir)
        {
            List<string> files = [];
            foreach (string entry in Directory.EnumerateFileSystemEntries(dir))
            {
                if (File.GetAttributes(entry).HasFlag(FileAttributes.Directory))
                {
                    files.AddRange(SearchDir(entry));
                }
                else
                {
                    files.Add(entry);
                }
            }

            return files;
        }

        public static void Unload()
        {
            Raylib.UnloadTexture(_placeHolderTex);
        }

        public static Texture2D GetTex(string name)
        {
            if (_texByName.TryGetValue(name, out Texture2D tex))
            {
                return tex;
            }

            return _placeHolderTex;
        }
    }
}

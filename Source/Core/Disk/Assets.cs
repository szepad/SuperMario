using Raylib_cs;

namespace Mario2
{
    public static class Assets
    {
        public static readonly string MusicFolder = Path.Combine("res", "music");

        private static readonly Dictionary<string, Texture2D> _texByName = [];
        private static readonly Dictionary<string, Sound> _soundsByName = [];
        private static readonly Dictionary<string, byte[]> _musicByName = [];

        private static Texture2D _placeHolderTex;

        private static readonly string[] ImageFormats = [".png"];
        private static readonly string[] SoundFormats = [".wav"];
        private const string IgnoreFolder = ".ignore";

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
                else if (SoundFormats.Contains(ext))
                {
                    string? dir = Path.GetDirectoryName(file);
                    if (dir == MusicFolder)
                    {
                        _musicByName[name] = File.ReadAllBytes(file);
                    }
                    else
                    {
                        _soundsByName[name] = Raylib.LoadSound(file);
                    }
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
                    if (Path.GetFileName(entry) != IgnoreFolder)
                    {
                        files.AddRange(SearchDir(entry));
                    }
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

        public static bool TryGetSound(string name, out Sound sound)
        {
            if (!_soundsByName.TryGetValue(name, out sound))
            {
                Console.WriteLine($"Couldn't retrieve sound '{name}'");
                return false;
            }

            return true;
        }

        public static bool TryGetMusic(string name, out byte[]? streamData)
        {
            if (!_musicByName.TryGetValue(name, out streamData))
            {
                Console.WriteLine($"Couldn't retrieve music stream '{name}'");
                return false;
            }

            return true;
        }
    }
}

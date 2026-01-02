namespace Mario2
{
    public static class SoundManager
    {
        public static MusicName CurrentMusic { get; private set; } = MusicName.None;
        private static Music _music;
        private static bool _musicLoaded;

        public static bool IsMusicStopped => !_musicLoaded;

        public static string GetSoundName(Sounds sound) =>
            sound == Sounds.None ? string.Empty : sound.ToString();

        public static string GetMusicName(MusicName name) =>
            name == MusicName.None ? string.Empty : name.ToString();

        public static float MusicVolumeScaler { get; set; } = 1f;

        public static void UpdateMusicVolume()
        {
            if (_musicLoaded)
            {
                Raylib.SetMusicVolume(_music, 0.75f * Game.Instance.Settings.MusicVolume * Helper.Clamp01(MusicVolumeScaler));
            }
        }

        public static void PlayMusic(MusicName music)
        {
            if (CurrentMusic == music)
                return;

            if (_musicLoaded)
            {
                StopMusic();
            }

            string name = GetMusicName(music);
            if (Assets.TryGetMusic(name, out byte[]? data))
            {
                _music = Raylib.LoadMusicStreamFromMemory(".wav", data);
            }

            if (Raylib.IsMusicValid(_music))
            {
                _musicLoaded = true;
                MusicVolumeScaler = 1f;
                UpdateMusicVolume();

                Raylib.PlayMusicStream(_music);
                CurrentMusic = music;
            }
            else
            {
                Console.WriteLine($"Failed to stream music at '{name}'");
                CurrentMusic = MusicName.None;
                _musicLoaded = false;
            }
        }

        public static void UpdateCurrentMusic()
        {
            if (!_musicLoaded)
                return;

            Raylib.UpdateMusicStream(_music);
        }

        public static void StopMusic()
        {
            if (_musicLoaded)
            {
                Raylib.UnloadMusicStream(_music);
                _musicLoaded = false;
            }

            CurrentMusic = MusicName.None;
        }

        public static void PauseMusic()
        {
            if (!_musicLoaded)
                return;

            Raylib.PauseMusicStream(_music);
        }

        public static void ResumeMusic()
        {
            if (!_musicLoaded)
                return;

            Raylib.ResumeMusicStream(_music);
        }

        public static void PlaySoundRandomPitch(Sounds name, float pitchVariance = 0.1f, float volume = 1f)
        {
            PlaySound(name, pitch: Rng.GetFloat(1f - pitchVariance, 1f + pitchVariance), volume);
        }

        public static void PlaySound(Sounds name, float pitch = 1f, float volume = 1f)
        {
            string stringName = GetSoundName(name);
            if (stringName == string.Empty || !Assets.TryGetSound(stringName, out Sound sound))
                return;

            Sound alias = Raylib.LoadSoundAlias(sound);
            Raylib.SetSoundPitch(alias, pitch);
            Raylib.SetSoundVolume(alias, volume * Game.Instance.Settings.SoundVolume);
            Raylib.PlaySound(alias);
        }
    }
}

namespace Mario2
{
    public class GameSettings
    {
        public bool IsFullscreen { get; private set; }
        public bool IsVsync { get; private set; }
        public int TargetFps { get; private set; }
        public bool IsIntegerScale { get; private set; }
        public float SoundVolume { get; private set; }
        public float MusicVolume { get; private set; }

        public void SetFullscreen(bool fullscreen)
        {
            if (OperatingSystem.IsMacOS())
            {
                IsFullscreen = false;
                return;
            }
            
            IsFullscreen = fullscreen;
            int monitor = Raylib.GetCurrentMonitor();
            int monitorWidth = Raylib.GetMonitorWidth(monitor);
            int monitorHeight = Raylib.GetMonitorHeight(monitor);
            Vector2 pos = Raylib.GetMonitorPosition(monitor);
            
            if (fullscreen)
            {
                Raylib.SetWindowState(ConfigFlags.UndecoratedWindow);
                Raylib.SetWindowState(ConfigFlags.TopmostWindow);
                Raylib.SetWindowSize(monitorWidth, monitorHeight);
                Raylib.SetWindowPosition((int)pos.X, (int)pos.Y);
            }
            else
            {
                Raylib.ClearWindowState(ConfigFlags.UndecoratedWindow);
                Raylib.ClearWindowState(ConfigFlags.TopmostWindow);
                Raylib.SetWindowSize(Game.Width, Game.Height);
                Raylib.SetWindowPosition(
                    (int)pos.X + (monitorWidth - Game.Width) / 2, 
                    (int)pos.Y + (monitorHeight - Game.Height) / 2);
            }
        }

        public void SetVsync(bool vsync)
        {
            IsVsync = vsync;

            if (vsync)
            {
                Raylib.SetTargetFPS(0);
                Raylib.SetWindowState(ConfigFlags.VSyncHint);
            }
            else
            {
                Raylib.SetTargetFPS(TargetFps);
                Raylib.ClearWindowState(ConfigFlags.VSyncHint);
            }
        }

        public void SetFps(int fps)
        {
            TargetFps = fps;
            if (!IsVsync)
            {
                Raylib.SetTargetFPS(fps);
            }
        }

        public void SetIntegerScale(bool val)
        {
            IsIntegerScale = val;
        }

        public void SetSoundVolume(float volume)
        {
            SoundVolume = Helper.Clamp01(volume);
        }

        public void SetMusicVolume(float volume)
        {
            MusicVolume = Helper.Clamp01(volume);
            SoundManager.UpdateMusicVolume();
        }
    }
}

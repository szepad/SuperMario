namespace Mario2
{
    public class SettingsScene : Scene
    {
        private readonly UIWidgetHandler _buttons = new();

        private readonly GameSettings _settings = Game.Instance.Settings;

        private string Label_Fullscreen => $"Fullscreen {(_settings.IsFullscreen ? "on" : "off")}";
        private string Label_VSync => $"VSync {(_settings.IsVsync ? "on" : "off")}";
        private string Label_Fps => $"FPS {_settings.TargetFps}";
        private string Label_VolumeMaster => $"Volume {(int)(Raylib.GetMasterVolume() * 100f)}";

        private readonly UISelectionButton<int> _fpsButton;
        private bool FpsButtonGreyedOut => _settings.IsVsync;

        private static readonly int[] _fpsSelection = [
            0, 10, 20, 30, 60, 75, 100, 120, 144, 165, 200, 240, 360
        ];

        public SettingsScene()
        {
            _buttons.Add(new UIPressButton(Label_Fullscreen, Button_Fullscreen));
            _buttons.Add(new UIPressButton(Label_VSync, Button_VSync));
            _fpsButton = (UISelectionButton<int>)_buttons.Add(
                new UISelectionButton<int>(Label_Fps, _fpsSelection, Button_Fps,
                currIndex: _fpsSelection.IndexOf(_settings.TargetFps))
            {
                IsGreyedOut = FpsButtonGreyedOut
            });

            _buttons.Add(new UIRangeButton(Label_VolumeMaster, Button_VolumeMaster,
                value: Raylib.GetMasterVolume()));

            _buttons.Add(new UIPressButton("Back", _ => Button_Back()));
            _buttons.PlaceVertically(new Vector2(Game.Width / 2f, Game.Height * 0.4f));
            _buttons[^1]?.Position.Y += 8f.Scale() + UIWidget.DefaultGap;
        }

        private void Button_Fullscreen(UIPressButton self)
        {
            _settings.SetFullscreen(!_settings.IsFullscreen);
            self.Label = Label_Fullscreen;
        }

        private void Button_VSync(UIPressButton self)
        {
            _settings.SetVsync(!_settings.IsVsync);
            self.Label = Label_VSync;
        }

        private void Button_Fps(UISelectionButton<int> self, int nextVal)
        {
            _settings.SetFps(nextVal);
            self.Label = Label_Fps;
        }

        private void Button_VolumeMaster(UIRangeButton self)
        {
            Raylib.SetMasterVolume(Helper.Clamp01(self.Value));
            self.Label = Label_VolumeMaster;
        }

        private void Button_Back()
        {
            ParentScene?.UnloadSubScene();
        }

        protected override void OnLoad()
        {
            
        }

        protected override void OnUnload()
        {
            
        }

        protected override void OnUpdate()
        {
            _buttons.Update();
            _fpsButton.IsGreyedOut = FpsButtonGreyedOut;

            if (Input.IsKeyPressed(KeyboardKey.Escape))
                Button_Back();
        }

        protected override void OnDraw()
        {
            Raylib.ClearBackground(Color.Black);
            _buttons.Draw();
        }
    }
}

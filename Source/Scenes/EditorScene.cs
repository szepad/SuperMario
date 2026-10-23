using Mario2.Editor;

namespace Mario2
{
    /// <summary>
    /// Hosts the level editor. Playtests run as a sub-scene, so returning from one resumes the
    /// editor exactly where it was.
    /// </summary>
    public class EditorScene : Scene
    {
        public override bool DrawsToWindow => true;

        private LevelEditor _editor = null!;

        protected override void OnLoad()
        {
            Raylib.ShowCursor();
            SoundManager.PlayMusic(MusicName.None);

            _editor = new LevelEditor(this);
            _editor.Load();
        }

        protected override void OnUnload()
        {
            _editor.Unload();
        }

        protected override void OnSubSceneUnload()
        {
            _editor.OnReturnFromPlaytest();
        }

        protected override void OnUpdate()
        {
            _editor.Update();
        }

        protected override void OnDraw()
        {
            _editor.Draw();
        }
    }
}

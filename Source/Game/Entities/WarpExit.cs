namespace Mario2
{
    public class WarpExit : Entity
    {
        public string Tag { get; set; } = string.Empty;

        public WarpExit(World world)
            : base(world)
        {
            Size = HitboxSize = new Vector2(16f).Scale();
        }

        protected override void OnSpawn()
        {

        }

        public override void Draw()
        {
            if (Game.Instance.ActiveScene is EditorScene editor && editor.SubScene is null)
            {
                Raylib.DrawTextureEx(Assets.GetTex("editor_pipe_exit"), Position, 0f, Game.TextureScale, Color.White);
            }

            base.Draw();
        }

        public override string SaveCustomData()
        {
            return Tag;
        }

        public override void LoadCustomData(string data)
        {
            Tag = data;
        }
    }
}

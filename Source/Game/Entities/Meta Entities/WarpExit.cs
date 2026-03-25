namespace Mario2
{
    public class WarpExit : Entity
    {
        public string Tag { get; set; } = string.Empty;

        public WarpExit(World world)
            : base(world)
        {
            Size = HitboxSize = new Vector2(16f).Scale();
            IsCollidable = false;
            Gravity = 0f;
            CanBeDestroyed = false;
        }

        protected override void OnSpawn()
        {

        }

        protected override void DrawEditor()
        {
            Raylib.DrawTextureEx(Assets.GetTex("editor_pipe_exit"), Position, 0f, Game.TextureScale, Color.White);
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

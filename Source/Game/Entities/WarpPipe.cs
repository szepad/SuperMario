namespace Mario2
{
    public class WarpPipe : Entity
    {
        public string Destination { get; set; } = string.Empty;
        public string Tag { get; set; } = string.Empty;

        public WarpPipe(World world)
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
                Raylib.DrawTextureEx(Assets.GetTex("editor_pipe"), Position, 0f, Game.TextureScale, Color.White);
            }

            base.Draw();
        }

        public override string SaveCustomData()
        {
            return $"{Destination},{Tag}";
        }

        public override void LoadCustomData(string data)
        {
            string[] split = data.Split(',');
            Destination = split[0];
            Tag = split[1];
        }
    }
}

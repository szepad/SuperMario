namespace Mario2
{
    public enum PipeDirection
    {
        Down,
        Right
    }

    public class WarpPipe : Entity
    {
        public string Destination { get; set; } = string.Empty;
        public string Tag { get; set; } = string.Empty;
        public PipeDirection Dir { get; set; } = PipeDirection.Down;

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
                if (Dir == PipeDirection.Right)
                {
                    Raylib.DrawTextureEx(Assets.GetTex("editor_pipe"), Position + new Vector2(0f, 16f).Scale(), 270f, Game.TextureScale, Color.White);
                }
                else
                {
                    Raylib.DrawTextureEx(Assets.GetTex("editor_pipe"), Position, 0f, Game.TextureScale, Color.White);
                }
            }

            base.Draw();
        }

        public override string SaveCustomData()
        {
            return $"{Destination},{Tag},{(int)Dir}";
        }

        public override void LoadCustomData(string data)
        {
            string[] split = data.Split(',');
            Destination = split[0];
            Tag = split[1];
            Dir = (PipeDirection)int.Parse(split[2]);
        }
    }
}

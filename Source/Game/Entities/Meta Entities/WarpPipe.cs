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
        public bool ReturnToSubworld { get; set; } = false;

        public WarpPipe(World world)
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
            if (Dir == PipeDirection.Right)
            {
                Raylib.DrawTextureEx(Assets.GetTex("editor_pipe"), Position + new Vector2(0f, 16f).Scale(), 270f, Game.TextureScale, Color.White);
            }
            else
            {
                Raylib.DrawTextureEx(Assets.GetTex("editor_pipe"), Position, 0f, Game.TextureScale, Color.White);
            }
        }

        public override string SaveCustomData()
        {
            return $"{Destination},{Tag},{(int)Dir},{ReturnToSubworld}";
        }

        public override void LoadCustomData(string data)
        {
            string[] split = data.Split(',');
            Destination = split[0];
            Tag = split[1];
            Dir = (PipeDirection)int.Parse(split[2]);
            ReturnToSubworld = Convert.ToBoolean(split[3]);
        }
    }
}

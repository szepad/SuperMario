namespace Mario2
{
    public class ConstantPlatform : Platform
    {
        public PlatformDirection Dir { get; set; } = PlatformDirection.Up;
        public float Speed { get; set; } = Tile.Size * 3.5f;

        public ConstantPlatform(World world)
            : base(world)
        {

        }

        public override void Update()
        {
            Velocity = Dir switch
            {
                PlatformDirection.Left => new Vector2(-Speed, 0f),
                PlatformDirection.Right => new Vector2(Speed, 0f),
                PlatformDirection.Down => new Vector2(0f, Speed),
                _ => new Vector2(0f, -Speed)
            };

            // Vertical wrap-around
            if (Feet.Y < -GameScene.CameraOffsetY)
            {
                SetFeetPos(Feet.X, Game.Height + Size.Y - GameScene.CameraOffsetY);
            }
            else if (Head.Y > Game.Height - GameScene.CameraOffsetY)
            {
                SetFeetPos(Feet.X, -GameScene.CameraOffsetY);
            }

            base.Update();
        }

        public override string SaveCustomData()
        {
            return $"{Type},{Dir}";
        }

        public override void LoadCustomData(string data)
        {
            string[] split = data.Split(',');
            if (Enum.TryParse(split[0], out PlatformType type)) Resize(type);
            if (Enum.TryParse(split[1], out PlatformDirection dir)) Dir = dir;
        }
    }
}

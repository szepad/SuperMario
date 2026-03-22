namespace Mario2
{
    public class PingPongPlatform : Platform
    {
        public float Speed { get; set; } = Tile.Size * 3.5f;
        public bool IsHorizontal { get; set; }
        public float TileOffset { get; set; }

        public PingPongPlatform(World world)
            : base(world)
        {

        }

        public override void Update()
        {
            base.Update();
        }

        public override string SaveCustomData()
        {
            return $"{Type},{IsHorizontal},{(int)(TileOffset * 10f)}";
        }

        public override void LoadCustomData(string data)
        {
            string[] split = data.Split(',');
            if (Enum.TryParse(split[0], out PlatformType type))
                Resize(type);

            IsHorizontal = Convert.ToBoolean(split[1]);
            TileOffset = int.Parse(split[2]) / 10f;
        }
    }
}

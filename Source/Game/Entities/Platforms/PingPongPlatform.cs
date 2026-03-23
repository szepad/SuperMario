namespace Mario2
{
    public class PingPongPlatform(World world) : Platform(world)
    {
        public float TileOffset { get; set; }
        public bool IsHorizontal { get; set; }

        private readonly PingPongBehaviour _pingPong = new();

        protected override void OnSpawn()
        {
            _pingPong.Setup(Position, TileOffset, IsHorizontal);
            base.OnSpawn();
        }

        public override void Update()
        {
            _pingPong.Update();
            Velocity = _pingPong.GetVelocity(Position);

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

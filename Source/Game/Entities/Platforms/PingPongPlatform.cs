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
    }
}

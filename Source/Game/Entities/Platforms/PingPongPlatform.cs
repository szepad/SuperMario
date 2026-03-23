using System.Timers;

namespace Mario2
{
    public class PingPongPlatform : Platform
    {
        public float LerpTime { get; set; } = 3f;
        public bool IsHorizontal { get; set; }
        public float TileOffset { get; set; }

        private Vector2 _start;
        private Vector2 _end;

        private float _timer;
        private float _dir = 1f;

        public PingPongPlatform(World world)
            : base(world)
        {

        }

        protected override void OnSpawn()
        {
            _start = Position;
            _end = Position;

            if (IsHorizontal) _end.X += TileOffset * Tile.Size;
            else _end.Y += TileOffset * Tile.Size;

            base.OnSpawn();
        }

        public override void Update()
        {
            _timer += Time.DeltaTime * _dir;

            if (_timer >= LerpTime)
            {
                _timer = LerpTime;
                _dir = -1f;
            }
            else if (_timer <= 0f)
            {
                _timer = 0f;
                _dir = 1f;
            }

            Vector2 newPos = Vector2.Lerp(
                _start, _end,
                Helper.EaseInOut(_timer / LerpTime));

            Velocity = (newPos - Position) / Time.DeltaTime;

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

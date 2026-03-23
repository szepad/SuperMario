namespace Mario2
{
    public class PingPongBehaviour
    {
        public float LerpTime { get; set; } = 3f;

        private bool _horizontal;
        private Vector2 _start;
        private Vector2 _end;

        private float _timer;
        private float _dir = 1f;

        public void Setup(Vector2 pos, float tileOffset, bool horizontal)
        {
            _horizontal = horizontal;
            _start = pos;
            _end = pos;

            if (horizontal) _end.X += tileOffset * Tile.Size;
            else _end.Y += tileOffset * Tile.Size;
        }

        public void Update()
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
        }

        public Vector2 GetVelocity(Vector2 pos)
        {
            Vector2 newPos = Vector2.Lerp(
                _start, _end,
                Helper.EaseInOut(_timer / LerpTime));

            newPos -= pos;

            if (_horizontal) newPos.Y = 0f;
            else newPos.X = 0f;

            return newPos / Time.DeltaTime;
        }
    }
}

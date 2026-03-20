namespace Mario2
{
    public class PipeEnterRightCutscene(Player player, WarpPipe pipe)
        : Cutscene<SingleCutsceneState>(player)
    {
        private readonly WarpPipe _pipe = pipe;

        private float _waitTimer = 1f;

        public override Action GetFunction(SingleCutsceneState _) => EnterState;

        private float _startY;
        private float _endY;

        public override void Start()
        {
            SoundManager.PlaySound(Sounds.PlayerWarp);
            Player.ForceStandUp();
            Player.PlayAnim(PlayerAnim.Run);
            Player.SetRunAnimFrameTimes(0.1f);
            Player.Sprite.FlipX = false;
            Player.IsCollidable = false;
            Player.IsHittable = false;
            Player.Gravity = 0f;
            Player.Velocity = Vector2.Zero;
            Player.LayerDepth = LayerDepth.Background;

            _startY = Player.Rect.Y;
            _endY = _startY - 2f.Scale();
        }

        private void EnterState()
        {
            if (Player.Rect.X >= _pipe.Rect.X + _pipe.Rect.Width + 8f.Scale())
            {
                _waitTimer -= Time.DeltaTime;
                if (_waitTimer <= 0f)
                {
                    IsFinished = true;
                    Player.World.HandlePipeTransition(_pipe);
                }
            }
            else
            {
                Player.Position.X += 16f.Scale() * Time.DeltaTime;

                float relPlayerX = Player.Rect.X + Player.Size.X - (_pipe.Rect.X + _pipe.Rect.Width);
                float t = Helper.Clamp01(relPlayerX / _pipe.Rect.Width);
                Player.Position.Y = Raymath.Lerp(_startY, _endY, t);
            }
        }
    }
}

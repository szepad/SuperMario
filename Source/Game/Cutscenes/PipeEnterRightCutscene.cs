namespace Mario2
{
    public class PipeEnterRightCutscene(Player player, WarpPipe pipe)
        : Cutscene<SingleCutsceneState>(player)
    {
        private readonly WarpPipe _pipe = pipe;

        public override Action GetFunction(SingleCutsceneState _) => EnterState;

        public override void Start()
        {
            SoundManager.PlaySound(Sounds.PlayerWarp);
            Player.PlayAnim(PlayerAnim.Run);
            Player.SetRunAnimFrameTimes(0.1f);
            Player.Sprite.FlipX = false;
            Player.IsCollidable = false;
            Player.IsHittable = false;
            Player.Gravity = 0f;
            Player.Velocity = Vector2.Zero;
            Player.LayerDepth = LayerDepth.Background;
        }

        private void EnterState()
        {
            Player.Position.X += 16f.Scale() * Time.DeltaTime;

            if (Player.Position.X >= _pipe.Rect.X + _pipe.Rect.Width + 8f.Scale())
            {
                IsFinished = true;
                Player.World.HandlePipeTransition(_pipe);
            }
        }
    }
}

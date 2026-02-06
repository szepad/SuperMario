namespace Mario2
{
    public class PipeEnterDownCutscene(Player player, WarpPipe pipe)
        : Cutscene<SingleCutsceneState>(player)
    {
        private readonly WarpPipe _pipe = pipe;

        private float _waitTimer = 1f;

        public override Action GetFunction(SingleCutsceneState _) => EnterState;

        public override void Start()
        {
            SoundManager.PlaySound(Sounds.PlayerWarp);
            Player.ForceStandUp();
            Player.PlayAnim(PlayerAnim.Emote);
            Player.Sprite.SetFrame(1);
            Player.IsCollidable = false;
            Player.IsHittable = false;
            Player.Gravity = 0f;
            Player.Velocity = Vector2.Zero;
            Player.LayerDepth = LayerDepth.Background;
        }

        private void EnterState()
        {
            if (Player.Position.Y >= _pipe.Feet.Y + 16f.Scale())
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
                Player.Position.Y += 32f.Scale() * Time.DeltaTime;
            }
        }
    }
}

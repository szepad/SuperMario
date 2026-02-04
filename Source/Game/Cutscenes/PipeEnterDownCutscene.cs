namespace Mario2
{
    public class PipeEnterDownCutscene(Player player, WarpPipe pipe)
        : Cutscene<SingleCutsceneState>(player)
    {
        private readonly WarpPipe _pipe = pipe;

        public override Action GetFunction(SingleCutsceneState _) => EnterState;

        public override void Start()
        {
            SoundManager.PlaySound(Sounds.PlayerWarp);
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
            Player.Position.Y += 32f.Scale() * Time.DeltaTime;

            if (Player.Position.Y >= _pipe.Feet.Y + 8f.Scale())
            {
                IsFinished = true;
                Player.World.HandlePipeTransition(_pipe);
            }
        }
    }
}


namespace Mario2
{
    public class PipeExitCutscene(Player player, WarpExit exit)
        : Cutscene<SingleCutsceneState>(player)
    {
        private readonly WarpExit _exit = exit;

        public override Action GetFunction(SingleCutsceneState state) => ExitState;

        public override void Start()
        {
            Player.SetFeetPos(_exit.Feet);
            Player.Position.Y += Player.Size.Y + 8f.Scale();
            Player.LayerDepth = LayerDepth.Background;
            Player.IsCollidable = false;
            Player.IsHittable = false;
            Player.Gravity = 0f;
            Player.PlayAnim(PlayerAnim.Idle);
            Player.Sprite.FlipX = false;
            Player.Velocity = Vector2.Zero;
        }

        private void ExitState()
        {
            Player.Position.Y -= 32f.Scale() * Time.DeltaTime;
            Player.OldPosition.Y = Player.Position.Y;
            if (Player.Feet.Y <= _exit.Feet.Y + 0.5f.Scale())
            {
                IsFinished = true;
                Player.LayerDepth = LayerDepth.Player;
                Player.IsCollidable = true;
                Player.IsHittable = true;
                Player.Gravity = Entity.DefaultGravity;

                // Do this so collision system snaps player on top of the pipe
                Player.OldPosition.Y--;
            }
        }
    }
}

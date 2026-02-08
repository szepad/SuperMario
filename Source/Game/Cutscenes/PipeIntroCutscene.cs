namespace Mario2
{
    public class PipeIntroCutscene(Player player)
        : Cutscene<SingleCutsceneState>(player)
    {
        public override Action GetFunction(SingleCutsceneState _) => EnterState;

        public override void Start()
        {
            Player.Sprite.FlipX = false;
            Player.Velocity.X = Player.World.CameraAutoScrollSpeed = 40f.Scale();
            Player.World.CameraStyle = CameraStyle.AutoScroll;
            Player.PlayAnim(PlayerAnim.Run);
            Player.SetRunAnimFrameTimes(0.125f);
        }

        private void EnterState()
        {

        }

        public override void OnTileCollision(Tile tile, CollisionSide side)
        {
            if (side != CollisionSide.Right)
                return;

            if (Player.CollidingWithPipeEntity(out WarpPipe? pipe))
            {
                Player.StartCutscene(new PipeEnterRightCutscene(Player, pipe));
            }
        }
    }
}

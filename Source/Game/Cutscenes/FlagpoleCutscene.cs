namespace Mario2
{
    public enum FlagpoleCutsceneState
    {
        SlideDown,
        TurnAround,
        Wait,
        WalkToCastle,
        Emote,
        Disappear,
        WaitAgain
    }

    public class FlagpoleCutscene(Player player, Flagpole flagpole)
        : Cutscene<FlagpoleCutsceneState>(player)
    {
        private readonly Flagpole _flagpole = flagpole;

        private const float SlideSpeed = Tile.Size * 5f;

        public override Action GetFunction(FlagpoleCutsceneState state) => state switch
        { 
            FlagpoleCutsceneState.SlideDown => SlideDown,
            _ => () => { }
        };

        public override void Start()
        {
            Player.Position.X = Player.OldPosition.X = _flagpole.Position.X - 8f.Scale();
            Player.Velocity = Vector2.Zero;
            Player.Gravity = 0f;
            Player.Sprite.FlipX = false;
            Player.PlayAnim(PlayerAnim.Climb);
        }

        private void SlideDown()
        {
            int belowTileRow = (int)((Player.Feet.Y + Tile.Size * 0.3f) / Tile.Size);
            int belowTileCol = (int)(Player.Center.X / Tile.Size);

            if (Player.World.GetTile(belowTileCol, belowTileRow, out Tile tile) && !tile.IsCollidable)
            {
                Player.Velocity.Y = SlideSpeed;
                Player.PlayAnim(PlayerAnim.Climb);
            }
            else
            {
                Player.Velocity.Y = 0f;
                Player.PlayAnim(PlayerAnim.ClimbStop);
            }

            _flagpole.FlagRelY += SlideSpeed * Time.DeltaTime;
        }
    }
}

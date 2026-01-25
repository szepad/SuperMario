namespace Mario2
{
    public enum FlagpoleCutsceneState
    {
        SlideDown,
        TurnAround,
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

        private const float TurnAroundTime = 0.5f;

        public override Action GetFunction(FlagpoleCutsceneState state) => state switch
        { 
            FlagpoleCutsceneState.SlideDown => SlideDown,
            FlagpoleCutsceneState.TurnAround => TurnAround,
            FlagpoleCutsceneState.WalkToCastle => WalkToCastle,
            _ => () => { }
        };

        public override void Start()
        {
            Player.Position.X = Player.OldPosition.X = _flagpole.Position.X - 8f.Scale();
            Player.Velocity = Vector2.Zero;
            Player.Gravity = 0f;
            Player.Sprite.FlipX = false;
            Player.PlayAnim(PlayerAnim.Climb);
            Player.World.CameraStyle = CameraStyle.Stopped;

            SoundManager.StopMusic();
            SoundManager.PlaySound(Sounds.Flagpole);
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
            if (_flagpole.FlagRelY >= Tile.Size * 7.75f)
            {
                State++;
                Player.Velocity.Y = 0f;
                StateTimer = 0f;
                Player.Sprite.FlipX = true;
                Player.Position.X = Player.OldPosition.X = _flagpole.Position.X + 4f.Scale();
                SoundManager.PlayMusic(MusicName.LevelClear);
            }
        }

        private void TurnAround()
        {
            StateTimer += Time.DeltaTime;
            if (StateTimer >= TurnAroundTime)
            {
                State++;
            }
        }

        private void WalkToCastle()
        {
            Player.Sprite.FlipX = false;
            Player.Gravity = Entity.DefaultGravity;
            Player.Velocity.X = Player.World.CameraAutoScrollSpeed = 69f.Scale();
            Player.World.CameraStyle = CameraStyle.AutoScroll;
            Player.PlayAnim(PlayerAnim.Run);
            Player.SetRunAnimFrameTimes(0.15f);
        }
    }
}

namespace Mario2
{
    public enum FlagpoleCutsceneState
    {
        SlideDown,
        TurnAround,
        WalkToCastle,
        Emote,
        Disappear,
        TimerCountdown,
        StarFlag
    }

    public class FlagpoleCutscene(Player player, Flagpole flagpole)
        : Cutscene<FlagpoleCutsceneState>(player)
    {
        private readonly Flagpole _flagpole = flagpole;
        private CastleEntrance? _entrance;

        private const float SlideSpeed = Tile.Size * 5f;

        private const float TurnAroundTime = 0.5f;
        private const float EmoteTime = 1f;
        private const float DisappearTime = 0.35f;
        private const float TimerSoundInterval = 0.0355f;
        private const float TimerCountdownInterval = 0.015f;
        private const float TimerCountdownIntervalFaster = 0.005f;
        private float _timerCountdownTimer;
        private float _timerSoundIntervalTimer;

        private float _flagYEnd;
        private const float StarFlagSpeed = 5f * Tile.Size;

        private const float AfterFlagWaitTime = 1.5f;

        private readonly ScoreText _score = new(player.World,
            ScoreRewards.GetFlagpoleScore(player, flagpole),
            flagpole.Position + new Vector2(13f, 130f).Scale(), 
            float.PositiveInfinity, SlideSpeed);

        public override Action GetFunction(FlagpoleCutsceneState state) => state switch
        { 
            FlagpoleCutsceneState.SlideDown => SlideDown,
            FlagpoleCutsceneState.TurnAround => TurnAround,
            FlagpoleCutsceneState.WalkToCastle => WalkToCastle,
            FlagpoleCutsceneState.Emote => Emote,
            FlagpoleCutsceneState.Disappear => Disappear,
            FlagpoleCutsceneState.TimerCountdown => TimerCountdown,
            FlagpoleCutsceneState.StarFlag => StarFlag,
            _ => () => { }
        };

        public override void Start()
        {
            if (Player.State == PlayerState.Small)
            {
                Player.Position.X = Player.OldPosition.X = _flagpole.Position.X - 8f.Scale();
            }
            else
            {
                Player.Position.X = Player.OldPosition.X = _flagpole.Position.X - 11f.Scale();
            }

            Player.Velocity = Vector2.Zero;
            Player.Gravity = 0f;
            Player.Sprite.FlipX = false;
            Player.PlayAnim(PlayerAnim.Climb);
            Player.World.CameraStyle = CameraStyle.Stopped;

            SoundManager.StopMusic();
            SoundManager.PlaySound(Sounds.Flagpole);

            foreach (Entity entity in Player.World.Entities)
            {
                if (entity is CastleEntrance entrance)
                {
                    _entrance = entrance;
                    Player.World.StarFlagPosition.X = _entrance.Center.X;
                    Player.World.StarFlagPosition.Y = _entrance.Position.Y - Tile.Size * 4f;
                    _flagYEnd = Player.World.StarFlagPosition.Y - Tile.Size * 0.95f;
                }
            }

            if (_score.Points >= 1000) _score.Position.X += 2f.Scale();
            Player.World.SpawnEntity(_score);
            SessionManager.GetScoreWithoutText(_score.Points);
        }

        private void SlideDown()
        {
            int belowTileRow = (int)((Player.Feet.Y + Tile.Size * 0.1f) / Tile.Size);
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
                Player.PlayAnim(PlayerAnim.ClimbStop);
                SoundManager.PlayMusic(MusicName.LevelClear, isLooped: false);
                _score.Speed = 0f;
            }
        }

        private void TurnAround()
        {
            StateTimer += Time.DeltaTime;
            if (StateTimer >= TurnAroundTime)
            {
                State++;
                Player.Sprite.FlipX = false;
                Player.Gravity = Entity.DefaultGravity;
                Player.Velocity.X = Player.World.CameraAutoScrollSpeed = 69f.Scale();
                Player.World.CameraStyle = CameraStyle.AutoScroll;
                Player.PlayAnim(PlayerAnim.Run);
                Player.SetRunAnimFrameTimes(0.15f);
            }
        }

        private void WalkToCastle()
        {
            if (_entrance is null || Player.Feet.X > _entrance.Feet.X)
            {
                State++;
                StateTimer = 0f;
                Player.PlayAnim(PlayerAnim.Emote);
                Player.Velocity = Vector2.Zero;
            }
        }

        private void Emote()
        {
            StateTimer += Time.DeltaTime;
            if (StateTimer >= EmoteTime)
            {
                State++;
                StateTimer = 0f;
                Player.PlayAnim(PlayerAnim.Run);
            }
        }
        
        private void Disappear()
        {
            StateTimer += Time.DeltaTime;

            Player.Velocity.X = 5f.Scale();
            Player.Sprite.Opacity = Raymath.Lerp(1f, 0f, StateTimer / DisappearTime);

            if (StateTimer >= DisappearTime)
            {
                State++;
                Player.Sprite.IsVisible = false;
                Player.Velocity.X = 0f;
                StateTimer = 0f;
            }
        }

        private void TimerCountdown()
        {
            StateTimer += Time.DeltaTime;
            _timerSoundIntervalTimer += Time.DeltaTime;
            _timerCountdownTimer += Time.DeltaTime;

            if (_timerCountdownTimer >= (SoundManager.IsCurrentMusicFinished()
                ? TimerCountdownIntervalFaster : TimerCountdownInterval))
            {
                _timerCountdownTimer = 0f;
                if (Player.World.TimeLeft > 0)
                {
                    Player.World.TimeLeft--;
                    SessionManager.GetScoreWithoutText(ScoreRewards.Time);
                }
            }

            if (_timerSoundIntervalTimer >= TimerSoundInterval)
            {
                _timerSoundIntervalTimer = 0f;
                
                if (Player.World.TimeLeft > 0)
                {
                    SoundManager.PlaySound(Sounds.Score);
                }
                else
                {
                    SoundManager.PlaySound(Sounds.ScoreEnd);
                    State++;
                    Player.World.DrawStarFlag = true;
                    StateTimer = 0f;
                }
            }
        }

        private void StarFlag()
        {
            if (Player.World.StarFlagPosition.Y > _flagYEnd)
            {
                Player.World.StarFlagPosition.Y -= StarFlagSpeed * Time.DeltaTime;
            }

            StateTimer += Time.DeltaTime;
            if (StateTimer >= AfterFlagWaitTime)
            {
                StateTimer = float.NegativeInfinity;
                IsFinished = true;
                Player.World.ReachLevelEnd();
            }
        }
    }
}

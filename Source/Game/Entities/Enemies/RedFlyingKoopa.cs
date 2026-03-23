namespace Mario2
{
    public class RedFlyingKoopa : RedKoopa
    {
        public float TileOffset { get; set; }
        public bool IsWinged { get; private set; } = true;

        private readonly Animation _walkAnim;
        private readonly PingPongBehaviour _pingPong = new();

        public RedFlyingKoopa(World world) : base(world)
        {
            _walkAnim = Sprite.Animations[WalkAnim];
            Sprite.ChangeAnim(WalkAnim, new Animation(2, 16, 24, 24, 0.1f));
            Gravity = 0f;
        }

        protected override void OnSpawn()
        {
            _pingPong.Setup(Position, TileOffset, false);
            base.OnSpawn();
        }

        protected override void OnActivate()
        {
            //base.OnActivate(); // Keep dummy Ai
        }

        protected override void OnStomp(Entity stomper)
        {
            if (IsWinged)
            {
                LoseWings();
                ResetStomped();
            }
            else
            {
                base.OnStomp(stomper);
            }
        }

        protected override void OnDie()
        {
            LoseWings();
            base.OnDie();
        }

        public void LoseWings()
        {
            if (!IsWinged) return;
            IsWinged = false;
            
            Sprite.ChangeAnim(WalkAnim, _walkAnim);
            Gravity = DefaultGravity;
            SetAi(NormalAi);
        }

        public override void Update()
        {
            if (IsWinged)
            {
                _pingPong.Update();
                Velocity = _pingPong.GetVelocity(Position);
            }

            base.Update();
        }

        public override string SaveCustomData()
        {
            return $"{(int)(TileOffset * 10f)}";
        }

        public override void LoadCustomData(string data)
        {
            TileOffset = int.Parse(data) / 10f;
        }
    }
}

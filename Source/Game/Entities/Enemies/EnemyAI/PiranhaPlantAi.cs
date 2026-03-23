namespace Mario2
{
    public class PiranhaPlantAi(PiranhaPlant plant) : EnemyAi(plant)
    {
        public enum State
        {
            Wait,
            TryGoUp,
            GoUp,
            Attack,
            GoDown
        }

        private const float InsideTime = 2f;
        private const float OutsideTime = 2f;
        private const float Speed = 32f * Game.TextureScale;

        public float InsideY { get; private set; }
        public float OutsideY { get; private set; }

        public State CurrentState { get; set; }
        private float _stateTimer;

        protected override void OnSet()
        {
            if (FirstSet)
            {
                InsideY = Owner.Position.Y;
                OutsideY = Owner.Position.Y - Owner.Size.Y;
            }

            Wait();
        }

        private void Wait()
        {
            CurrentState = State.Wait;
            _stateTimer = InsideTime;
        }

        private void Attack()
        {
            CurrentState = State.Attack;
            _stateTimer = OutsideTime;
        }

        public bool IsPlayerClose()
        {
            return MathF.Abs(Owner.Center.X - Owner.World.Player.Center.X) < Tile.Size * 3;
        }

        public override void Update()
        {
            _stateTimer -= Time.DeltaTime;
            if (_stateTimer <= 0f)
            {
                if (CurrentState == State.Wait) CurrentState = State.TryGoUp;
                else if (CurrentState == State.Attack) CurrentState = State.GoDown;
            }

            if (CurrentState == State.TryGoUp)
            {
                if (!IsPlayerClose())
                {
                    CurrentState = State.GoUp;
                }
            }

            if (CurrentState == State.GoUp)
            {
                Owner.Position.Y -= Speed * Time.DeltaTime;

                if (Owner.Position.Y <= OutsideY)
                {
                    Owner.Position.Y = OutsideY;
                    Attack();
                }
            }
            else if (CurrentState == State.GoDown)
            {
                Owner.Position.Y += Speed * Time.DeltaTime;

                if (Owner.Position.Y >= InsideY)
                {
                    Owner.Position.Y = InsideY;
                    Wait();
                }
            }

            base.Update();
        }
    }
}

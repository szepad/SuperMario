namespace Mario2
{
    public abstract class Entity
    {
        public Vector2 Position;
        public Vector2 OldPosition;
        public Vector2 Velocity;

        public const float DefaultGravity = 350f;
        public float Gravity { get; set; } = DefaultGravity;

        public Sprite Sprite { get; set; } = new();

        public virtual void Update()
        {
            Velocity.Y += Gravity * Time.DeltaTime;

            OldPosition = Position;
            Position += Velocity * Time.DeltaTime;

            Sprite.Update();
        }

        public virtual void Draw()
        {
            Sprite.Draw(Position);
        }
    }
}

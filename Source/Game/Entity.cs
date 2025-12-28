namespace Mario2
{
    public abstract class Entity(World world)
    {
        public World World { get; } = world;

        public Vector2 Position;
        public Vector2 OldPosition;
        public Vector2 Velocity;
        public Vector2 Size;
        public Vector2 SpriteOffset;

        public const float DefaultGravity = 500f;
        public float Gravity { get; set; } = DefaultGravity;

        public Sprite Sprite { get; set; } = new();

        public Rectangle Rect => new(Position, Size);

        public virtual void Update()
        {
            Velocity.Y += Gravity * Time.DeltaTime;

            OldPosition = Position;
            Position.X += Velocity.X * Time.DeltaTime;
            HandleCollisions(horizontal: true);
            Position.Y += Velocity.Y * Time.DeltaTime;
            HandleCollisions(horizontal: false);

            Sprite.Update();
        }

        private void HandleCollisions(bool horizontal)
        {
            for (int y = 0; y < World.Rows; y++)
            {
                for (int x = 0; x < World.Cols; x++)
                {
                    Tile tile = World.Tiles[y, x];
                    if (!tile.IsCollidable)
                        continue;

                    Rectangle tileRect = new(x * Tile.Size, y * Tile.Size, Tile.Size, Tile.Size);
                    if (Rect.X < tileRect.X + tileRect.Width
                        && Rect.X + Rect.Width > tileRect.X
                        && Rect.Y < tileRect.Y + tileRect.Height
                        && Rect.Y + Rect.Height > tileRect.Y)
                    {
                        if (horizontal)
                        {
                            if (Velocity.X < 0f)
                            {
                                // Left collision
                                OldPosition.X = Position.X = tileRect.X + tileRect.Width;
                                Velocity.X = 0f;
                            }
                            else if (Velocity.X > 0f)
                            {
                                // Right collision
                                OldPosition.X = Position.X = tileRect.X - Rect.Width;
                                Velocity.X = 0f;
                            }
                        }
                        else
                        {
                            if (Velocity.Y < 0f)
                            {
                                // Top collision
                                OldPosition.Y = Position.Y = tileRect.Y + tileRect.Height;
                                Velocity.Y = 0f;
                            }
                            else if (Velocity.Y > 0f)
                            {
                                // Bottom collision
                                OldPosition.Y = Position.Y = tileRect.Y - Rect.Height;
                                Velocity.Y = 0f;
                            }
                        }
                    }
                }
            }
        }

        public virtual void Draw()
        {
            Sprite.Draw(Position - SpriteOffset);

            if (Game.Instance.IsDevMode)
            {
                Raylib.DrawRectangle((int)Position.X, (int)Position.Y,
                    (int)Size.X, (int)Size.Y, new Color(255, 255, 0, 128));
            }
        }
    }
}

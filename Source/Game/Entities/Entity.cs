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

        public const float DefaultGravity = 650f * Game.TextureScale;
        public float Gravity { get; set; } = DefaultGravity;

        public Sprite Sprite { get; set; } = new();

        public Rectangle Rect => new(Position, Size);

        public LayerDepth LayerDepth { get; set; } = LayerDepth.Normal;
        public bool IsCollidable { get; set; } = true;
        public bool IsGrounded { get; private set; }
        public bool IsSpawned { get; private set; }
        public bool IsDestroyed { get; private set; }

        public void SetPosition(float x, float y) => SetPosition(new Vector2(x, y));
        public void SetPosition(Vector2 pos)
        {
            Position = OldPosition = pos;
        }

        public void SetFeetPos(float x, float y) => SetFeetPos(new Vector2(x, y));
        public void SetFeetPos(Vector2 pos)
        {
            Position = OldPosition = new Vector2(
                pos.X - Size.X / 2f,
                pos.Y - Size.Y
            );
        }

        public void Spawn()
        {
            if (IsSpawned)
                return;

            IsSpawned = true;
            OnSpawn();
        }

        public void Destroy()
        {
            if (IsDestroyed)
                return;

            IsDestroyed = true;
            OnDestroy();
        }

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
            if (!IsCollidable)
                return;

            if (!horizontal)
            {
                IsGrounded = false;
            }

            for (int y = 0; y < World.Rows; y++)
            {
                for (int x = 0; x < World.Cols; x++)
                {
                    Tile tile = World.Tiles[y, x];
                    Rectangle tileRect = new(x * Tile.Size, y * Tile.Size, Tile.Size, Tile.Size);

                    if (Rect.X < tileRect.X + tileRect.Width
                        && Rect.X + Rect.Width > tileRect.X
                        && Rect.Y < tileRect.Y + tileRect.Height
                        && Rect.Y + Rect.Height > tileRect.Y)
                    {
                        if (tile.IsCollidable)
                        {
                            if (horizontal)
                            {
                                if (Velocity.X < 0f && !tile.CollisionExclude.Contains(CollisionSide.Right))
                                {
                                    // Left collision
                                    OldPosition.X = Position.X = tileRect.X + tileRect.Width;
                                    Velocity.X = 0f;
                                    OnTileCollision(tile, CollisionSide.Left);
                                }
                                else if (Velocity.X > 0f && !tile.CollisionExclude.Contains(CollisionSide.Left))
                                {
                                    // Right collision
                                    OldPosition.X = Position.X = tileRect.X - Rect.Width;
                                    Velocity.X = 0f;
                                    OnTileCollision(tile, CollisionSide.Right);
                                }
                            }
                            else
                            {
                                if (Velocity.Y < 0f && !tile.CollisionExclude.Contains(CollisionSide.Bottom))
                                {
                                    // Top collision
                                    OldPosition.Y = Position.Y = tileRect.Y + tileRect.Height;
                                    Velocity.Y = 0f;
                                    OnTileCollision(tile, CollisionSide.Top);
                                }
                                else if (Velocity.Y > 0f && !tile.CollisionExclude.Contains(CollisionSide.Top))
                                {
                                    // Bottom collision
                                    OldPosition.Y = Position.Y = tileRect.Y - Rect.Height;
                                    Velocity.Y = 0f;
                                    IsGrounded = true;
                                    OnTileCollision(tile, CollisionSide.Bottom);
                                }
                            }
                        }
                        else
                        {
                            OnTileOverlap(tile);
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

        protected abstract void OnSpawn();
        protected abstract void OnDestroy();

        protected virtual void OnTileCollision(Tile tile, CollisionSide side) { }
        protected virtual void OnTileOverlap(Tile tile) { }
    }
}

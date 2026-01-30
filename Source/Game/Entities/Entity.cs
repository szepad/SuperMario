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
        public Vector2 HitboxSize;
        public Vector2 HitboxOffset;

        public const float DefaultGravity = 650f * Game.TextureScale;
        public float Gravity { get; set; } = DefaultGravity;

        public Sprite Sprite { get; set; } = new();

        public Rectangle Rect => new(Position, Size);
        public Rectangle Hitbox => new(Position + HitboxOffset, HitboxSize);
        public Vector2 Center => Position + Size / 2f;
        public Vector2 Feet => new(Position.X + Size.X / 2f, Position.Y + Size.Y);
        public Vector2 Head => new(Position.X + Size.X / 2f, Position.Y);

        public LayerDepth LayerDepth { get; set; } = LayerDepth.Normal;
        public bool IsCollidable { get; set; } = true;
        public bool IsHittable { get; set; } = true;
        public bool IsGrounded { get; private set; }
        public bool IsSpawned { get; private set; }
        public bool IsDestroyed { get; private set; }
        public bool IsActive { get; private set; }

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

        private readonly List<Entity> _overlapDisabled = [];

        private bool _steppedUpFromHorizontalCollision;

        public void Spawn()
        {
            if (IsSpawned) return;
            IsSpawned = true;

            OnSpawn();
        }

        public void Destroy()
        {
            if (IsDestroyed) return;
            IsDestroyed = true;

            OnDestroy();
        }

        public void Activate()
        {
            if (IsActive) return;
            IsActive = true;

            OnActivate();
        }

        public void ActivateSurrounding(float activateRadius = 40f * Game.TextureScale)
        {
            foreach (Entity entity in World.Entities)
            {
                if (entity == this || entity.IsActive)
                    continue;

                Vector2 center = Position + Size / 2f;
                Vector2 otherCenter = entity.Position + entity.Size / 2f;

                if (Vector2.DistanceSquared(center, otherCenter) < activateRadius * activateRadius)
                {
                    entity.Activate();
                    entity.ActivateSurrounding();
                }
            }
        }

        public virtual void Update()
        {
            ApplyGravity();
            MoveAndCollide();
        }

        protected void ApplyGravity()
        {
            Velocity.Y += Gravity * Time.DeltaTime;
        }

        protected void MoveAndCollide()
        {
            _steppedUpFromHorizontalCollision = false;

            OldPosition = Position;
            Position.X += Velocity.X * Time.DeltaTime;
            HandleCollisions(horizontal: true);
            Position.Y += Velocity.Y * Time.DeltaTime;
            HandleCollisions(horizontal: false);

            Sprite.Update();

            if (Feet.Y > World.FallOffHeight)
            {
                Destroy();
            }
        }

        protected void HandleCollisions(bool horizontal)
        {
            if (!IsCollidable)
                return;

            if (!horizontal)
            {
                IsGrounded = false;
            }

            List<Tile> tilesToCheck = GetTilesForCollisionCheck();
            EnumerateCollisionTiles(tilesToCheck, (tile, tileRect) =>
            {
                if (tile.IsCollidable)
                {
                    if (horizontal)
                    {
                        const float MaxYBias = 3f * Game.TextureScale;
                        float yBias = Position.Y + Size.Y - tileRect.Y;
                        if (yBias >= 0 && yBias <= MaxYBias 
                            && MathF.Abs(Velocity.X) > 140f * Game.TextureScale
                            && (tile is not ContainerTile ct || !ct.IsHidden))
                        {
                            _steppedUpFromHorizontalCollision = true;
                        }
                        else
                        {
                            if (Position.X < OldPosition.X && !tile.CollisionExclude.Contains(CollisionSide.Right))
                            {
                                // Left collision
                                OldPosition.X = Position.X = tileRect.X + tileRect.Width;
                                Velocity.X = 0f;
                                OnTileCollision(tile, CollisionSide.Left);
                            }
                            else if (Position.X > OldPosition.X && !tile.CollisionExclude.Contains(CollisionSide.Left))
                            {
                                // Right collision
                                OldPosition.X = Position.X = tileRect.X - Rect.Width;
                                Velocity.X = 0f;
                                OnTileCollision(tile, CollisionSide.Right);
                            }
                        }
                    }
                    else
                    {
                        if (_steppedUpFromHorizontalCollision ||
                            (Position.Y > OldPosition.Y && !tile.CollisionExclude.Contains(CollisionSide.Top)))
                        {
                            // Bottom collision
                            OldPosition.Y = Position.Y = tileRect.Y - Rect.Height;
                            Velocity.Y = 0f;
                            IsGrounded = true;
                            OnTileCollision(tile, CollisionSide.Bottom);
                        }
                        else if (Position.Y < OldPosition.Y && !tile.CollisionExclude.Contains(CollisionSide.Bottom))
                        {
                            // Top collision
                            OldPosition.Y = Position.Y = tileRect.Y + tileRect.Height;
                            Velocity.Y = 0f;
                            OnTileCollision(tile, CollisionSide.Top);
                        }
                    }
                }
                else
                {
                    OnTileOverlap(tile);
                }
            });
        }

        protected List<Tile> GetTilesForCollisionCheck()
        {
            List<Tile> tilesToCheck = new(capacity: 16);
            Vector2 min = Vector2.Min(OldPosition, Position);
            Vector2 max = Vector2.Max(OldPosition, Position) + Size;

            for (int y = (int)(min.Y / Tile.Size) - 1; y <= (int)(max.Y / Tile.Size) + 1; y++)
            {
                for (int x = (int)(min.X / Tile.Size) - 1; x <= (int)(max.X / Tile.Size) + 1; x++)
                {
                    if (World.GetTile(x, y, out Tile tile)
                        && tile != SimpleTile.Air)
                    {
                        tilesToCheck.Add(tile);
                    }
                }
            }

            Vector2 center = Position + Size / 2f;
            tilesToCheck.Sort((t1, t2) =>
                Vector2.DistanceSquared(new Vector2(t1.Col + 0.5f, t1.Row + 0.5f) * Tile.Size, center)
                .CompareTo(Vector2.DistanceSquared(new Vector2(t2.Col + 0.5f, t2.Row + 0.5f) * Tile.Size, center))
                );

            return tilesToCheck;
        }

        protected void EnumerateCollisionTiles(List<Tile> tiles, Action<Tile, Rectangle> OnBroadCheck)
        {
            foreach (Tile tile in tiles)
            {
                Rectangle tileRect = new(tile.Col * Tile.Size, tile.Row * Tile.Size, Tile.Size, Tile.Size);

                if (Rect.X < tileRect.X + tileRect.Width
                    && Rect.X + Rect.Width > tileRect.X
                    && Rect.Y < tileRect.Y + tileRect.Height
                    && Rect.Y + Rect.Height > tileRect.Y)
                {
                    OnBroadCheck(tile, tileRect);
                }
            }
        }

        public void HandleEntityCollisions()
        {
            if (!IsHittable)
                return;

            for (int i = World.Entities.Count - 1; i >= 0; i--)
            {
                Entity other = World.Entities[i];
                if (other == this || !other.IsActive)
                    continue;

                bool disabled = _overlapDisabled.Contains(other);

                if (Raylib.CheckCollisionRecs(Hitbox, other.Hitbox))
                {
                    if (other.IsHittable && !disabled)
                    {
                        OnEntityOverlap(other);
                    }
                }
                else
                {
                    if (disabled)
                    {
                        _overlapDisabled.Remove(other);
                    }
                }
            }
        }

        public virtual void Draw()
        {
            if (!IsActive)
                return;

            Sprite.Draw(Position - SpriteOffset);
            DrawDebug();
        }

        protected virtual void DrawDebug()
        {
            if (!Game.Instance.IsDevModeOn)
                return;
            
            Rect.Draw(new Color(0, 255, 0, 128));
            Hitbox.Draw(new Color(255, 0, 0, 128));
        }

        public void DisableOverlapUntilCollisionLeave(Entity entity)
        {
            if (!_overlapDisabled.Contains(entity))
            {
                _overlapDisabled.Add(entity);
            }
        }

        public virtual void TileBounceHit(Entity bouncer, Tile tile) { }

        public virtual void OnCameraLeaveX() { }

        protected abstract void OnSpawn();
        protected virtual void OnActivate() { }
        protected virtual void OnDestroy() { }

        protected virtual void OnTileCollision(Tile tile, CollisionSide side) { }
        protected virtual void OnTileOverlap(Tile tile) { }
        protected virtual void OnEntityOverlap(Entity other) { }

        public virtual string SaveCustomData() => string.Empty;
        public virtual void LoadCustomData(string data) { }
    }
}

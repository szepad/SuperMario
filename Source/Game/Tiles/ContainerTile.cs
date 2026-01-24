namespace Mario2
{
    public abstract class ContainerTile(bool isCollidable) : AnimatedTile(isCollidable)
    {
        public bool IsHittable { get; set; } = true;
        public bool IsBreakable { get; set; } = false;
        
        public ItemType ItemInside { get; private set; } = ItemType.None;
        public bool IsHidden { get; private set; }

        protected bool OnlyHitOnce { get; set; } = true;

        public const float BounceTime = 0.2f;
        private const float BounceOffset = 6f * Game.TextureScale;

        private float _bounceTimer;
        private bool _isBouncing;

        public void SetItem(ItemType item)
        {
            ItemInside = item;
        }

        public void SetHidden()
        {
            if (!IsHittable)
                return;

            IsHidden = true;
            CollisionExclude.Add(CollisionSide.Top);
            CollisionExclude.Add(CollisionSide.Left);
            CollisionExclude.Add(CollisionSide.Right);
        }

        public void OnHit(Entity entity)
        {
            if (!IsHittable)
                return;

            _bounceTimer = 0f;
            _isBouncing = true;
            IsHittable = false;

            HandleEntityBounceHit(entity);
            HandleCoinTileBounceHit();
            HandleRevealing();
            HandleBreaking(entity);

            OnHitFirstFrame(entity);
        }

        public override void Update()
        {
            base.Update();
            HandleBounceLogic();
        }

        protected void HandleBounceLogic()
        {
            if (!_isBouncing)
                return;
            
            const float Half = BounceTime / 2f;
            float t = Helper.Clamp01(_bounceTimer > Half
                ? 1f - (_bounceTimer - Half) / Half
                : _bounceTimer / Half);

            SpriteOffset.Y = BounceOffset * t;

            _bounceTimer += Time.DeltaTime;
            if (_bounceTimer >= BounceTime)
            {
                _isBouncing = false;
                SpriteOffset.Y = 0f;
                if (!OnlyHitOnce) IsHittable = true;
                OnHitLastFrame();
            }
        }

        protected virtual void OnHitFirstFrame(Entity entity)
        {
            if (ItemInside == ItemType.Coin)
            {
                SpawnItemInside();
            }
        }

        protected virtual void OnHitLastFrame()
        {
            if (ItemInside != ItemType.None && ItemInside != ItemType.Coin)
            {
                SpawnItemInside();
            }
        }

        protected void SpawnItem(ItemType itemType)
        {
            if (World is null || itemType == ItemType.None)
                return;

            Item item = Item.Get(World, itemType);
            World.SpawnEntity(item, new Vector2((Col + 0.5f) * Size, (Row + 1f) * Size - Game.TextureScale));
        }

        protected void SpawnItemInside()
        {
            if (ItemInside == ItemType.Mushroom && World?.Player.State != PlayerState.Small)
                ItemInside = ItemType.FireFlower;

            SpawnItem(ItemInside);
            ItemInside = ItemType.None;
        }

        private void HandleEntityBounceHit(Entity bouncer)
        {
            Rectangle bounceRect = new(
                Col * Size, Row * Size - BounceOffset,
                Size, BounceOffset
            );

            for (int i = bouncer.World.Entities.Count - 1; i >= 0; i--)
            {
                Entity entity = bouncer.World.Entities[i];
                if (entity == bouncer || !entity.IsHittable || !entity.IsActive)
                    continue;

                if (Raylib.CheckCollisionRecs(entity.Rect, bounceRect))
                {
                    entity.TileBounceHit(bouncer, this);
                }
            }
        }

        private void HandleCoinTileBounceHit()
        {
            if (World is null)
                return;

            if (World.GetTile(Col, Row - 1, out Tile tile) && tile is CoinTile)
            {
                World.SetTile(Col, Row - 1, SimpleTile.Air);
                World.SpawnEntity(new CoinPickup(World), new Vector2(Col + 0.5f, Row) * Size);
            }
        }

        private void HandleRevealing()
        {
            if (!IsHidden) return;
            IsHidden = false;
            
            CollisionExclude.Clear();
        }

        private void HandleBreaking(Entity entity)
        {
            if (IsBreakable && ItemInside == ItemType.None
                && entity is Player player && player.State != PlayerState.Small)
            {
                entity.World.SetTile(Col, Row, SimpleTile.Air);

                Vector2 center = new Vector2(Col + 0.5f, Row + 0.5f) * Size;
                entity.World.SpawnEffect(center, new Vector2(-25f, -121f).Scale() * 1.75f, EffectData.Brick);
                entity.World.SpawnEffect(center, new Vector2(-33f, -175f).Scale() * 1.75f, EffectData.Brick);
                entity.World.SpawnEffect(center, new Vector2(25f, -121f).Scale() * 1.75f, EffectData.Brick);
                entity.World.SpawnEffect(center, new Vector2(33f, -175f).Scale() * 1.75f, EffectData.Brick);

                SoundManager.PlaySound(Sounds.BrickBreak);
                SessionManager.GetScoreWithoutText(ScoreRewards.BrickBreak);
            }
        }

        public override void Draw(int col, int row, float opacity = 1)
        {
            base.Draw(col, row, opacity);
        }
    }
}

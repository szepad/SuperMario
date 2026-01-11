namespace Mario2
{
    public abstract class ContainerTile : AnimatedTile
    {
        public bool IsHittable { get; set; } = true;
        
        public ItemType ItemInside { get; private set; } = ItemType.None;
        public bool IsHidden { get; private set; }

        protected bool OnlyHitOnce { get; set; } = true;

        public const float BounceTime = 0.2f;
        private const float BounceOffset = 6f * Game.TextureScale;

        private float _bounceTimer;
        private bool _isBouncing;

        public ContainerTile(bool isCollidable) : base("question", isCollidable)
        {
            Sprite.Add("?", new Animation(3, 16, 16, 0, 0.15f));
            Sprite.Add("block_ow", new Animation(1, 16, 16, 16, 1f));
            Sprite.Add("block_ug", new Animation(1, 16, 16, 32, 1f));
            Sprite.Add("block_castle", new Animation(1, 16, 16, 48, 1f));
            Sprite.Add("brick_ow", new Animation(1, 16, 16, 64, 1f));
            Sprite.Add("brick_ug", new Animation(1, 16, 16, 80, 1f));
            Sprite.Add("brick_castle", new Animation(1, 16, 16, 96, 1f));
        }

        public void SetItem(ItemType item)
        {
            ItemInside = item;
        }

        public void SetHidden()
        {
            if (!IsHittable)
                return;

            IsHidden = true;
            Sprite.IsVisible = false;
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

            OnHitFirstFrame(entity);

            if (IsHidden)
            {
                Sprite.IsVisible = true;
                CollisionExclude.Clear();
            }
        }

        public override void Update()
        {
            base.Update();

            if (_isBouncing)
            {
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

        private void SpawnItemInside()
        {
            if (World is null || ItemInside == ItemType.None)
                return;

            Item item = Item.Get(World, ItemInside);
            World.SpawnEntity(item, new Vector2((Col + 0.5f) * Size, (Row + 1f) * Size));
            ItemInside = ItemType.None;
        }

        private void HandleEntityBounceHit(Entity bouncer)
        {
            Rectangle bounceRect = new(
                Col * Size, Row * Size - BounceOffset,
                Size, BounceOffset
            );

            foreach (Entity entity in bouncer.World.Entities)
            {
                if (entity == bouncer || !entity.IsHittable)
                    continue;

                if (Raylib.CheckCollisionRecs(entity.Rect, bounceRect))
                {
                    entity.OnTileBounceHit(bouncer, this);
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
    }
}

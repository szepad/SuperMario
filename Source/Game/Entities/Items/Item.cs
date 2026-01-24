namespace Mario2
{
    public abstract class Item : Entity
    {
        protected enum AppearState
        {
            Wait,
            Appearing,
            Done,
        }

        public bool SlowAppear { get; }
        protected AppearState State { get; private set; }

        protected const float WaitBeforeAppearTime = ContainerTile.BounceTime / 2f;
        protected const float AppearSpeed = Tile.Size * 2f;
        private float _appearTimer = WaitBeforeAppearTime;

        public Item(World world, bool slowAppear = true) : base(world)
        {
            SlowAppear = slowAppear;
            State = slowAppear ? AppearState.Wait : AppearState.Done;

            Sprite.SetSheet("items");
        }

        public static Item Get(World world, ItemType type)
        {
            Type typeObj = Type.GetType($"{typeof(Item).Namespace}.{type}") ?? typeof(CoinPickup);
            Item item = (Item)Activator.CreateInstance(typeObj, world)!;
            return item;
        }

        public virtual void Pickup()
        {
            Destroy();
        }

        protected override void OnSpawn()
        {
            if (SlowAppear)
            {
                Sprite.IsVisible = false;
                LayerDepth = LayerDepth.Background;
                Gravity = 0f;
                IsCollidable = false;
                IsHittable = false;
                SoundManager.PlaySound(Sounds.AppearItem);
            }
        }

        protected virtual void OnAppear() { }

        public override void Update()
        {
            HandleAppearing();
            base.Update();
        }

        private void HandleAppearing()
        {
            if (State == AppearState.Done)
                return;

            _appearTimer -= Time.DeltaTime;
            if (_appearTimer <= 0f)
            {
                if (State == AppearState.Wait)
                {
                    State = AppearState.Appearing;
                    Sprite.IsVisible = true;
                    IsHittable = true;
                    Velocity.Y = -AppearSpeed;
                    _appearTimer = (Tile.Size * 0.99f) / AppearSpeed;
                }
                else if (State == AppearState.Appearing)
                {
                    State = AppearState.Done;
                    LayerDepth = LayerDepth.Normal;
                    Gravity = DefaultGravity;
                    IsCollidable = true;

                    // Do this so collision system snaps it upwards
                    Velocity.Y = 1f;
                    OldPosition.Y--; 
                    HandleCollisions(horizontal: false);
                    
                    OnAppear();
                }
            }
        }
    }
}

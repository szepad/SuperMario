namespace Mario2
{
    public class BrickTile : ContainerTile
    {
        private int _coinStashAmount = 10;

        public BrickTile() : base(true)
        {
            Sprite.Play("brick_ow");
            OnlyHitOnce = false;
            IsBreakable = true;
        }

        protected override void OnHitFirstFrame(Entity entity)
        {
            if (ItemInside == ItemType.CoinStash)
            {
                SpawnItem(ItemType.Coin);
                _coinStashAmount--;
                if (_coinStashAmount <= 0)
                {
                    Sprite.Play("block_ow");
                    OnlyHitOnce = true;
                }

                return;
            }

            if (ItemInside != ItemType.None)
            {
                Sprite.Play("block_ow");
                OnlyHitOnce = true;
            }

            base.OnHitFirstFrame(entity);
        }

        protected override void OnHitLastFrame()
        {
            if (ItemInside != ItemType.CoinStash)
            {
                base.OnHitLastFrame();
            }
        }
    }
}

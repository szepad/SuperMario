namespace Mario2
{
    public class BrickTile : ContainerTile
    {
        public TilePalette Palette { get; private set; }

        private int _coinStashAmount = 10;

        public BrickTile() : base(true)
        {
            DrawSprite = BrickSpriteOw;
            OnlyHitOnce = false;
            IsBreakable = true;
        }

        public void SetPalette(TilePalette palette)
        {
            Palette = palette;

            switch (Palette)
            {
                case TilePalette.Overworld: DrawSprite = BrickSpriteOw; break;
                case TilePalette.Underground: DrawSprite = BrickSpriteUg; break;
                case TilePalette.Castle: DrawSprite = BrickSpriteCastle; break;
            }
        }

        protected override void OnHitFirstFrame(Entity entity)
        {
            if (ItemInside == ItemType.CoinStash)
            {
                SpawnItem(ItemType.Coin);
                _coinStashAmount--;
                if (_coinStashAmount <= 0)
                {
                    DrawSprite = BlockSpriteOw;
                    OnlyHitOnce = true;
                }

                return;
            }

            if (ItemInside != ItemType.None)
            {
                DrawSprite = BlockSpriteOw;
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

        public override string GetSaveData()
        {
            return $"{base.GetSaveData()},{(int)Palette}";
        }

        public override void LoadSaveData(string data)
        {
            base.LoadSaveData(data);

            SetPalette((TilePalette)int.Parse(data.Split(',')[2]));
        }
    }
}

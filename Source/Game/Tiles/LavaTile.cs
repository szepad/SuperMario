namespace Mario2
{
    public class LavaTile : AnimatedTile
    {
        public LavaTile() : base(false)
        {
            DrawSprite = LavaTopSprite;
        }
    }

    public class LavaTileBottom : AnimatedTile
    {
        public LavaTileBottom() : base(false)
        {
            DrawSprite = LavaBottomSprite;
        }
    }
}

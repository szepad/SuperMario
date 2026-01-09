namespace Mario2
{
    public class CoinTile : AnimatedTile
    {
        public CoinTile() : base("items", false)
        {
            Sprite.Add("coin", new Animation(3, 16, 16, 0, 0.15f));
        }
    }
}

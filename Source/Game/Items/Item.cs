namespace Mario2
{
    public abstract class Item(World world) : Entity(world)
    {
        public static Item? Get(World world, ItemType type) => type switch
        {
            ItemType.Coin => new CoinPickup(world),
            _ => null
        };
    }
}

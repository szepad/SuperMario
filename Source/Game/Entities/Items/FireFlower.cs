namespace Mario2
{
    public class FireFlower : Item
    {
        public FireFlower(World world) : base(world)
        {
            Size = new Vector2(Tile.Size);
            Sprite.Add("fire_flower", new Animation(4, 16, 16, 32, 0.1f));
        }
    }
}

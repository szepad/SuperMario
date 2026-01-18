namespace Mario2
{
    public class FireFlower : Item
    {
        public FireFlower(World world) : base(world)
        {
            Size = HitboxSize = new Vector2(14f, 14f).Scale();
            SpriteOffset = new Vector2(1f).Scale();
            Sprite.Add("fire_flower", new Animation(4, 16, 16, 32, 0.07f));
        }
    }
}

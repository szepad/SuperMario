namespace Mario2
{
    public class Toad : Entity
    {
        public Toad(World world)
            : base(world)
        {
            Size = new Vector2(16f, 24f).Scale();

            Sprite.SetSheet("toad");
            Sprite.Add("idle", new Animation(1, 16, 24, 0, 0.1f));
        }

        protected override void OnSpawn()
        {
            
        }
    }
}

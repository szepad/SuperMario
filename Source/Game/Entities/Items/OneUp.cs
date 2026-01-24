namespace Mario2
{
    public class OneUp : Mushroom
    {
        public OneUp(World world) : base(world)
        {
            Sprite.Add("one_up", new Animation(1, 16, 16, 48, 1f));
            Sprite.Play("one_up");
        }

        public override void Pickup()
        {
            SessionManager.GetLife();
            base.Pickup();
        }
    }
}

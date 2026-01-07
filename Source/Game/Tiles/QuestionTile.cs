namespace Mario2
{
    public class QuestionTile : AnimatedTile
    {
        public QuestionTile() : base(true)
        {
            Sprite.Add("?", new Animation(3, 16, 16, 0, 0.15f, true));
        }
    }
}

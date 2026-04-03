namespace Mario2
{
    public class Bowser : Enemy
    {
        public int HitPoints { get; private set; } = 5;

        public const string WalkAnim = "walk";
        public const string SpitAnim_Prep = "spit_prep";
        public const string SpitAnim_Open = "spit_open";
        public const string DieAnim = "die";

        public Bowser(World world)
            : base(world)
        {
            Size = new Vector2(32f).Scale();
            SizeOffset = new Vector2(Size.X / 2f, Size.Y);
            SpriteOffset = new Vector2(16f, 40f).Scale();
            HitboxSize = new Vector2(16f, 26f).Scale();
            HitboxOffset = new Vector2(-HitboxSize.X / 2f, -HitboxSize.Y);

            IsStompable = false;
            KillableWithTileBounce = false;
            KillableWithFireball = false;

            Sprite.SetSheet("bowser");
            Sprite.Add(WalkAnim, new Animation(4, 32, 40, 0, 0.2f));
            Sprite.Add(SpitAnim_Prep, new Animation(1, 32, 40, 40, 0.2f));
            
            Animation openAnim = Sprite.Add(SpitAnim_Open, new Animation(1, 32, 40, 80, 0.5f));
            openAnim.OnAnimEnd += _ => Sprite.Play(WalkAnim);

            Sprite.Add(DieAnim, new Animation(2, 32, 40, 120, 0.1f));
            Sprite.FlipX = true;

            Gravity = DefaultGravity * 0.375f;

            SetAi(new BowserAi(this));
        }

        protected override void OnActivate()
        {
            foreach (Entity entity in World.Entities)
                if (entity is Platform)
                    SolidEntityCollisionExceptions.Add(entity);

            World.RequestMusic(MusicName.Bowser);
        }

        public void TakeDamage(Fireball fireball)
        {
            HitPoints--;
            if (HitPoints <= 0)
            {
                Die(fireball);
            }
        }

        protected override void OnDie()
        {
            Sprite.Play(DieAnim);
            base.OnDie();
        }

        public override void Update()
        {
            base.Update();
        }
    }
}

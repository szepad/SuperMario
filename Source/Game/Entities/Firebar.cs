namespace Mario2
{
    public enum FirebarLength
    {
        Short,
        Long
    }

    public class Firebar : Entity
    {
        public FirebarLength Length { get; private set; }
        public bool IsGoingRight { get; set; }

        private readonly List<FirebarChild> _children = [];
        private float _angleRad;

        public Firebar(World world)
            : base(world)
        {
            Size = new Vector2(Tile.Size);
            SizeOffset = Size / 2f;

            IsCollidable = false;
            IsHittable = false;
            Gravity = 0f;
        }

        protected override void OnSpawn()
        {
            SetLength(Length);
        }

        protected override void OnDestroy()
        {
            
        }

        private void ClearChildren()
        {
            foreach (FirebarChild child in _children)
            {
                child.Destroy();
                World.RemoveEntity(child);
            }

            _children.Clear();
        }

        public void SetLength(FirebarLength len)
        {
            Length = len;
            ClearChildren();

            int count = 6;
            if (len == FirebarLength.Long)
                count *= 2;

            for (int i = 0; i < count; i++)
            {
                _children.Add(World.SpawnEntity(
                    new FirebarChild(World, this)));
            }

            SetChildrenPositions();
        }

        private void SetChildrenPositions()
        {
            for (int i = 0; i < _children.Count; i++)
            {
                FirebarChild child = _children[i];
                Vector2 dir = new(MathF.Sin(_angleRad), -MathF.Cos(_angleRad));
                child.SetPosition(Position + dir * child.Size * i);
            }
        }

        public override void Update()
        {
            if (IsGoingRight)
            {
                _angleRad += MathF.PI / 2f * Time.DeltaTime;
                if (_angleRad >= MathF.Tau)
                    _angleRad -= MathF.Tau;
            }
            else
            {
                _angleRad -= MathF.PI / 2f * Time.DeltaTime;
                if (_angleRad <= -MathF.Tau)
                    _angleRad += MathF.Tau;
            }

            SetChildrenPositions();
            
            base.Update();
        }
    }

    public class FirebarChild : Entity
    {
        public Firebar Parent { get; }

        public FirebarChild(World world, Firebar parent) 
            : base(world)
        {
            Parent = parent;

            Gravity = 0f;
            IsCollidable = false;
            LayerDepth = LayerDepth.Foreground;

            Size = new Vector2(8f).Scale();
            SizeOffset = Size / 2f;
            SpriteOffset = Size;
            HitboxSize = new Vector2(4f).Scale();
            HitboxOffset = -HitboxSize / 2f;

            Sprite.SetSheet("fireball");
            Sprite.Add("fireball", new Animation(4, 16, 16, 0, 0.075f));
        }

        protected override void OnSpawn()
        {
            
        }
    }
}

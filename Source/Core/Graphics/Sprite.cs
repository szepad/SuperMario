namespace Mario2
{
    public record Animation(int FrameCount, int FrameWidth, int FrameHeight, 
        int Y, float[] FrameTimes, bool IsLooped = true)
    {
        public Animation(int frameCount, int width, int height,
            int y, float frameTime, bool isLooped = true)
            : this(frameCount, width, height, y, 
                  Enumerable.Repeat(frameTime, frameCount).ToArray(), isLooped)
        { }
    }

    public class Sprite
    {
        public Texture2D SpriteSheet { get; set; }

        public Vector2 Origin { get; set; } = Vector2.Zero;
        public Color Color { get; set; } = Color.White;
        public float Scale { get; set; } = 1f;
        public bool FlipX { get; set; }
        public bool FlipY { get; set; }

        public string CurrentAnimationName { get; private set; } = string.Empty;
        public Animation? CurrentAnimation { get; private set; }
        public int CurrentFrame { get; private set; }

        private readonly Dictionary<string, Animation> _anims = [];

        private float _frameTimer;

        public void SetSheet(string name)
        {
            SpriteSheet = Assets.GetTex(name);
        }

        public void Add(string name, Animation animation)
        {
            _anims.Add(name, animation);
            
            if (CurrentAnimation is null)
            {
                Play(name);
            }
        }

        public void Play(string name)
        {
            if (CurrentAnimationName == name)
                return;

            if (_anims.TryGetValue(name, out Animation? anim))
            {
                CurrentAnimation = anim;
                CurrentAnimationName = name;
                CurrentFrame = 0;
                _frameTimer = CurrentAnimation.FrameTimes[0];
            }
        }

        public void Update()
        {
            _frameTimer -= Time.DeltaTime;
            if (_frameTimer <= 0f)
            {
                ProgressNextFrame();
            }
        }

        public void ProgressNextFrame()
        {
            if (CurrentAnimation is null)
                return;

            CurrentFrame++;
            if (CurrentFrame >= CurrentAnimation.FrameCount)
            {
                if (CurrentAnimation.IsLooped) CurrentFrame = 0;
                else CurrentFrame--;
            }

            if (CurrentFrame < CurrentAnimation.FrameTimes.Length)
            {
                _frameTimer = CurrentAnimation.FrameTimes[CurrentFrame];
            }
            else
            {
                _frameTimer = CurrentAnimation.FrameTimes[^1];
            }
        }

        public Rectangle GetSourceRectangle()
        {
            if (CurrentAnimation is null)
                return default;

            return new Rectangle(
                CurrentAnimation.FrameWidth * CurrentFrame,
                CurrentAnimation.Y,
                FlipX ? -CurrentAnimation.FrameWidth : CurrentAnimation.FrameWidth,
                FlipY ? -CurrentAnimation.FrameHeight : CurrentAnimation.FrameHeight);
        }

        public void Draw(Vector2 position)
        {
            if (CurrentAnimation is null)
                return;

            Raylib.DrawTexturePro(SpriteSheet, GetSourceRectangle(),
                new Rectangle(position,
                new Vector2(CurrentAnimation.FrameWidth, CurrentAnimation.FrameHeight).Scale() * Scale),
                Origin, 0f, Color);
        }
    }
}

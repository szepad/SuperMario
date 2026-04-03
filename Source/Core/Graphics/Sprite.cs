namespace Mario2
{
    public record Animation(int FrameCount, int FrameWidth, int FrameHeight, 
        int Y, float[] FrameTimes, bool IsLooped = true)
    {
        public event Action<Animation>? OnAnimEnd;

        public Animation(int frameCount, int width, int height,
            int y, float frameTime, bool isLooped = true)
            : this(frameCount, width, height, y,
                  [.. Enumerable.Repeat(frameTime, frameCount)], isLooped)
        { }

        public void InvokeAnimEnd() => OnAnimEnd?.Invoke(this);
    }

    public class Sprite
    {
        public Texture2D SpriteSheet { get; set; }

        public Vector2 Origin { get; set; } = Vector2.Zero;
        public Color Color { get; set; } = Color.White;
        public float Scale { get; set; } = 1f;
        public float Opacity { get; set; } = 1f;
        public bool FlipX { get; set; }
        public bool FlipY { get; set; }
        public bool IsVisible { get; set; } = true;

        public string CurrentAnimationName { get; private set; } = string.Empty;
        public Animation? CurrentAnimation { get; private set; }
        public int CurrentFrame { get; private set; }
        public float CurrentFrameTimer { get; private set; }

        public IReadOnlyDictionary<string, Animation> Animations => _anims;
        private readonly Dictionary<string, Animation> _anims = [];

        public Sprite() { }
        public Sprite(string sheetName)
        {
            SetSheet(sheetName);
        }

        public Sprite(string sheetName, Animation animation) : this(sheetName)
        {
            Add("animation", animation);
        }

        public void SetSheet(string name)
        {
            SpriteSheet = Assets.GetTex(name);
        }

        public Animation Add(string name, Animation animation)
        {
            _anims.Add(name, animation);
            
            if (CurrentAnimation is null)
            {
                Play(name);
            }

            return animation;
        }

        public void ChangeAnim(string name, Animation animation)
        {
            if (!_anims.ContainsKey(name))
                return;

            _anims[name] = animation;
            ForcePlay(name);
        }

        public void Play(string name)
        {
            if (CurrentAnimationName == name)
                return;

            ForcePlay(name);
        }

        public void ForcePlay(string name)
        {
            if (!_anims.TryGetValue(name, out Animation? anim))
                return;

            CurrentAnimation = anim;
            CurrentAnimationName = name;
            CurrentFrame = 0;
            CurrentFrameTimer = 0f;
        }

        public void SetFrameTimer(float time)
        {
            CurrentFrameTimer = time;
        }

        public void SetFrame(int frame)
        {
            if (CurrentAnimation is null)
                return;

            if (frame >= CurrentAnimation.FrameCount)
            {
                if (CurrentAnimation.IsLooped) frame = 0;
                else frame--;

                CurrentAnimation.InvokeAnimEnd();
            }

            CurrentFrame = frame;
        }

        public void Update()
        {
            if (CurrentAnimation is null)
                return;

            CurrentFrameTimer += Time.DeltaTime;

            float frameTarget = CurrentFrame < CurrentAnimation.FrameTimes.Length
                ? CurrentAnimation.FrameTimes[CurrentFrame] 
                : CurrentAnimation.FrameTimes[^1];

            if (CurrentFrameTimer >= frameTarget)
            {
                ProgressNextFrame();
            }
        }

        public void ProgressNextFrame()
        {
            if (CurrentAnimation is null)
                return;

            SetFrame(CurrentFrame + 1);
            CurrentFrameTimer = 0f;
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

        public void Draw(Vector2 position, float opacityScale = 1f)
        {
            if (CurrentAnimation is null || !IsVisible)
                return;

            Raylib.DrawTexturePro(SpriteSheet, GetSourceRectangle(),
                new Rectangle(position,
                new Vector2(CurrentAnimation.FrameWidth, CurrentAnimation.FrameHeight).Scale() * Scale),
                Origin, 0f, Color.Alpha(Opacity * opacityScale));
        }
    }
}

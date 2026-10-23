namespace Mario2
{
    public abstract class Scene
    {
        public Scene? ParentScene { get; private set; }
        public Scene? SubScene { get; private set; }

        /// <summary>The innermost scene, i.e. the one that's actually updated and drawn.</summary>
        public Scene ActiveLeaf => SubScene?.ActiveLeaf ?? this;

        /// <summary>
        /// When true, the scene draws straight to the window (managing Begin/EndDrawing itself)
        /// instead of to the scaled virtual screen.
        /// </summary>
        public virtual bool DrawsToWindow => false;

        private bool _isLoaded;

        public void Load()
        {
            if (_isLoaded) return;
            _isLoaded = true;

            OnLoad();
        }

        public void Unload()
        {
            if (!_isLoaded) return;
            _isLoaded = false;

            UnloadSubScene();
            OnUnload();
        }

        public void LoadSubScene(Scene scene)
        {
            SubScene?.Unload();
            SubScene = scene;
            SubScene.ParentScene = this;
            SubScene.Load();

            OnSubSceneLoad();
        }

        public void UnloadSubScene()
        {
            SubScene?.Unload();
            SubScene = null;

            OnSubSceneUnload();
        }

        public void Update()
        {
            if (SubScene is not null)
            {
                SubScene.Update();
            }
            else
            {
                OnUpdate();
            }
        }

        public void PreDraw()
        {
            if (SubScene is not null)
            {
                SubScene.PreDraw();
            }
            else
            {
                OnPreDraw();
            }
        }

        public void Draw()
        {
            if (SubScene is not null)
            {
                SubScene.Draw();
            }
            else
            {
                OnDraw();
            }
        }

        protected abstract void OnLoad();
        protected abstract void OnUnload();

        protected abstract void OnUpdate();
        protected abstract void OnDraw();

        protected virtual void OnPreDraw() { }
        protected virtual void OnSubSceneLoad() { }
        protected virtual void OnSubSceneUnload() { }
    }
}

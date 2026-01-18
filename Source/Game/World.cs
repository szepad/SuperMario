using System.Diagnostics.CodeAnalysis;

namespace Mario2
{
    public class World
    {
        public const int Rows = 14;
        public const float FallOffHeight = Rows * 1.5f * Tile.Size;

        public int Cols { get; private set; } = 100;
        public WorldBackground Background { get; private set; }
        public ParallaxBackground Parallax { get; private set; }

        public Player Player { get; }
        public Tile[,] Tiles { get; private set; }
        public List<Entity> Entities { get; } = [];

        public bool IsFrozen => _freezeTimer > 0f;
        private float _freezeTimer;

        public World()
        {
            Player = new Player(this);
            Player.Position.X = 50;
            Entities.Add(Player);
            Player.Activate();

            Tiles = new Tile[Rows, Cols];
            Clear(Tiles);

            SetBackground(WorldBackground.Sky);
        }

        [MemberNotNull(nameof(Parallax))]
        public void SetBackground(WorldBackground background)
        {
            Background = background;
            Parallax = WorldBackgroundExtension.GetParallax(background);
        }

        public void Clear() => Clear(Tiles);
        public static void Clear(Tile[,] tiles)
        {
            for (int row = 0; row < tiles.GetLength(0); row++)
            {
                for (int col = 0; col < tiles.GetLength(1); col++)
                {
                    tiles[row, col] = SimpleTile.Air;
                }
            }
        }

        public bool GetTile(int col, int row, out Tile tile)
        {
            tile = SimpleTile.Air;
            if (col < 0 || col >= Cols || row < 0 || row >= Rows)
                return false;

            tile = Tiles[row, col];
            return true;
        }

        public Tile? SetTile(int col, int row, Tile tile)
        {
            if (col < 0 || col >= Cols || row < 0 || row >= Rows)
                return null;

            tile.World = this;
            tile.Col = col;
            tile.Row = row;
            Tiles[row, col] = tile;
            return tile;
        }

        public void SetCols(int newCols)
        {
            if (newCols < 0)
                return;

            Tile[,] newMtx = new Tile[Rows, newCols];
            Clear(newMtx);

            for (int col = 0; col < newCols; col++)
            {
                if (col >= Cols)
                    break;

                for (int row = 0; row < Rows; row++)
                {
                    newMtx[row, col] = Tiles[row, col];
                }
            }

            Cols = newCols;
            Tiles = newMtx;
        }

        public T SpawnEntity<T>(T entity, Vector2 feetPos) where T : Entity
        {
            Entities.Add(entity);
            entity.SetFeetPos(feetPos);
            entity.Spawn();
            return entity;
        }

        public T SpawnEntity<T>(T entity) where T : Entity
        {
            Entities.Add(entity);
            entity.Spawn();
            return entity;
        }

        public void Freeze(float time)
        {
            _freezeTimer = time;
        }

        public void HandleEntityActivation(Camera2D camera)
        {
            foreach (Entity entity in Entities)
            {
                if (entity.IsActive)
                    continue;

                if (entity.Rect.X <= camera.Target.X + camera.Offset.X)
                {
                    entity.Activate();
                    entity.ActivateSurrounding();
                }
            }
        }

        public void Update()
        {
            if (!IsFrozen)
            {
                UpdateEntities();
            }
            else
            {
                Player.UpdateIFrameTimer();
                Player.Sprite.Update();
                _freezeTimer -= Time.DeltaTime;
                RemoveDestroyedEntities();
            }

            UpdateTiles();
        }

        private void UpdateEntities()
        {
            for (int i = Entities.Count - 1; i >= 0; i--)
            {
                if (!Entities[i].IsActive)
                    continue;
                
                Entities[i].Update();
            }

            RemoveDestroyedEntities();

            // Handle entity collisions after every entity has moved for the frame
            foreach (Entity entity in Entities)
            {
                entity.HandleEntityCollisions();
            }
        }

        private void RemoveDestroyedEntities()
        {
            for (int i = Entities.Count - 1; i >= 0; i--)
            {
                if (Entities[i].IsDestroyed)
                {
                    Entities.RemoveAt(i);
                }
            }
        }

        private void UpdateTiles()
        {
            for (int y = 0; y < Rows; y++)
            {
                for (int x = 0; x < Cols; x++)
                {
                    Tile tile = Tiles[y, x];
                    if (tile == SimpleTile.Air)
                        continue;

                    tile.Update();
                    
                    if (tile.IsDestroyed)
                    {
                        Tiles[y, x] = SimpleTile.Air;
                    }
                }
            }
        }

        public void UpdateParallax(Camera2D camera)
        {
            Parallax.Update(camera);
        }

        public void DrawBackground()
        {
            Parallax.Draw();
        }

        public void Draw(bool editor = false)
        {
            //Entities.Sort((x, y) => x.LayerDepth.CompareTo(y.LayerDepth));
            List<Entity> drawList = [..Entities.OrderBy(x => x.LayerDepth)];

            int lastBg = -1;
            for (int i = 0; i < drawList.Count; i++)
            {
                Entity entity = drawList[i];
                if (entity.LayerDepth == LayerDepth.Background)
                {
                    drawList[i].Draw();
                    lastBg = i;
                }
                else
                {
                    break;
                }
            }

            DrawTiles(editor);

            for (int i = lastBg + 1; i < drawList.Count; i++)
            {
                drawList[i].Draw();
            }
        }

        private void DrawTiles(bool editor)
        {
            for (int y = 0; y < Rows; y++)
            {
                for (int x = 0; x < Cols; x++)
                {
                    Tile tile = Tiles[y, x];
                    if (tile == SimpleTile.Air)
                        continue;

                    DrawTile(tile, x, y, editor);
                }
            }
        }

        private void DrawTile(Tile tile, int x, int y, bool editor)
        {
            if (!editor)
            {
                tile.Draw(x, y);
                return;
            }

            tile.Draw(x, y, tile.IsCollidable ? 1f : 0.5f);
            if (tile is ContainerTile ct)
            {
                if (ct.IsHidden)
                {
                    ct.Sprite.IsVisible = true;
                    ct.Draw(x, y, 0.5f);
                    ct.Sprite.IsVisible = false;
                }

                if (ct.ItemInside != ItemType.None)
                {
                    Raylib.DrawTexturePro(Assets.GetTex("items"),
                        new Rectangle(0, ((int)ct.ItemInside - 1) * 16, 16, 16),
                        new Rectangle(x * Tile.Size, (y - 1) * Tile.Size, new Vector2(Tile.Size)),
                        Vector2.Zero, 0f, Color.White.Alpha(0.5f));
                }
            }
        }
    }
}

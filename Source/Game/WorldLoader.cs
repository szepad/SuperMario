namespace Mario2
{
    public static class WorldLoader
    {
        private const string TileSimpleAttr = "simple";
        private const string TileAnimAttr = "anim";

        public static void Serialize(World world, string path)
        {
            using StreamWriter writer = new(path);

            // Meta
            writer.WriteLine($"{world.Cols};{(int)world.Background};{(int)(world.Player.Position.X / Game.TextureScale)};{(int)(world.Player.Position.Y / Game.TextureScale)};{(int)world.Music};{world.IsPipeIntro};{world.TimeLeft}");

            // Tiles
            for (int row = 0; row < World.Rows; row++)
            {
                for (int col = 0; col < world.Cols; col++)
                {
                    Tile tile = world.Tiles[row, col];
                    if (tile is SimpleTile st)
                    {
                        writer.Write($"{TileSimpleAttr},{st.Id},{st.IsCollidable},{st.GetSaveData()}");
                    }
                    else
                    {
                        writer.Write($"{TileAnimAttr},{tile.GetType().FullName},{tile.GetSaveData()}");
                    }

                    writer.Write(";");
                }

                writer.WriteLine();
            }

            // Entities
            foreach (Entity entity in world.Entities)
            {
                if (!entity.IsEditorSaveable)
                    continue;

                writer.WriteLine($"{entity.GetType().FullName};{(int)(entity.Position.X / Game.TextureScale)};{(int)(entity.Position.Y / Game.TextureScale)};{entity.SaveCustomData()}");
            }
        }

        public static World? Deserialize(string path, out Exception? failEx)
        {
            failEx = null;

            try
            {
                return DeserializeImpl(path);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"WorldLoader.Deserialize failed to load '{path}'");
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.StackTrace);

                failEx = ex;
                return null;
            }
        }

        private static World DeserializeImpl(string path)
        {
            using StreamReader reader = new(path);
            World world = new() { LoadedPath = Path.GetFullPath(path) };

            // Meta
            string[] meta = reader.ReadLine()!.Split(";");
            world.SetCols(int.Parse(meta[0]));
            world.SetBackground((WorldBackground)int.Parse(meta[1]));
            world.PlayerSpawnPosition = new Vector2(int.Parse(meta[2]) * Game.TextureScale, int.Parse(meta[3]) * Game.TextureScale);
            world.Player.SetPosition(world.PlayerSpawnPosition);
            world.SetMusic((MusicName)int.Parse(meta[4]));
            world.IsPipeIntro = Convert.ToBoolean(meta[5]);
            world.TimeLeft = int.Parse(meta[6]);

            // Tiles
            for (int row = 0; row < World.Rows; row++)
            {
                string[] tiles = reader.ReadLine()!.Split(';');

                for (int col = 0; col < world.Cols; col++)
                {
                    string[] attr = tiles[col].Split(',');
                    if (attr[0] == TileSimpleAttr)
                    {
                        SimpleTile tile = new(
                            int.Parse(attr[1]), 
                            Convert.ToBoolean(attr[2]));

                        tile.LoadSaveData(attr[3]);
                        world.SetTile(col, row, tile.Id == -1 ? SimpleTile.Air : tile);
                    }
                    else if (attr[0] == TileAnimAttr)
                    {
                        Type? type = Type.GetType(attr[1]);
                        if (type is not null)
                        {
                            string data = attr[2];
                            for (int i = 3; i < attr.Length; i++)
                            {
                                data += $",{attr[i]}";
                            }

                            Tile tile = (Tile)(Activator.CreateInstance(type) ?? SimpleTile.Air);
                            tile.LoadSaveData(data);
                            world.SetTile(col, row, tile);
                        }
                    }
                }
            }

            // Entities
            string? line;
            while ((line = reader.ReadLine()) != null)
            {
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                string[] data = line.Split(';', 4);
                Type? type = Type.GetType(data[0]);
                if (type is null)
                {
                    Console.WriteLine($"World load error: Type not found: {data[0]}");
                    continue;
                }

                Entity? entity = (Entity?)Activator.CreateInstance(type, world);
                if (entity is null)
                {
                    Console.WriteLine($"World load error: Instantiation failed from type: {data[0]}");
                    continue;
                }

                entity.SetPosition(int.Parse(data[1]) * Game.TextureScale, int.Parse(data[2]) * Game.TextureScale);
                entity.LoadCustomData(data[3]);
                world.SpawnEntity(entity);
            }

            return world;
        }
    }
}

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
            writer.WriteLine($"{world.Cols};{(int)world.Background};{world.Player.Position.X};{world.Player.Position.Y};{(int)world.Music}");

            // Tiles
            for (int row = 0; row < World.Rows; row++)
            {
                for (int col = 0; col < world.Cols; col++)
                {
                    Tile tile = world.Tiles[row, col];
                    if (tile is SimpleTile st)
                    {
                        writer.Write($"{TileSimpleAttr},{st.Id},{st.IsCollidable}");
                    }
                    else
                    {
                        writer.Write($"{TileAnimAttr},{tile.GetType().FullName}");

                        if (tile is ContainerTile ct)
                        {
                            writer.Write($",{(int)ct.ItemInside},{ct.IsHidden}");
                        }
                    }

                    writer.Write(";");
                }

                writer.WriteLine();
            }

            // Entities
            foreach (Entity entity in world.Entities)
            {
                if (entity is Player)
                    continue;

                writer.WriteLine($"{entity.GetType().FullName};{(int)entity.Position.X};{(int)entity.Position.Y};{entity.SaveCustomData()}");
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
            int[] meta = [..reader.ReadLine()!.Split(";").Select(int.Parse)];
            world.SetCols(meta[0]);
            world.SetBackground((WorldBackground)meta[1]);
            world.Player.SetPosition(meta[2], meta[3]);
            world.SetMusic((MusicName)meta[4]);

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

                        world.SetTile(col, row, tile.Id == -1 ? SimpleTile.Air : tile);
                    }
                    else if (attr[0] == TileAnimAttr)
                    {
                        Type? type = Type.GetType(attr[1]);
                        if (type is not null)
                        {
                            Tile tile = (Tile)(Activator.CreateInstance(type) ?? SimpleTile.Air);
                            world.SetTile(col, row, tile);

                            if (tile is ContainerTile ct)
                            {
                                ct.SetItem((ItemType)int.Parse(attr[2]));
                                if (Convert.ToBoolean(attr[3]))
                                    ct.SetHidden();
                            }
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

                entity.SetPosition(int.Parse(data[1]), int.Parse(data[2]));
                entity.LoadCustomData(data[3]);
                world.SpawnEntity(entity);
            }

            return world;
        }
    }
}

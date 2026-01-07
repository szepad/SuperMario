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
            writer.WriteLine($"{world.Cols};{(int)world.Background}");

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
                    }

                    writer.Write(";");
                }

                writer.WriteLine();
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
            World world = new();

            // Meta
            int[] meta = [..reader.ReadLine()!.Split(";").Select(int.Parse)];
            world.SetCols(meta[0]);
            world.SetBackground((WorldBackground)meta[1]);

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
                        }
                    }
                }
            }

            return world;
        }
    }
}

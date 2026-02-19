namespace Mario2.Editor
{
    public abstract record PrefabAction;

    public record PrefabActionTile(int Col, int Row, Tile Tile, bool OnlyOnAir = true)
        : PrefabAction;

    public sealed record PrefabActionRecurse(int Col, int Row, Tile Tile, int OffX, int OffY, bool OnlyOnAir = true)
        : PrefabActionTile(Col, Row, Tile, OnlyOnAir);

    public record PrefabActionEntity(float RelXInPx, float RelYInPx, Type Type) 
        : PrefabAction;

    public sealed record PrefabTile(bool IsCollidable, int PreviewCol, int PreviewRow, 
        List<PrefabAction> Blueprint)
        : TileObject(PreviewCol, PreviewRow)
    {
        public override Tile OnPlace(World world, int col, int row)
        {
            Tile retTile = SimpleTile.Air;
            
            foreach (PrefabAction action in Blueprint)
            {
                if (action is PrefabActionTile tileAction)
                {
                    if (tileAction.Col == 0 && tileAction.Row == 0)
                        retTile = tileAction.Tile;

                    if (action is PrefabActionRecurse recurse)
                    {
                        bool stop;
                        int x = col;
                        int y = row;

                        const int MaxIter = 100;
                        int iterCount = 0;
                        do
                        {
                            stop = !PlaceTile(world, x, y, tileAction);
                            x += recurse.OffX;
                            y += recurse.OffY;
                            iterCount++;
                        }
                        while (!stop && iterCount < MaxIter);
                    }
                    else
                    {
                        PlaceTile(world, col, row, tileAction);
                    }
                }
                else if (action is PrefabActionEntity entityAction)
                {
                    Entity? entity = (Entity?)Activator.CreateInstance(entityAction.Type, world);
                    if (entity is not null)
                    {
                        entity.SetPosition(col * Tile.Size + entityAction.RelXInPx.Scale(),
                            row * Tile.Size + entityAction.RelYInPx.Scale());

                        if (EntityNotColliding(world, entity))
                        {
                            world.SpawnEntity(entity);
                        }
                    }
                }
            }

            return retTile;
        }

        private static bool PlaceTile(World world, int col, int row, PrefabActionTile action)
        {
            if (action.OnlyOnAir && world.GetTile(col + action.Col, row + action.Row, out Tile tile)
                    && tile != SimpleTile.Air)
                return false;

            world.SetTile(col + action.Col, row + action.Row, action.Tile);
            return true;
        }

        private static bool EntityNotColliding(World world, Entity entity)
        {
            foreach (Entity other in world.Entities)
            {
                if (Raylib.CheckCollisionRecs(entity.Rect, other.Rect))
                {
                    return false;
                }
            }

            return true;
        }
    }
}

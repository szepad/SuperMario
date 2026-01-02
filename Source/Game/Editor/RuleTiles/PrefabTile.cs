using System;

namespace Mario2.Editor
{
    public record PrefabAction(int Col, int Row, Tile Tile, bool OnlyOnAir = true);
    public sealed record PrefabActionRecurse(int Col, int Row, Tile Tile, int OffX, int OffY, bool OnlyOnAir = true)
        : PrefabAction(Col, Row, Tile, OnlyOnAir);

    public sealed record PrefabTile(bool IsCollidable, int PreviewCol, int PreviewRow, 
        List<PrefabAction> Blueprint)
        : TileObject(PreviewCol, PreviewRow)
    {
        public static readonly PrefabTile Pipe = new(true, 0, 3,
        [
            new PrefabAction(0, 0, new SimpleTile(24, true)),
            new PrefabAction(1, 0, new SimpleTile(25, true)),
            new PrefabActionRecurse(0, 1, new SimpleTile(32, true), 0, 1),
            new PrefabActionRecurse(1, 1, new SimpleTile(33, true), 0, 1),
        ]);

        public override Tile OnPlace(World world, int col, int row)
        {
            Tile retTile = SimpleTile.Air;
            
            foreach (PrefabAction action in Blueprint)
            {
                if (action.Col == 0 && action.Row == 0)
                    retTile = action.Tile;

                if (action is PrefabActionRecurse recurse)
                {
                    bool stop;
                    int x = col;
                    int y = row;
                    do
                    {
                        stop = !Place(world, x, y, action);
                        x += recurse.OffX;
                        y += recurse.OffY;
                    }
                    while (!stop);
                }
                else
                {
                    Place(world, col, row, action);
                }
            }

            return retTile;
        }

        private static bool Place(World world, int col, int row, PrefabAction action)
        {
            if (action.OnlyOnAir && world.GetTile(col + action.Col, row + action.Row, out Tile tile)
                    && tile != SimpleTile.Air)
                return false;

            world.SetTile(col + action.Col, row + action.Row, action.Tile);
            return true;
        }
    }
}

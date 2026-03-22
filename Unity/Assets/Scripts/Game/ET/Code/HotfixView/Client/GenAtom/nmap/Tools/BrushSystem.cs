using Unity.Mathematics;

namespace ET
{
    [FriendOf(typeof(Brush))]
    [EntitySystemOf(typeof(Brush))]
    public static partial class BrushSystem
    {
        [EntitySystem]
        private static void Awake(this Brush self)
        {
        }

        public static void Init(this Brush self)
        {
            self.UVMap.Clear();
            self.UV2Map.Clear();

            for (int j = 0; j < UVTileCover.TileCount; j++)
            {
                for (int i = 0; i < UVTileCover.TileCount; i++)
                {
                    UVTileCover uv = new UVTileCover(new int2(i, j));
                    self.UVMap.Add(uv.Id, uv);
                }
            }

            for (int j = 0; j < UVTileMain.TileCount; j++)
            {
                for (int i = 0; i < UVTileMain.TileCount; i++)
                {
                    UVTileMain uvTile = new UVTileMain(new int2(i, j));
                    self.UV2Map.Add(uvTile.Id, uvTile);
                }
            }
        }
    }
}

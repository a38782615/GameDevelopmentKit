using System.Collections.Generic;
using Unity.Mathematics;

namespace ET
{
    // Biome 的辅助属性表：
    // 1. Colors 用于地图可视化着色
    // 2. Chinese 用于界面或日志里的中文展示
    public static class BiomeProperties
    {
        [StaticField]
        public static Dictionary<Biome, float4> Colors = new Dictionary<Biome, float4>
        {
            { Biome.Ocean, HexToColor("44447a") },
            //{ COAST, HexToColor("33335a") },
            //{ LAKESHORE, HexToColor("225588") },
            { Biome.Lake, HexToColor("336699") },
            //{ RIVER, HexToColor("225588") },
            { Biome.Marsh, HexToColor("2f6666") },
            { Biome.Ice, HexToColor("99ffff") },
            { Biome.Beach, HexToColor("a09077") },
            //{ ROAD1, HexToColor("442211") },
            //{ ROAD2, HexToColor("553322") },
            //{ ROAD3, HexToColor("664433") },
            //{ BRIDGE, HexToColor("686860") },
            //{ LAVA, HexToColor("cc3333") },
            { Biome.Snow, HexToColor("ffffff") },
            { Biome.Tundra, HexToColor("bbbbaa") },
            { Biome.Bare, HexToColor("888888") },
            { Biome.Scorched, HexToColor("555555") },
            { Biome.Taiga, HexToColor("99aa77") },
            { Biome.Shrubland, HexToColor("889977") },
            { Biome.TemperateDesert, HexToColor("c9d29b") },
            { Biome.TemperateRainForest, HexToColor("448855") },
            { Biome.TemperateDeciduousForest, HexToColor("679459") },
            { Biome.Grassland, HexToColor("88aa55") },
            { Biome.SubtropicalDesert, HexToColor("d2b98b") },
            { Biome.TropicalRainForest, HexToColor("337755") },
            { Biome.TropicalSeasonalForest, HexToColor("559944") }
        };

        [StaticField]
        public static Dictionary<Biome, string> Chinese = new Dictionary<Biome, string>
        {
            { Biome.Ocean,"海洋"},
            //{ COAST, HexToColor("33335a") },
            //{ LAKESHORE, HexToColor("225588") },
            { Biome.Lake, "湖泊"},
            //{ RIVER, HexToColor("225588") },
            { Biome.Marsh, "沼泽"},
            { Biome.Ice, "冰原"},
            { Biome.Beach, "海滩"},
            //{ ROAD1, HexToColor("442211") },
            //{ ROAD2, HexToColor("553322") },
            //{ ROAD3, HexToColor("664433") },
            //{ BRIDGE, HexToColor("686860") },
            //{ LAVA, HexToColor("cc3333") },
            { Biome.Snow, "雪山"},
            { Biome.Tundra, "冻原"},
            { Biome.Bare, "荒原"},
            { Biome.Scorched, "焦土"},
            { Biome.Taiga, "针叶林"},
            { Biome.Shrubland,"灌木丛"},
            { Biome.TemperateDesert, "温带沙漠"},
            { Biome.TemperateRainForest, "温带雨林"},
            { Biome.TemperateDeciduousForest, "温带落叶林"},
            { Biome.Grassland, "草原"},
            { Biome.SubtropicalDesert, "亚热带沙漠"},
            { Biome.TropicalRainForest, "热带雨林"},
            { Biome.TropicalSeasonalForest, "热带季雨林"},
        };

        // 将十六进制 RGB 字符串转换为 Unity.Mathematics.float4 颜色。
        static float4 HexToColor(string hex)
        {
            byte r = byte.Parse(hex.Substring(0, 2), System.Globalization.NumberStyles.HexNumber);
            byte g = byte.Parse(hex.Substring(2, 2), System.Globalization.NumberStyles.HexNumber);
            byte b = byte.Parse(hex.Substring(4, 2), System.Globalization.NumberStyles.HexNumber);
            return new float4(r / 255f, g / 255f, b / 255f, 1);
        }
    }

    // 地图中心点的群系类型。
    // 具体取值通常由海陆、水分、温度等条件共同决定。
    public enum Biome
    {
        // 海洋
        Ocean,
        // 沼泽
        Marsh,
        // 冰原/冻水区域
        Ice,
        // 湖泊
        Lake,
        // 海岸沙滩
        Beach,
        // 雪地
        Snow,
        // 草原
        Grassland,
        // 灌木地
        Shrubland,
        // 冻原
        Tundra,
        // 裸岩荒地
        Bare,
        // 焦土
        Scorched,
        // 针叶林
        Taiga,
        // 温带沙漠
        TemperateDesert,
        // 温带雨林
        TemperateRainForest,
        // 温带落叶林
        TemperateDeciduousForest,
        // 热带雨林
        TropicalRainForest,
        // 热带季雨林
        TropicalSeasonalForest,
        // 亚热带沙漠
        SubtropicalDesert
    }
}

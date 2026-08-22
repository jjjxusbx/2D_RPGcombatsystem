using System.Collections.Generic;

namespace TurnBased
{
    /// <summary>房间数据：边界与中心，供生成器与自检使用。</summary>
    public sealed class RoomInfo
    {
        public string Name;
        public GridPosition Min;
        public GridPosition Max;

        public GridPosition Center => new GridPosition((Min.X + Max.X) / 2, (Min.Y + Max.Y) / 2);
    }

    /// <summary>
    /// 楼层数据模型：宽高 + 可行走二维数组 + 房间列表。
    /// 程序化地图生成（元胞自动机等）可在后续里程碑实现为 ILevelGenerator 并替换此处来源。
    /// </summary>
    public sealed class LevelData
    {
        public int Width;
        public int Height;
        public bool[,] Walkable;
        public List<RoomInfo> Rooms = new List<RoomInfo>();

        /// <summary>从 ASCII 模板构造：'#' 为墙，'.' 为地板。</summary>
        public static LevelData FromTemplate(string[] rows)
        {
            int height = rows.Length;
            int width = rows[0].Length;
            var data = new LevelData
            {
                Width = width,
                Height = height,
                Walkable = new bool[width, height],
            };
            for (int y = 0; y < height; y++)
            {
                string row = rows[y];
                for (int x = 0; x < width; x++)
                {
                    data.Walkable[x, y] = row[x] == '.';
                }
            }
            return data;
        }
    }
}
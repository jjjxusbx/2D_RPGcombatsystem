namespace TurnBased
{
    /// <summary>
    /// 简单楼层生成器（框架阶段）：提供一张硬编码的 U 形房间测试地图，
    /// 用于验证寻路绕障与视野遮挡。后续里程碑可替换为元胞自动机 / 房间拼接生成器。
    /// </summary>
    public static class SimpleLevelGenerator
    {
        public static LevelData GenerateDemoLevel()
        {
            string[] rows =
            {
                "##############",
                "#............#",
                "#..#####.....#",
                "#..#.........#",
                "#..#.........#",
                "#..#.........#",
                "#..#####.....#",
                "#............#",
                "#............#",
                "##############",
            };

            var level = LevelData.FromTemplate(rows);
            level.Rooms.Add(new RoomInfo
            {
                Name = "左侧 U 形房间（右侧开口，需绕行进入）",
                Min = new GridPosition(1, 1),
                Max = new GridPosition(12, 8),
            });
            return level;
        }
    }
}
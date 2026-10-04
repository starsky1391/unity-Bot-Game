using UnityEngine;

namespace HollowDemo
{
    public static class ChineseFont
    {
        static Font font;
        public static Font Shared => font != null ? font : font = Font.CreateDynamicFontFromOSFont(
            new[] { "Microsoft YaHei", "SimHei", "Noto Sans CJK SC" }, 24);
        public static readonly string[] EnemyNames = { "巡逻虫", "追逐虫", "冲撞虫", "跳跃虫", "射击虫", "盾甲虫", "重锤虫", "钻地虫", "追踪飞虫", "俯冲飞虫" };
    }
}

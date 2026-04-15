

namespace RefactorHeatAlertPostGre.Models.Enums
{
    public enum DangerLevel
    {
        Cool = 0,           // Below 26°C - Blue
        Normal = 1,         // 26-37°C - Emerald/Green
        Caution = 2,        // 38-41°C - Amber/Yellow
        Danger = 3,         // 42-48°C - Orange
        ExtremeDanger = 4   // 49°C+ - Red
    }

    public static class DangerLevelExtensions
    {
        public static DangerLevel GetDangerLevel(this int heatIndex)
        {
            return heatIndex switch
            {
                >= 49 => DangerLevel.ExtremeDanger,
                >= 42 => DangerLevel.Danger,
                >= 38 => DangerLevel.Caution,
                >= 26 => DangerLevel.Normal,
                _ => DangerLevel.Cool
            };
        }

        public static string GetEmoji(this DangerLevel level)
        {
            return level switch
            {
                DangerLevel.ExtremeDanger => "🔴",
                DangerLevel.Danger => "🟠",
                DangerLevel.Caution => "🟡",
                DangerLevel.Normal => "🟢",
                DangerLevel.Cool => "🔵",
                _ => "⚪"
            };
        }

        public static string GetDisplayName(this DangerLevel level)
        {
            return level switch
            {
                DangerLevel.ExtremeDanger => "EXTREME DANGER",
                DangerLevel.Danger => "DANGER",
                DangerLevel.Caution => "CAUTION",
                DangerLevel.Normal => "NORMAL",
                DangerLevel.Cool => "COOL",
                _ => "UNKNOWN"
            };
        }
    }
}
using BepInEx.Configuration;
using BepInEx;

namespace Hikaria.DamageAnalyzer.Config
{
    internal static class ConfigManager
    {
        public static void Setup()
        {
            Language = _config.Bind("语言设置", "语言", LanguageType.zh_CN, "语言 zh-CN 或 en-US.");
            BarFontSize = _config.Bind("字体大小设置", "BarFontSize", 13, "生命值条字体大小");
            HintFontSize = _config.Bind("字体大小设置", "HintFontSize", 13, "提示字体大小");
            BarLength = _config.Bind("通用设置", "HPBarLength", 70, "生命值条长度 (字符个数)");
            LabelWidth = _config.Bind("通用设置", "LabelWidth", 720, "宽度");
            LabelHeight = _config.Bind("通用设置", "LabelHeight", 120, "高度");
            LastingTime = _config.Bind("通用设置", "LastingTime", 2.5f, "显示持续时间");
            LabelWidthOffset = _config.Bind("通用设置", "LabelWidthOffset", 40, "宽度偏移值");
            LabelHeightOffset = _config.Bind("通用设置", "LabelHeightOffset", 92, "高度偏移值");
            ShowHints = _config.Bind("通用设置", "ShowHints", true, "显示额外信息");
            ShowHPBar = _config.Bind("生命值条设置", "ShowHPBar", true, "显示 HP Bar");
            BarFillChar_Remaining = _config.Bind("生命值条设置", "剩余血量显示字符", "#", "生命值条填充字符 (剩余的HP)");
            BarFillChar_Losted = _config.Bind("生命值条设置", "损失血量显示字符", "=", "生命值条填充字符 (失去的HP)");
            ShowName = _config.Bind("命中信息设置", "ShowName", true, "显示命中敌人名称");
            ShowPos = _config.Bind("命中信息设置", "ShowPos", true, "显示命中部位名称");
            ShowDamage = _config.Bind("命中信息设置", "ShowDamage", true, "显示每次命中的伤害");
            ShowSentryDamage = _config.Bind("命中信息设置", "ShowSentryDamage", true, "显示哨戒炮的命中信息, 仅当房主也安装了本插件才能显示");
        }

        public static ConfigEntry<LanguageType> Language { get; set; }

        public static ConfigEntry<float> LastingTime { get; set; }

        public static ConfigEntry<int> BarFontSize { get; set; }

        public static ConfigEntry<int> HintFontSize { get; set; }

        public static ConfigEntry<int> BarLength { get; set; }

        public static ConfigEntry<int> LabelWidthOffset { get; set; }

        public static ConfigEntry<int> LabelHeightOffset { get; set; }

        public static ConfigEntry<int> LabelWidth { get; set; }

        public static ConfigEntry<int> LabelHeight { get; set; }

        public static ConfigEntry<bool> ShowHints { get; set; }

        public static ConfigEntry<string> BarFillChar_Remaining { get; set; }

        public static ConfigEntry<string> BarFillChar_Losted { get; set; }

        public static ConfigEntry<bool> ShowHPBar { get; set; }

        public static ConfigEntry<bool> ShowName { get; set; }

        public static ConfigEntry<bool> ShowPos { get; set; }

        public static ConfigEntry<bool> ShowDamage { get; set; }

        public static ConfigEntry<bool> ShowSentryDamage { get; set; }

        private static readonly ConfigFile _config = new(string.Concat(Paths.ConfigPath, "\\Hikaria\\DamageAnalyzer\\Config.cfg"), true);

        public enum LanguageType : byte
        {
            en_US,
            zh_CN
        }
    }
}

namespace BBDown.Core
{
    public class Config
    {
        //For WEB
        public static string COOKIE { get; set; } = "";
        //For APP/TV
        public static string TOKEN { get; set; } = "";
        //日志级别
        public static bool DEBUG_LOG { get; set; } = false;
        //BiliPlus Host
        public static string HOST { get; set; } = "api.bilibili.com";
        //BiliPlus EP Host
        public static string EPHOST { get; set; } = "api.bilibili.com";
        //BiliPlus Area
        public static string AREA { get; set; } = "";

        public static string WBI { get; set; } = "";

        public static readonly Dictionary<string, string> qualitys = new() {
            {"127","8K 超高清" }, {"126","杜比视界" }, {"125","HDR 真彩" }, {"120","4K 超清" }, {"116","1080P 高帧率" },
            {"112","1080P 高码率" }, {"100","智能修复" }, {"80","1080P 高清" }, {"74","720P 高帧率" },
            {"64","720P 高清" }, {"48","720P 高清" }, {"32","480P 清晰" }, {"16","360P 流畅" },
            {"5","144P 流畅" }, {"6","240P 流畅" }
        };
    }
}

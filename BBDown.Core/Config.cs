using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

        public static Dictionary<string, string> qualitys = new Dictionary<string, string>() {
            {"127","8K 超高清" }, {"126","杜比视界" }, {"125","HDR 真彩" }, {"120","4K 超清" }, {"116","1080P 高帧率" },
            {"112","1080P 高码率" }, {"80","1080P 高清" }, {"74","720P 高帧率" },
            {"64","720P 高清" }, {"48","720P 高清" }, {"32","480P 清晰" }, {"16","360P 流畅" }
        };
    }
}

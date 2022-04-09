using static BBDown.Core.Logger;
using System.Text;
using System.Xml;

namespace BBDown.Core
{
    public class DanmakuUtil
    {
        private const int MONITOR_WIDTH = 1920;         //渲染字幕时的渲染范围的高度
        private const int MONITOR_HEIGHT = 1080;        //渲染字幕时的渲染范围的高度
        private const int FONT_SIZE = 40;               //字体大小
        private const double MOVE_SPEND_TIME = 8.00;    //单条条滚动弹幕存在时间（控制速度）
        private const double TOP_SPEND_TIME = 4.00;     //单条顶部或底部弹幕存在时间
        private const int PROTECT_LENGTH = 50;          //滚动弹幕屏占百分比
        public static readonly DanmakuComparer comparer = new DanmakuComparer();

        /*public static async Task DownloadAsync(Page p, string xmlPath, bool aria2c, string aria2cProxy)
        {
            string danmakuUrl = "https://comment.bilibili.com/" + p.cid + ".xml";
            await DownloadFile(danmakuUrl, xmlPath, aria2c, aria2cProxy);
        }*/

        public static DanmakuItem[] ParseXml(string xmlPath)
        {
            // 解析xml文件
            XmlDocument xmlFile = new XmlDocument();
            XmlReaderSettings settings = new XmlReaderSettings();
            settings.IgnoreComments = true;//忽略文档里面的注释
            var danmakus = new List<DanmakuItem>();
            using (var reader = XmlReader.Create(xmlPath, settings))
			{
                try
                {
                    xmlFile.Load(reader);
                }
                catch (Exception ex)
                {
                    LogDebug("解析字幕xml时出现异常: {0}", ex.ToString());
                    return null;
				}
            }

            XmlNode? rootNode = xmlFile.SelectSingleNode("i");
            if (rootNode != null)
            {
                XmlElement rootElement = (XmlElement)rootNode;
                XmlNodeList? dNodeList = rootElement.SelectNodes("d");
                if (dNodeList != null)
                {
                    foreach (XmlNode node in dNodeList)
                    {
                        XmlElement dElement = (XmlElement)node;
                        string attr = dElement.GetAttribute("p").ToString();
                        if (attr != null)
                        {
                            string[] vs = attr.Split(',');
                            if (vs.Length >= 8)
                            {
                                DanmakuItem danmaku = new(vs, dElement.InnerText);
                                danmakus.Add(danmaku);
                            }
                        }
                    }
                }
            }
            return danmakus.ToArray();
        }

        /// <summary>
        /// 保存为ASS字幕文件
        /// </summary>
        /// <param name="danmakus">弹幕</param>
        /// <param name="outputPath">保存路径</param>
        /// <returns></returns>
        public static async Task SaveAsAssAsync(DanmakuItem[] danmakus, string outputPath)
        {
            var sb = new StringBuilder();
            // ASS字幕文件头
            sb.AppendLine("[Script Info]");
            sb.AppendLine("Script Updated By: BBDown(https://github.com/nilaoda/BBDown)");
            sb.AppendLine("ScriptType: v4.00+");
            sb.AppendLine($"PlayResX: {MONITOR_WIDTH}");
            sb.AppendLine($"PlayResY: {MONITOR_HEIGHT}");
            sb.AppendLine($"Aspect Ratio: {MONITOR_WIDTH}:{MONITOR_HEIGHT}");
            sb.AppendLine("Collisions: Normal");
            sb.AppendLine("WrapStyle: 2");
            sb.AppendLine("ScaledBorderAndShadow: yes");
            sb.AppendLine("YCbCr Matrix: TV.601");
            sb.AppendLine("[V4+ Styles]");
            sb.AppendLine("Format: Name, Fontname, Fontsize, PrimaryColour, SecondaryColour, OutlineColour, BackColour, Bold, Italic, Underline, StrikeOut, ScaleX, ScaleY, Spacing, Angle, BorderStyle, Outline, Shadow, Alignment, MarginL, MarginR, MarginV, Encoding");
            sb.AppendLine($"Style: BBDOWN_Style, 宋体, {FONT_SIZE}, &H00FFFFFF, &H00FFFFFF, &H00000000, &H00000000, 0, 0, 0, 0, 100, 100, 0.00, 0.00, 1, 2, 0, 7, 0, 0, 0, 0");
            sb.AppendLine("[Events]");
            sb.AppendLine("Format: Layer, Start, End, Style, Name, MarginL, MarginR, MarginV, Effect, Text");
            
            PositionController controller = new PositionController();   // 弹幕位置控制器
            Array.Sort(danmakus, comparer);
            foreach (DanmakuItem danmaku in danmakus)
            {
                int height = controller.updatePosition(danmaku.DanmakuMode, danmaku.Second, danmaku.Content.Length);
                if (height == -1) continue;
                string effect = "";
                switch (danmaku.DanmakuMode)
                {
                    case 3:
                        effect += $"\\an8\\pos({MONITOR_WIDTH / 2}, {MONITOR_HEIGHT - FONT_SIZE - height})";
                        break;
                    case 2:
                        effect += $"\\an8\\pos({MONITOR_WIDTH / 2}, {height})";
                        break;
                    default:
                        effect += $"\\move({MONITOR_WIDTH}, {height}, {-danmaku.Content.Length * FONT_SIZE}, {height})";
                        break;
                }
                if (danmaku.Color != "FFFFFF")
                {
                    effect += $"\\c&{danmaku.Color}&";
                }
                sb.AppendLine($"Dialogue: 2,{danmaku.StartTime},{danmaku.EndTime},BBDOWN_Style,,0000,0000,0000,,{{{effect}}}{danmaku.Content}");
            }

            await File.WriteAllTextAsync(outputPath, sb.ToString(), Encoding.UTF8);
        }

        protected class PositionController
        {
            int maxLine = MONITOR_HEIGHT * PROTECT_LENGTH / FONT_SIZE / 100;    //总行数
            // 三个位置的弹幕队列，记录弹幕结束时间
            List<double> moveQueue = new List<double>();
            List<double> topQueue = new List<double>();
            List<double> bottomQueue = new List<double>();

            public PositionController()
            {
                for (int i = 0; i < maxLine; i++)
                {
                    moveQueue.Add(0.00);
                    topQueue.Add(0.00);
                    bottomQueue.Add(0.00);
                }
            }

            public int updatePosition(int type, double time, int length)
            {
                // 获取可用位置
                List<double> vs;
                double displayTime = TOP_SPEND_TIME;
                if (type == POS_BOTTOM)
                {
                    vs = bottomQueue;
                }
                else if (type == POS_TOP)
                {
                    vs = topQueue;
                }
                else
                {
                    vs = moveQueue;
                    displayTime = MOVE_SPEND_TIME * (length + 5) * FONT_SIZE / (MONITOR_WIDTH + length * MOVE_SPEND_TIME);
                }
                for (int i = 0; i < maxLine; i++)
                {
                    if (time >= vs[i])
                    {   // 此条弹幕已结束，更新该位置信息
                        vs[i] = time + displayTime;
                        return i * FONT_SIZE;
                    }
                }
                return -1;
            }
        }

        public class DanmakuItem
        {
            public DanmakuItem(string[] attrs, string content)
            {
                switch (attrs[1])
                {
                    case "4":
                        DanmakuMode = POS_BOTTOM;
                        break;
                    case "5":
                        DanmakuMode = POS_TOP;
                        break;
                    default:
                        DanmakuMode = POS_MOVE;
                        break;
                }
                try
                {
                    double second = double.Parse(attrs[0]);
                    Second = second;
                    StartTime = computeTime(second);
                    EndTime = computeTime(second + (DanmakuMode == 1 ? MOVE_SPEND_TIME : TOP_SPEND_TIME));
                }
                catch (Exception e)
                {
                    Log(e.Message);
                }
                FontSize = attrs[2];
                try
                {
                    int colorD = int.Parse(attrs[3]);
                    Color = string.Format("{0:X6}", colorD);
                }
                catch (FormatException e)
                {
                    Log(e.Message);
                }
                Timestamp = attrs[4];
                Content = content;
            }
            private string computeTime(double second)
            {
                int hour = (int)second / 3600;
                int minute = (int)(second - hour * 3600) / 60;
                second -= hour * 3600 + minute * 60;
                return hour.ToString() + string.Format(":{0:D2}:", minute) + string.Format("{0:00.00}", second);
            }
            public string Content { get; set; } = "";
            // 弹幕内容
            public string StartTime { get; set; } = "";
            // 出现时间
            public double Second { get; set; } = 0.00;
            // 出现时间（秒为单位）
            public string EndTime { get; set; } = "";
            // 消失时间
            public int DanmakuMode { get; set; } = POS_MOVE;
            // 弹幕类型
            public string FontSize { get; set; } = "";
            // 字号
            public string Color { get; set; } = "";
            // 颜色
            public string Timestamp { get; set; } = "";
            // 时间戳
        }

        public class DanmakuComparer : IComparer<DanmakuItem>
        {
            public int Compare(DanmakuItem? x, DanmakuItem? y)
            {
                if (x == null) return -1;
                if (y == null) return 1;
                return x.Second.CompareTo(y.Second);
            }
        }

        private const int POS_MOVE = 1;     //滚动弹幕
        private const int POS_TOP = 2;      //顶部弹幕
        private const int POS_BOTTOM = 3;   //底部弹幕
    }
}

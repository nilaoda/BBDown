using System.IO;
using System.Threading.Tasks;
using static BBDown.BBDownEntity;
using static BBDown.BBDownUtil;
using static BBDown.BBDownLogger;
using System.Text;
using System.Xml;
using System.Collections.Generic;
using System;

namespace BBDown
{
    class BBDownDanmaku
    {
        private const int MONITOR_WIDTH = 1920;
        private const int MONITOR_HEIGHT = 1080;
        private const int FONT_SIZE = 40;
        private const double MOVE_SPEND_TIME = 8.00;
        private const double TOP_SPEND_TIME = 4.00;
        private const int PROTECT_LENGTH = 50;

        private static List<Danmaku> danmakus = new List<Danmaku>();

        public static async Task DownloadDanmaku(Page p, string mp4Path)
        {
            string danmakuUrl = "https://comment.bilibili.com/" + p.cid + ".xml";
            string xmlPath = mp4Path.Substring(0, mp4Path.Length - 4) + ".xml";
            string assPath = mp4Path.Substring(0, mp4Path.Length - 4) + ".ass";

            if (File.Exists(xmlPath) && new FileInfo(xmlPath).Length != 0)
            {
                Log("弹幕文件已存在，跳过...");
            }

            Log($"开始下载P{p.index}弹幕");
            await DownloadFile(danmakuUrl, xmlPath, false, "");
            if (File.Exists(xmlPath) && new FileInfo(xmlPath).Length != 0)
            {
                await ParsingXml(xmlPath);
                await saveAsAss(assPath);
                Log($"P{p.index}弹幕下载完成");
            }
            else
            {
                Log($"P{p.index}弹幕下载失败...");
            }
        }

        public static Task ParsingXml(string xmlPath)
        {
            Log(xmlPath);
            if (!File.Exists(xmlPath) || new FileInfo(xmlPath).Length == 0)
            {
                return Task.CompletedTask;
            }
            XmlDocument xmlFile = new XmlDocument();
            XmlReaderSettings settings = new XmlReaderSettings();
            settings.IgnoreComments = true;//忽略文档里面的注释
            try
            {
                //xml文件路径
                XmlReader reader = XmlReader.Create(xmlPath, settings);
                xmlFile.Load(reader);
            }
            catch (Exception ex)
            {
                //MessageBox.Show("读取文件失败！");
                Log(ex);
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
                                Danmaku danmaku = new(vs, dElement.InnerText);
                                danmakus.Add(danmaku);
                            }
                        }
                    }
                }
            }

            return Task.CompletedTask;
        }

        public static Task saveAsAss(string outputPath)
        {
            string header = "[Script Info]" + Environment.NewLine +
           "Script Updated By: BBDown(https://github.com/nilaoda/BBDown)" + Environment.NewLine +
           "ScriptType: v4.00+" + Environment.NewLine +
           $"PlayResX: {MONITOR_WIDTH}" + Environment.NewLine +
           $"PlayResY: {MONITOR_HEIGHT}" + Environment.NewLine +
           $"Aspect Ratio: {MONITOR_WIDTH}:{MONITOR_HEIGHT}" + Environment.NewLine +
           "Collisions: Normal" + Environment.NewLine +
           "WrapStyle: 2" + Environment.NewLine +
           "ScaledBorderAndShadow: yes" + Environment.NewLine +
           "YCbCr Matrix: TV.601" + Environment.NewLine + Environment.NewLine +
           "[V4+ Styles]" + Environment.NewLine +
           "Format: Name, Fontname, Fontsize, PrimaryColour, SecondaryColour, OutlineColour, BackColour, Bold, Italic, Underline, StrikeOut, ScaleX, ScaleY, Spacing, Angle, BorderStyle, Outline, Shadow, Alignment, MarginL, MarginR, MarginV, Encoding" + Environment.NewLine +
           $"Style: BBDOWN_Style, 宋体, {FONT_SIZE}, &H00FFFFFF, &H00FFFFFF, &H00000000, &H00000000, 0, 0, 0, 0, 100, 100, 0.00, 0.00, 1, 2, 0, 7, 0, 0, 0, 0" + Environment.NewLine + Environment.NewLine +
           "[Events]" + Environment.NewLine +
           "Format: Layer, Start, End, Style, Name, MarginL, MarginR, MarginV, Effect, Text" + Environment.NewLine;
            PositionController controller = new PositionController();
            List<string> lines = new List<string>();
            danmakus.Sort(new DanmakuComparer());
            foreach (Danmaku danmaku in danmakus)
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
                string format = $"Dialogue: 2,{danmaku.StartTime},{danmaku.EndTime},BBDOWN_Style,,0000,0000,0000,,{{{effect}}}{danmaku.Content}";
                lines.Add(format);
            }

            try
            {
                using (FileStream fs = File.Create(outputPath))
                {
                    byte[] info = new UTF8Encoding(true).GetBytes(header);
                    fs.Write(info, 0, info.Length);
                    foreach (string line in lines)
                    {
                        info = new UTF8Encoding(true).GetBytes(line + Environment.NewLine);
                        fs.Write(info, 0, info.Length);
                    }
                }
            }
            catch (Exception e)
            {
                Log(e.Message);
            }
            return Task.CompletedTask;
        }

        protected class PositionController
        {
            int maxLine = MONITOR_HEIGHT * PROTECT_LENGTH / FONT_SIZE / 100;
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
                    {   // 此条弹幕已结束
                        vs[i] = time + displayTime;
                        return i * FONT_SIZE;
                    }
                }
                return -1;
            }
        }

        protected class Danmaku
        {
            public Danmaku(string[] attrs, string content)
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
            public double Second { get; set; } = 0.00;
            // 出现时间
            public string EndTime { get; set; } = "";
            // 消失时间
            public int DanmakuMode { get; set; } = POS_MOVE;
            // 1 滚动弹幕 2 顶端弹幕 3 底端弹幕 
            public string FontSize { get; set; } = "";
            // 字号
            public string Color { get; set; } = "";
            // 颜色
            public string Timestamp { get; set; } = "";
            // 时间戳
        }

        protected class DanmakuComparer : IComparer<Danmaku>
        {
            public int Compare(Danmaku? x, Danmaku? y)
            {
                if (x == null) return -1;
                if (y == null) return 1;
                return x.Second.CompareTo(y.Second);
            }
        }

        private const int POS_MOVE = 1;
        private const int POS_TOP = 2;
        private const int POS_BOTTOM = 3;
    }
}

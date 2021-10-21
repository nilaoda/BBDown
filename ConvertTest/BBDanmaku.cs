using System.Xml;
using System.IO;
using System;
using System.Collections;

namespace ConvertTest
{
    class DNode
    {
        private DateTime startTime;
        private DateTime endTime;
        private string text;

        public DNode(DateTime start, string content)
        {
            startTime = start;
            endTime = startTime.AddSeconds(5);
            text = content;
        }
        public override string ToString()
        {
            string lineStr = "Dialogue: 2," +
                    startTime.ToLongTimeString() + ".00," +
                    endTime.ToLongTimeString() + ".00," +
                    "style,,0000,0000,0000,," +
                    "{\\move(1920, 0, -240, 0)}" +
                    text;
            return lineStr;
        }

    }
    class BBDanmaku
    {
        // ASS字幕文件头
        private static string danmakuTemplate = "[Script Info]" + Environment.NewLine +
            "ScriptType: v4.00 " + Environment.NewLine +
            "PlayResX: 1920" + Environment.NewLine +
            "PlayResY: 1080" + Environment.NewLine +
            "Aspect Ratio: 1920:1080" + Environment.NewLine +
            "Collisions: Normal" + Environment.NewLine +
            "WrapStyle: 2" + Environment.NewLine +
            "ScaledBorderAndShadow: yes" + Environment.NewLine +
            "YCbCr Matrix: TV.601" + Environment.NewLine + Environment.NewLine +
            "[V4 + Styles]" + Environment.NewLine +
            "Format: Name, Fontname, Fontsize, PrimaryColour, SecondaryColour, OutlineColour, BackColour, Bold, Italic, Underline, StrikeOut, ScaleX, ScaleY, Spacing, Angle, BorderStyle, Outline, Shadow, Alignment, MarginL, MarginR, MarginV, Encoding" + Environment.NewLine +
            "Style: style, 宋体, 40, &H00FFFFFF, &H00FFFFFF, &H00000000, &H00000000, 0, 0, 0, 0, 100, 100, 0.00, 0.00, 1, 1, 0, 7, 0, 0, 0, 0" + Environment.NewLine + Environment.NewLine +
            "[Events]" + Environment.NewLine +
            "Format: Layer, Start, End, Style, Name, MarginL, MarginR, MarginV, Effect, Text" + Environment.NewLine;
        private static void Log(string str)
        {
            Console.WriteLine(str);
        }
        public static int Convert(string xmlPath, string assPath)
        {
            if (!File.Exists(xmlPath) || new FileInfo(xmlPath).Length == 0)
            {
                Log($"xml文件不存在");
                return -1;
            }
            XmlDocument Document = new XmlDocument();
            XmlReaderSettings settings = new XmlReaderSettings();
            settings.IgnoreComments = true;
            try
            {
                XmlReader reader = XmlReader.Create(xmlPath, settings);
                Document.Load(reader);
            }
            catch
            {
                Log("xml读取失败");
                return -1;
            }
            ArrayList allDanmaku = new ArrayList();
            XmlNode xmlRootNode = Document.SelectSingleNode("i");
            if (xmlRootNode != null)
            {
                string[] normalType = { "1", "4", "5", "6" };
                XmlNodeList xmlNodeList = xmlRootNode.ChildNodes;
                foreach (XmlElement lineNode in xmlNodeList)
                {
                    if (lineNode.Name == "d")
                    {
                        var attr = lineNode.GetAttribute("p").Split(',');
                        float start = float.Parse(attr[0]);
                        string text = lineNode.InnerText;
                        DateTime startTime = new DateTime(0).AddSeconds((int)start);
                        DNode dNode = new DNode(startTime, text);
                        allDanmaku.Add(dNode);
                    }
                }
            }
            FileStream assFile = File.Create(assPath);
            StreamWriter streamWriter = new StreamWriter(assFile);
            streamWriter.Write(danmakuTemplate);
            foreach (DNode i in allDanmaku)
            {
                streamWriter.WriteLine(i.ToString());
            }
            streamWriter.Close();
            assFile.Close();
            return 0;
        }
    }
}
using System.Xml;
using System.IO;
using System;
using System.Collections;
using static BBDown.BBDownLogger;
using System.Collections.Generic;

namespace BBDown
{
    class DNode
    {
        private DateTime startTime;
        private DateTime endTime;
        private string text;
        private string type;
        private string color;
        public DNode() { }

        public DNode(DateTime newStart, string newType, string newColor, string newText)
        {
            startTime = newStart;
            endTime = startTime.AddSeconds(5);
            text = newText;
            type = newType;
            color = newColor;
        }
        public DateTime getStartTime()
        {
            return startTime;
        }
        public override string ToString()
        {
            string style = "";
            switch (type)
            {
                case "5":
                    style += "pos{\\1920}";
                    break;
            }
            string lineStr = $"Dialogue: 2,{startTime.ToLongTimeString()}.00, {endTime.ToLongTimeString()}.00," +
                $"BBDownStyle,,0000,0000,0000,,{{\\move(1920,0,-240,0)}}{text}";
            return lineStr;
        }


    }
    class BBDanmaku
    {
        // ASS字幕文件头
        private static string ASSFileHeader = "[Script Info]" + Environment.NewLine +
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
            "Style: BBDownStyle, 宋体, 40, &H00FFFFFF, &H00FFFFFF, &H00000000, &H00000000, 0, 0, 0, 0, 100, 100, 0.00, 0.00, 1, 1, 0, 7, 0, 0, 0, 0" + Environment.NewLine + Environment.NewLine +
            "[Events]" + Environment.NewLine +
            "Format: Layer, Start, End, Style, Name, MarginL, MarginR, MarginV, Effect, Text" + Environment.NewLine;
        public static bool ConvertDanmaku(string xmlPath, string assPath)
        {
            List<DNode> allDanmaku = new List<DNode>();

            XmlDocument Document = new XmlDocument();
            XmlReaderSettings settings = new XmlReaderSettings();
            settings.IgnoreComments = true;
            try
            {
                XmlReader reader = XmlReader.Create(xmlPath, settings);
                Document.Load(reader);
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
                            int start = (int)float.Parse(attr[0]);
                            int type = int.Parse(attr[1]);
                            int fontsize = int.Parse(attr[2]);
                            int color = int.Parse(attr[3]);
                            string text = lineNode.InnerText;
                            DateTime startTime = new DateTime(0).AddSeconds(start);
                            DNode dNode = new DNode();
                            allDanmaku.Add(dNode);
                        }
                    }
                }
                reader.Close();
            }
            catch
            {
                return false;
            }

            allDanmaku.Sort(delegate (DNode x, DNode y)
            {
                return x.getStartTime().CompareTo(y.getStartTime());
            });
            FileStream assFile = File.Create(assPath);
            StreamWriter streamWriter = new StreamWriter(assFile);
            streamWriter.Write(ASSFileHeader);
            foreach (DNode i in allDanmaku)
            {
                streamWriter.WriteLine(i.ToString());
            }
            streamWriter.Close();
            assFile.Close();
            return true;
        }
    }
}
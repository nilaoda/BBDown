using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using static BBDown.BBDownEntity;
using static BBDown.BBDownUtil;
using static BBDown.BBDownSubUtil;
using static BBDown.BBDownLogger;
using System.IO;

namespace BBDown
{
    class BBDownMuxer
    {
        public static string FFMPEG = "ffmpeg";
        public static string MP4BOX = "mp4box";

        public static int RunExe(string app, string parms, bool customBin = false)
        {
            // 若不是手动指定，则自动寻找可执行文件
            if (!customBin)
            {
                if (File.Exists(Path.Combine(Program.APP_DIR, $"{app}")))
                    app = Path.Combine(Program.APP_DIR, $"{app}");
                if (File.Exists(Path.Combine(Program.APP_DIR, $"{app}.exe")))
                    app = Path.Combine(Program.APP_DIR, $"{app}.exe");
            }
            int code = 0;
            Process p = new Process();
            p.StartInfo.FileName = app;
            p.StartInfo.Arguments = parms;
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.RedirectStandardError = true;
            p.StartInfo.CreateNoWindow = false;
            p.ErrorDataReceived += delegate (object sendProcess, DataReceivedEventArgs output) {
                if (!string.IsNullOrWhiteSpace(output.Data))
                    Log(output.Data);
            };
            p.StartInfo.StandardErrorEncoding = Encoding.UTF8;
            p.Start();
            p.BeginErrorReadLine();
            p.WaitForExit();
            p.Close();
            p.Dispose();
            return code;
        }

        private static string EscapeString(string str)
        {
            return str.Replace("\"", "'");
        }

        public static int MuxByMp4box(string videoPath, string audioPath, string outPath, string desc, string title, string episodeId, string pic, string lang, List<Subtitle> subs, bool audioOnly, bool videoOnly)
        {
            StringBuilder inputArg = new StringBuilder();
            StringBuilder metaArg = new StringBuilder();
            inputArg.Append(" -inter 500 -noprog ");
            if (!string.IsNullOrEmpty(videoPath))
                inputArg.Append($" -add \"{videoPath}#trackID=1:name=\" ");
            if (!string.IsNullOrEmpty(audioPath))
                inputArg.Append($" -add \"{audioPath}:lang={(lang == "" ? "und" : lang)}\" ");
            
            if (!string.IsNullOrEmpty(pic))
                metaArg.Append($":cover=\"{pic}\"");
            if (!string.IsNullOrEmpty(episodeId))
                metaArg.Append($":album=\"{title}\":name=\"{episodeId}\"");
            else
                metaArg.Append($":name=\"{title}\"");
            metaArg.Append($":comment=\"{desc}\"");

            if (subs != null)
            {
                for (int i = 0; i < subs.Count; i++)
                {
                    if (File.Exists(subs[i].path) && File.ReadAllText(subs[i].path) != "")
                    {
                        inputArg.Append($" -add \"{subs[i].path}#trackID=1:name={GetSubtitleCode(subs[i].lan).Item2}:lang={GetSubtitleCode(subs[i].lan).Item1}\" ");
                    }
                }
            }

            //----分析完毕
            var arguments = inputArg.ToString() + (metaArg.ToString() == "" ? "" : " -itags tools=\"\"" + metaArg.ToString()) + $" \"{outPath}\"";
            LogDebug("mp4box命令：{0}", arguments);
            return RunExe(MP4BOX, arguments, MP4BOX != "mp4box");
        }

        public static int MuxAV(bool useMp4box, string videoPath, string audioPath, string outPath, string desc = "", string title = "", string episodeId = "", string pic = "", string lang = "", List<Subtitle> subs = null, bool audioOnly = false, bool videoOnly = false)
        {
            desc = EscapeString(desc);
            title = EscapeString(title);
            episodeId = EscapeString(episodeId);

            if (useMp4box)
            {
                return MuxByMp4box(videoPath, audioPath, outPath, desc, title, episodeId, pic, lang, subs, audioOnly, videoOnly);
            }

            if (outPath.Contains("/") && ! Directory.Exists(Path.GetDirectoryName(outPath)))
                Directory.CreateDirectory(Path.GetDirectoryName(outPath));
            //----分析并生成-i参数
            StringBuilder inputArg = new StringBuilder();
            StringBuilder metaArg = new StringBuilder();
            if (!string.IsNullOrEmpty(videoPath))
                inputArg.Append($" -i \"{videoPath}\" ");
            if (!string.IsNullOrEmpty(audioPath))
                inputArg.Append($" -i \"{audioPath}\" ");
            if (!string.IsNullOrEmpty(pic))
                inputArg.Append($" -i \"{pic}\" ");
            if (subs != null)
            {
                for (int i = 0; i < subs.Count; i++)
                {
                    if(File.Exists(subs[i].path) && File.ReadAllText(subs[i].path) != "")
                    {
                        inputArg.Append($" -i \"{subs[i].path}\" ");
                        metaArg.Append($" -metadata:s:s:{i} handler_name=\"{GetSubtitleCode(subs[i].lan).Item2}\" -metadata:s:s:{i} language={GetSubtitleCode(subs[i].lan).Item1} ");
                    }
                }
            }
            if (!string.IsNullOrEmpty(pic))
                metaArg.Append(" -disposition:v:1 attached_pic ");
            var inputCount = Regex.Matches(inputArg.ToString(), "-i \"").Count;
            for (int i = 0; i < inputCount; i++)
            {
                inputArg.Append($" -map {i} ");
            }

            //----分析完毕
            var arguments = $"-loglevel warning -y " +
                 inputArg.ToString() + metaArg.ToString() + $" -metadata title=\"" + (episodeId == "" ? title : episodeId) + "\" " +
                 (lang == "" ? "" : $"-metadata:s:a:0 language={lang} ") +
                 $"-metadata description=\"{desc}\" " +
                 (episodeId == "" ? "" : $"-metadata album=\"{title}\" ") +
                 (audioOnly ? " -vn " : "") + (videoOnly ? " -an " : "") +
                 $"-c copy " +
                 (subs != null ? " -c:s mov_text " : "") +
                 "-movflags faststart " +
                 $"\"{outPath}\"";
            LogDebug("ffmpeg命令：{0}", arguments);
            return RunExe(FFMPEG, arguments, FFMPEG != "ffmpeg");
        }

        public static void MergeFLV(string[] files, string outPath)
        {
            if (files.Length == 1)
            {
                File.Move(files[0], outPath); 
            }
            else
            {
                foreach (var file in files)
                {
                    var tmpFile = Path.Combine(Path.GetDirectoryName(file), Path.GetFileNameWithoutExtension(file) + ".ts");
                    var arguments = $"-loglevel warning -y -i \"{file}\" -map 0 -c copy -f mpegts -bsf:v h264_mp4toannexb \"{tmpFile}\"";
                    LogDebug("ffmpeg命令：{0}", arguments);
                    RunExe("ffmpeg", arguments);
                    File.Delete(file);
                }
                var f = GetFiles(Path.GetDirectoryName(files[0]), ".ts");
                CombineMultipleFilesIntoSingleFile(f, outPath);
                foreach (var s in f) File.Delete(s);
            }
        }
    }
}

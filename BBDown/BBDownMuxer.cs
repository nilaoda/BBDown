using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using static BBDown.Core.Entity.Entity;
using static BBDown.BBDownUtil;
using static BBDown.Core.Util.SubUtil;
using static BBDown.Core.Logger;
using System.IO;
using BBDown.Core;

namespace BBDown
{
    partial class BBDownMuxer
    {
        public static string FFMPEG = "ffmpeg";
        public static string MP4BOX = "mp4box";

        private static int RunExe(string app, string parms, bool customBin = false)
        {
            int code = 0;
            Process p = new();
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
            return string.IsNullOrEmpty(str) ? str : str.Replace("\"", "'").Replace("\\", "\\\\");
        }

        private static int MuxByMp4box(string videoPath, string audioPath, string outPath, string desc, string title, string author, string episodeId, string pic, string lang, List<Subtitle>? subs, bool audioOnly, bool videoOnly, List<ViewPoint>? points)
        {
            StringBuilder inputArg = new();
            StringBuilder metaArg = new();
            int nowId = 0;
            inputArg.Append(" -inter 500 -noprog ");
            if (!string.IsNullOrEmpty(videoPath))
            {
                inputArg.Append($" -add \"{videoPath}#trackID={(audioOnly && audioPath == "" ? "2" : "1")}:name=\" ");
                nowId++;
            }
            if (!string.IsNullOrEmpty(audioPath))
            {
                inputArg.Append($" -add \"{audioPath}:lang={(lang == "" ? "und" : lang)}\" ");
                nowId++;
            }
            if (points != null && points.Any())
            {
                var meta = GetMp4boxMetaString(points);
                var metaFile = Path.Combine(Path.GetDirectoryName(string.IsNullOrEmpty(videoPath) ? audioPath : videoPath)!, "chapters");
                File.WriteAllText(metaFile, meta);
                inputArg.Append($" -chap  \"{metaFile}\"  ");
            }
            if (!string.IsNullOrEmpty(pic))
                metaArg.Append($":cover=\"{pic}\"");
            if (!string.IsNullOrEmpty(episodeId))
                metaArg.Append($":album=\"{title}\":title=\"{episodeId}\"");
            else
                metaArg.Append($":title=\"{title}\"");
            metaArg.Append($":comment=\"{desc}\"");
            metaArg.Append($":artist=\"{author}\"");

            if (subs != null)
            {
                for (int i = 0; i < subs.Count; i++)
                {
                    if (File.Exists(subs[i].path) && File.ReadAllText(subs[i].path!) != "")
                    {
                        nowId++;
                        inputArg.Append($" -add \"{subs[i].path}#trackID=1:name=:hdlr=sbtl:lang={GetSubtitleCode(subs[i].lan).Item1}\" ");
                        inputArg.Append($" -udta {nowId}:type=name:str=\"{GetSubtitleCode(subs[i].lan).Item2}\" ");
                    }
                }
            }

            //----分析完毕
            var arguments = (Config.DEBUG_LOG ? " -verbose " : "") + inputArg.ToString() + (metaArg.ToString() == "" ? "" : " -itags tool=" + metaArg.ToString()) + $" -new -- \"{outPath}\"";
            LogDebug("mp4box命令: {0}", arguments);
            return RunExe(MP4BOX, arguments, MP4BOX != "mp4box");
        }

        public static int MuxAV(bool useMp4box, string videoPath, string audioPath, List<AudioMaterial> audioMaterial, string outPath, 
            string desc = "", string title = "", string author = "", string episodeId = "", string pic = "", string lang = "", List<Subtitle>? subs = null, 
            bool audioOnly = false, bool videoOnly = false, List<ViewPoint>? points = null, 
            long pubTime = 0, bool simplyMux = false, MyOption myOption = null,Page p = null,int pagesCount = 0)
        {
            if (audioOnly && audioPath != "")
                videoPath = "";
            if (videoOnly)
                audioPath = "";
            desc = EscapeString(desc);
            title = EscapeString(title);
            episodeId = EscapeString(episodeId);

            if (useMp4box)
            {
                return MuxByMp4box(videoPath, audioPath, outPath, desc, title, author, episodeId, pic, lang, subs, audioOnly, videoOnly, points);
            }

            if (outPath.Contains('/') && ! Directory.Exists(Path.GetDirectoryName(outPath)))
                Directory.CreateDirectory(Path.GetDirectoryName(outPath)!);
            //----分析并生成-i参数
            StringBuilder inputArg = new();
            StringBuilder metaArg = new();
            byte inputCount = 0;
            foreach (string path in new string[] { videoPath, audioPath })
            {
                if (!string.IsNullOrEmpty(path))
                {
                    inputCount++;
                    inputArg.Append($"-i \"{path}\" ");
                }
            }

            if (audioMaterial.Any())
            {
                byte audioCount = 0;
                metaArg.Append("-metadata:s:a:0 title=\"原音频\" ");
                foreach (var audio in audioMaterial)
                {
                    inputCount++;
                    audioCount++;
                    inputArg.Append($"-i \"{audio.path}\" ");
                    if (!string.IsNullOrWhiteSpace(audio.title)) metaArg.Append($"-metadata:s:a:{audioCount} title=\"{audio.title}\" ");
                    if (!string.IsNullOrWhiteSpace(audio.personName)) metaArg.Append($"-metadata:s:a:{audioCount} artist=\"{audio.personName}\" ");
                }
            }

            if (!string.IsNullOrEmpty(pic))
            {
                inputCount++;
                inputArg.Append($"-i \"{pic}\" ");
            }

            if (subs != null)
            {
                for (int i = 0; i < subs.Count; i++)
                {
                    if(File.Exists(subs[i].path) && File.ReadAllText(subs[i].path!) != "")
                    {
                        inputCount++;
                        inputArg.Append($"-i \"{subs[i].path}\" ");
                        metaArg.Append($"-metadata:s:s:{i} title=\"{GetSubtitleCode(subs[i].lan).Item2}\" -metadata:s:s:{i} language={GetSubtitleCode(subs[i].lan).Item1} ");
                    }
                }
            }

            if (!string.IsNullOrEmpty(pic))
                metaArg.Append($"-disposition:v:{(audioOnly ? "0" : "1")} attached_pic ");
            // var inputCount = InputRegex().Matches(inputArg.ToString()).Count;

            if (points != null && points.Any())
            {
                var meta = GetFFmpegMetaString(points);
                var metaFile = Path.Combine(Path.GetDirectoryName(string.IsNullOrEmpty(videoPath) ? audioPath : videoPath)!, "chapters");
                File.WriteAllText(metaFile, meta);
                inputArg.Append($"-i \"{metaFile}\" -map_chapters {inputCount} ");
            }

            inputArg.Append(string.Concat(Enumerable.Range(0, inputCount).Select(i => $"-map {i} ")));

            //----分析完毕
            StringBuilder argsBuilder = new StringBuilder();
            argsBuilder.Append($"-loglevel {(Config.DEBUG_LOG ? "verbose" : "warning")} -y ");
            argsBuilder.Append(inputArg);
            argsBuilder.Append(metaArg);
            if (!simplyMux) {
                argsBuilder.Append($"-metadata title=\"{(episodeId == "" ? title : episodeId)}\" ");
                if (lang != "") argsBuilder.Append($"-metadata:s:a:0 language={lang} ");
                if (!string.IsNullOrWhiteSpace(desc)) argsBuilder.Append($"-metadata description=\"{desc}\" ");
                if (!string.IsNullOrEmpty(author)) argsBuilder.Append($"-metadata artist=\"{author}\" ");
                if (episodeId != "") argsBuilder.Append($"-metadata album=\"{title}\" ");
                if (pubTime != 0) argsBuilder.Append($"-metadata creation_time=\"{(DateTimeOffset.FromUnixTimeSeconds(pubTime).ToString("yyyy-MM-ddTHH:mm:ss.ffffffZ"))}\" ");
            }
            if (myOption.FfmpegMetadata != "") {
                Regex regex = new Regex(@"-metadata (\w+)=(.*?)(?=-metadata|$)", RegexOptions.Singleline);
                MatchCollection matches = regex.Matches(argsBuilder.ToString());
                string metadata = myOption.FfmpegMetadata;
                foreach (Match match in matches)
                {
                    if (match.Success)
                    {
                        string key = match.Groups[1].Value;
                        string value = FormatValue(GetValueForReplacement(key, metadata),title,p,pagesCount);
                        if (!string.IsNullOrEmpty(value))
                        {
                            argsBuilder.Replace($"{key}={match.Groups[2].Value}", $"{key}=\"{value}\" ");
                            metadata = metadata.Replace($"{key}={value}","");
                        }
                    }
                }
                string[] keyValuePairs = metadata.Split(',');
                foreach (var keyValuePair in keyValuePairs)
                {
                    string[] parts = keyValuePair.Split('=');
                    if (parts.Length == 2) 
                    {
                        string key = parts[0];
                        string value = FormatValue(parts[1], title, p, pagesCount);
                        argsBuilder.Append($"-metadata {key}=\"{value}\" ");
                    }
                }
            }
            argsBuilder.Append("-c copy ");
            if (audioOnly && audioPath == "") argsBuilder.Append("-vn ");
            if (subs != null) argsBuilder.Append("-c:s mov_text ");
            argsBuilder.Append($"-movflags faststart -strict unofficial -strict -2 -f mp4 -- \"{outPath}\"");

            string arguments = argsBuilder.ToString();

            LogDebug("ffmpeg命令: {0}", arguments);
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
                    var tmpFile = Path.Combine(Path.GetDirectoryName(file)!, Path.GetFileNameWithoutExtension(file) + ".ts");
                    var arguments = $"-loglevel warning -y -i \"{file}\" -map 0 -c copy -f mpegts -bsf:v h264_mp4toannexb \"{tmpFile}\"";
                    LogDebug("ffmpeg命令: {0}", arguments);
                    RunExe("ffmpeg", arguments);
                    File.Delete(file);
                }
                var f = GetFiles(Path.GetDirectoryName(files[0])!, ".ts");
                CombineMultipleFilesIntoSingleFile(f, outPath);
                foreach (var s in f) File.Delete(s);
            }
        }

        private static string GetValueForReplacement(string key, string input)
        {
            Regex regex = new Regex($"{key}=([^,]+)");
            Match match = regex.Match(input);

            if (match.Success)
            {
                return match.Groups[1].Value;
            }
            else
            {
                return string.Empty;
            }
        }

        private static string FormatValue(string value,string title,Page p,int pagesCount) {
            var regex = Program.InfoRegex();
            foreach (Match m in regex.Matches(value).Cast<Match>()) 
            {
                var key = m.Groups[1].Value;
                var v = key switch 
                {
                    "videoTitle" => BBDownUtil.GetValidFileName(title, filterSlash: true).Trim().TrimEnd('.').Trim(),
                    "pageNumber" => p.index.ToString(),
                    "pageNumberWithZero" => p.index.ToString().PadLeft(pagesCount.ToString().Length, '0'),
                    "pageTitle" => BBDownUtil.GetValidFileName(p.title, filterSlash: true).Trim().TrimEnd('.').Trim(),
                    "bvid" => p.bvid,
                    "aid" => p.aid,
                    "cid" => p.cid,
                    "ownerName" => p.ownerName == null ? "" : BBDownUtil.GetValidFileName(p.ownerName, filterSlash: true).Trim().TrimEnd('.').Trim(),
                    "ownerMid" => p.ownerMid ?? "",
                    _ => $"<{key}>"
                };
                value = value.Replace(m.Value, v);
            }
            return value;
        }
    }
}

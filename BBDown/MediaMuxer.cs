using BBDown.Core;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;

using static BBDown.Core.Logger;
using static BBDown.Core.Entity.Entity;
using static BBDown.Core.Util.SubUtil;
using System.Linq;

namespace BBDown.Mux
{
    public interface IMuxer
    {
        Task<MediaMuxResult> Mux(MediaMuxInfo info);
    }

    public abstract class ExternalMuxer : IMuxer
    {
        protected string binPath;

        public ExternalMuxer(string binPath)
        {
            this.binPath = binPath;
        }

        public abstract Task<MediaMuxResult> Mux(MediaMuxInfo info);

        protected static string EscapeStringWithQuote(string str)
        {
            if (OperatingSystem.IsWindows())
                return "\"" + str.Replace("\"", "\"\"") + "\"";
            else if (OperatingSystem.IsLinux())
                return "\"" + str.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";
            throw new NotSupportedException($"Unknown platform {Environment.OSVersion.Platform}");
        }

        protected Process CreateProcess(string argument)
        {
            var psi = new ProcessStartInfo()
            {
                FileName = binPath,
                Arguments = argument,
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = Environment.CurrentDirectory,
                RedirectStandardError = true,
                StandardErrorEncoding = Encoding.UTF8,
            };
            var process = new Process()
            {
                StartInfo = psi,
            };
            return process;
        }
    }

    public class MP4BoxMuxer : ExternalMuxer
    {
        public MP4BoxMuxer(string binPath) : base(binPath)
        {
        }

        public override async Task<MediaMuxResult> Mux(MediaMuxInfo info)
        {
            // MP4Box没用过
            StringBuilder inputArg = new();
            StringBuilder metaArg = new();
            List<string> cacheFiles = new();
            int nowId = 0;
            inputArg.Append(" -inter 500 -noprog ");

            if (!string.IsNullOrWhiteSpace(info.videoPath))
            {
                inputArg.Append($" -add \"{info.videoPath}:name=\" ");
                nowId++;
            }

            if (!string.IsNullOrWhiteSpace(info.audioPath))
            {
                inputArg.Append($" -add \"{info.audioPath}:lang={(string.IsNullOrEmpty(info.language) ? "und" : info.language)}\" ");
                nowId++;
            }

            if (info.viewPoints != null)
            {
                var qwq = new[] { info.videoPath, info.audioPath, info.coverPath, info.outputPath }.First(s => !string.IsNullOrWhiteSpace(s));
                var cachePath = Path.Combine(Path.GetDirectoryName(qwq) ?? "", "ffmpeg_viewpoint_cache.txt");
                File.Delete(cachePath);
                using var fs = File.Create(cachePath);
                using var writer = new StreamWriter(fs, Encoding.UTF8);

                foreach (var point in info.viewPoints)
                {
                    await writer.WriteLineAsync($"{BBDownUtil.FormatTime(point.start, true)} {point.title}");
                }
                inputArg.Append($" -chap  \"{cachePath}\"  ");
                cacheFiles.Add(cachePath);
            }

            if (!string.IsNullOrWhiteSpace(info.coverPath))
                metaArg.Append($":cover=\"{info.coverPath}\"");

            if (!string.IsNullOrWhiteSpace(info.title))
            {
                if (!string.IsNullOrWhiteSpace(info.episodeId))
                    metaArg.Append($":album=\"{info.title}\":title=\"{info.episodeId}\"");
                else
                    metaArg.Append($":title=\"{info.title}\"");
            }

            if (!string.IsNullOrWhiteSpace(info.description))
                metaArg.Append($":comment=\"{info.description}\"");
            if (!string.IsNullOrWhiteSpace(info.author))
                metaArg.Append($":artist=\"{info.author}\"");

            if (info.subtitles != null)
            {
                for (int i = 0; i < info.subtitles.Count; i++)
                {
                    if (info.subtitles[i].cachePath == null)
                        continue;
                    if (File.Exists(info.subtitles[i].cachePath) && new FileInfo(info.subtitles[i].cachePath!).Length == 0)
                    {
                        nowId++;
                        inputArg.Append($" -add \"{info.subtitles[i].cachePath}#trackID=1:name=:hdlr=sbtl:lang={GetSubtitleCode(info.subtitles[i].lan).lan}\" ");
                        inputArg.Append($" -udta {nowId}:type=name:str=\"{GetSubtitleCode(info.subtitles[i].lan).display}\" ");
                    }
                }
            }

            //----分析完毕
            var argument = (Config.DEBUG_LOG ? " -verbose " : "") + inputArg.ToString() + (metaArg.ToString() == "" ? "" : " -itags tool=" + metaArg.ToString()) + $" -new -- \"{info.outputPath}\"";
            LogDebug("mp4box命令: {0}", argument);
            var process = CreateProcess(argument);
            process.ErrorDataReceived += delegate (object sendProcess, DataReceivedEventArgs output) {
                if (!string.IsNullOrWhiteSpace(output.Data))
                    Log(output.Data);
            };
            process.Start();
            process.BeginErrorReadLine();
            await process.WaitForExitAsync();
            process.Close();
            process.Dispose();

            if (!File.Exists(info.outputPath))
                return MediaMuxResult.Failed("未知的错误");

            foreach (var cache in cacheFiles)
                File.Delete(cache);
            return MediaMuxResult.Succeed(info.outputPath);
        }
    }

    public class FFMPEGMuxer : ExternalMuxer
    {
        public FFMPEGMuxer(string binPath) : base(binPath)
        {
        }

        public async override Task<MediaMuxResult> Mux(MediaMuxInfo info)
        {
            var inputBuilder = new StringBuilder();
            var metaBuilder = new StringBuilder();
            var streamCount = 0;
            var cacheFiles = new List<string>();

            if (!string.IsNullOrWhiteSpace(info.videoPath))
            {
                inputBuilder.Append($"-i \"{info.videoPath}\" ");
                metaBuilder.Append($"-metadata:s:{streamCount} title=原视频 ");
                streamCount++;
            }

            if (!string.IsNullOrWhiteSpace(info.audioPath))
            {
                inputBuilder.Append($"-i \"{info.audioPath}\" ");
                metaBuilder.Append($"-metadata:s:{streamCount} title=原音频 ");
                streamCount++;
            }

            if (!string.IsNullOrWhiteSpace(info.coverPath))
            {
                inputBuilder.Append($"-i \"{info.coverPath}\" ");
                metaBuilder.Append($"-c:{streamCount} png ");
                metaBuilder.Append($"-disposition:{streamCount} attached_pic ");
                if (Path.GetExtension(info.outputPath) == ".mp3")
                {
                    metaBuilder.Append("-id3v2_version 3 ");
                    metaBuilder.Append($"-metadata:s:{streamCount} title=\"Album cover\" ");
                    metaBuilder.Append($"-metadata:s:v comment=\"Cover (Front)\" ");
                }
                streamCount++;
            }

            if (info.audioMaterials != null)
            {
                foreach (var audioMaterial in info.audioMaterials)
                {
                    inputBuilder.Append($"-i \"{audioMaterial.path}\" ");
                    metaBuilder.Append($"-metadata:s:{streamCount} title={EscapeStringWithQuote(audioMaterial.title)} ");
                    metaBuilder.Append($"-metadata:s:{streamCount} author={EscapeStringWithQuote(audioMaterial.personName)} ");
                    streamCount++;
                }
            }

            if (info.subtitles != null)
            {
                foreach (var subtitle in info.subtitles)
                {
                    if (subtitle.cachePath == null)
                        continue;
                    inputBuilder.Append($"-i \"{subtitle.cachePath}\" ");
                    (var lan, var display) = GetSubtitleCode(subtitle.lan);
                    metaBuilder.Append($"-metadata:s:{streamCount} title={EscapeStringWithQuote(display)} ");
                    metaBuilder.Append($"-metadata:s:{streamCount} language={lan} ");
                    streamCount++;
                }
            }

            if (info.viewPoints != null && info.viewPoints.Any())
            {
                var qwq = new[] { info.videoPath, info.audioPath, info.coverPath, info.outputPath }.First(s => !string.IsNullOrWhiteSpace(s));
                var cachePath = Path.Combine(Path.GetDirectoryName(qwq) ?? "", "ffmpeg_viewpoint_cache.txt");
                File.Delete(cachePath);
                using var fs = File.Create(cachePath);
                using var writer = new StreamWriter(fs, Encoding.UTF8);

                await writer.WriteLineAsync(";FFMETADATA");
                foreach (var point in info.viewPoints)
                {
                    var time = 1000; //固定 1000
                    writer.WriteLine("[CHAPTER]");
                    writer.WriteLine($"TIMEBASE=1/{time}");
                    writer.WriteLine($"START={point.start * time}");
                    writer.WriteLine($"END={point.end * time}");
                    writer.WriteLine($"title={point.title}");
                    writer.WriteLine();
                }

                inputBuilder.Append($"-i \"{cachePath}\" -map_chapters {streamCount} ");
                streamCount++;
            }

            inputBuilder.Append(string.Concat(Enumerable.Range(0, streamCount).Select(i => $"-map {i} ")));

            if (!string.IsNullOrWhiteSpace(info.title) || !string.IsNullOrWhiteSpace(info.episodeId))
                metaBuilder.Append($"-metadata title={EscapeStringWithQuote(info.episodeId ?? info.title!)} ");
            if (!string.IsNullOrWhiteSpace(info.language))
                metaBuilder.Append($"-metadata language={EscapeStringWithQuote(info.language)} ");
            if (!string.IsNullOrWhiteSpace(info.description))
                metaBuilder.Append($"-metadata description={EscapeStringWithQuote(info.description)} ");
            if (!string.IsNullOrWhiteSpace(info.author))
                metaBuilder.Append($"-metadata artist={EscapeStringWithQuote(info.author)} ");
            if (!string.IsNullOrWhiteSpace(info.episodeId))
                metaBuilder.Append($"-metadata album={EscapeStringWithQuote(info.episodeId)} ");
            if (info.pubTime != null)
                metaBuilder.Append($"-metadata creation_time=\"{info.pubTime.Value.ToString("yyyy-MM-ddTHH:mm:ss.ffffffZ")}\" ");
            if (info.subtitles != null)
                metaBuilder.Append("-c:s mov_text ");

            var argsBuilder = new StringBuilder();
            argsBuilder.Append(inputBuilder);
            argsBuilder.Append(metaBuilder);

            argsBuilder.Append($"-loglevel {(Config.DEBUG_LOG ? "verbose" : "warning")} -y ");
            if (Path.GetExtension(info.outputPath) == ".mp4")
                argsBuilder.Append("-c copy ");
            argsBuilder.Append($"-movflags faststart -strict unofficial -strict -2 -- \"{info.outputPath}\"");

            var argument = argsBuilder.ToString();
            LogDebug("ffmpeg命令: " + argument);
            var process = CreateProcess(argument);
            process.ErrorDataReceived += delegate (object sendProcess, DataReceivedEventArgs output) {
                if (!string.IsNullOrWhiteSpace(output.Data))
                    Log(output.Data);
            };
            process.Start();
            process.BeginErrorReadLine();
            await process.WaitForExitAsync();
            process.Close();
            process.Dispose();

            if (!File.Exists(info.outputPath))
                return MediaMuxResult.Failed("未知的错误");

            foreach (var cache in cacheFiles)
                File.Delete(cache);
            return MediaMuxResult.Succeed(info.outputPath);
        }
    }

    public class MediaMuxInfo
    {
        public string outputPath;
        public string? videoPath;
        public string? audioPath;
        public string? coverPath;
        public List<AudioMaterial>? audioMaterials;
        public List<ViewPoint>? viewPoints;
        public List<Subtitle>? subtitles;
        public string? title;
        public string? description;
        public string? author;
        public string? episodeId;
        public string? language;
        public IProgress<double>? progress;
        public DateTime? pubTime;

        public MediaMuxInfo(string outputPath)
        {
            this.outputPath = outputPath;
        }
    }

    public class MediaMuxResult
    {
        public readonly string? outputPath;
        public readonly string? message;

        public bool IsSucceed => outputPath != null;

        private MediaMuxResult(string? outputPath, string? message)
        {
            this.outputPath = outputPath;
            this.message = message;
        }

        public static MediaMuxResult Succeed(string outputPath) => new(outputPath, null);
        public static MediaMuxResult Failed(string message) => new(null, message);
    }
}

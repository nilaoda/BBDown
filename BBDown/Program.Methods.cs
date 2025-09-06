using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using static BBDown.Core.Entity.Entity;
using static BBDown.BBDownUtil;
using static BBDown.Core.Logger;
using System.Linq;
using System.Text.RegularExpressions;
using BBDown.Core;
using BBDown.Core.Entity;
using static BBDown.BBDownDownloadUtil;

namespace BBDown;

internal partial class Program
{

    /// <summary>
    /// 兼容旧版本命令行参数并给出警告
    /// </summary>
    /// <param name="myOption"></param>
    private static void HandleDeprecatedOptions(MyOption myOption)
    {
        if (myOption.AddDfnSubfix)
        {
            LogWarn("--add-dfn-subfix 已被弃用, 建议使用 --file-pattern/-F 或 --multi-file-pattern/-M 来自定义输出文件名格式");
            if (string.IsNullOrEmpty(myOption.FilePattern) && string.IsNullOrEmpty(myOption.MultiFilePattern))
            {
                SinglePageDefaultSavePath += "[<dfn>]";
                MultiPageDefaultSavePath += "[<dfn>]";
                LogWarn($"已切换至 -F \"{SinglePageDefaultSavePath}\" -M \"{MultiPageDefaultSavePath}\"");
            }
        }
        if (myOption.Aria2cProxy != "")
        {
            LogWarn("--aria2c-proxy 已被弃用, 请使用 --aria2c-args 来设置aria2c代理, 本次执行已添加该代理");
            myOption.Aria2cArgs += $" --all-proxy=\"{myOption.Aria2cProxy}\"";
        }
        if (myOption.OnlyHevc)
        {
            LogWarn("--only-hevc/-hevc 已被弃用, 请使用 --encoding-priority 来设置编码优先级, 本次执行已将hevc设置为最高优先级");
            myOption.EncodingPriority = "hevc";
        }
        if (myOption.OnlyAvc)
        {
            LogWarn("--only-avc/-avc 已被弃用, 请使用 --encoding-priority 来设置编码优先级, 本次执行已将avc设置为最高优先级");
            myOption.EncodingPriority = "avc";
        }
        if (myOption.OnlyAv1)
        {
            LogWarn("--only-av1/-av1 已被弃用, 请使用 --encoding-priority 来设置编码优先级, 本次执行已将av1设置为最高优先级");
            myOption.EncodingPriority = "av1";
        }
        if (myOption.NoPaddingPageNum)
        {
            LogWarn("--no-padding-page-num 已被弃用, 建议使用 --file-pattern/-F 或 --multi-file-pattern/-M 来自定义输出文件名格式");
            if (string.IsNullOrEmpty(myOption.FilePattern) && string.IsNullOrEmpty(myOption.MultiFilePattern))
            {
                MultiPageDefaultSavePath = MultiPageDefaultSavePath.Replace("<pageNumberWithZero>", "<pageNumber>");
                LogWarn($"已切换至 -M \"{MultiPageDefaultSavePath}\"");
            }
        }
        if (myOption.BandwithAscending)
        {
            LogWarn("--bandwith-ascending 已被弃用, 建议使用 --video-ascending 与 --audio-ascending 来指定视频或音频是否升序, 本次执行已将视频与音频均设为升序");
            myOption.VideoAscending = true;
            myOption.AudioAscending = true;
        }
    }

    /// <summary>
    /// 解析用户指定的编码优先级
    /// </summary>
    /// <param name="myOption"></param>
    /// <returns></returns>
    private static Dictionary<string, byte> ParseEncodingPriority(MyOption myOption, out string firstEncoding)
    {
        var encodingPriority = new Dictionary<string, byte>();
        firstEncoding = "";
        if (myOption.EncodingPriority != null)
        {
            var encodingPriorityTemp = myOption.EncodingPriority
                .ToUpper()
                .Replace('，', ',')
                .Replace("-", string.Empty)
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(s => !string.IsNullOrEmpty(s)).ToList();
            byte index = 0;
            firstEncoding = encodingPriorityTemp.First();
            foreach (string encoding in encodingPriorityTemp)
            {
                if (encodingPriority.ContainsKey(encoding))
                    continue;
                encodingPriority[encoding] = index;
                index++;
            }
        }
        return encodingPriority;
    }

    private static BBDownDanmakuFormat[] ParseDownloadDanmakuFormats(MyOption myOption)
    {
        if (string.IsNullOrEmpty(myOption.DownloadDanmakuFormats)) return BBDownDanmakuFormatInfo.DefaultFormats;

        var formats = myOption.DownloadDanmakuFormats.Replace("，", ",").ToLower().Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (formats.Any(format => !BBDownDanmakuFormatInfo.AllFormatNames.Contains(format)))
        {
            LogError($"包含不支持的下载弹幕格式：{myOption.DownloadDanmakuFormats}");
            return BBDownDanmakuFormatInfo.DefaultFormats;
        }
        
        return formats.Select(BBDownDanmakuFormatInfo.FromFormatName).ToArray();
    }

    /// <summary>
    /// 解析用户输入的清晰度规格优先级
    /// </summary>
    /// <param name="myOption"></param>
    /// <returns></returns>
    private static Dictionary<string, int> ParseDfnPriority(MyOption myOption)
    {
        var dfnPriority = new Dictionary<string, int>();
        if (myOption.DfnPriority != null)
        {
            var dfnPriorityTemp = myOption.DfnPriority.Replace("，", ",").Split(',').Select(s => s.ToUpper().Trim()).Where(s => !string.IsNullOrEmpty(s));
            int index = 0;
            foreach (string dfn in dfnPriorityTemp)
            {
                if (dfnPriority.ContainsKey(dfn)) { continue; }
                dfnPriority[dfn] = index;
                index++;
            }
        }
        return dfnPriority;
    }

    /// <summary>
    /// 寻找并设置所需的二进制文件
    /// </summary>
    /// <param name="myOption"></param>
    /// <exception cref="Exception"></exception>
    private static void FindBinaries(MyOption myOption)
    {
        if (!string.IsNullOrEmpty(myOption.FFmpegPath) && File.Exists(myOption.FFmpegPath))
        {
            BBDownMuxer.FFMPEG = myOption.FFmpegPath;
        }

        if (!string.IsNullOrEmpty(myOption.Mp4boxPath) && File.Exists(myOption.Mp4boxPath))
        {
            BBDownMuxer.MP4BOX = myOption.Mp4boxPath;
        }

        if (!string.IsNullOrEmpty(myOption.Aria2cPath) && File.Exists(myOption.Aria2cPath))
        {
            BBDownAria2c.ARIA2C = myOption.Aria2cPath;
        }
        //寻找ffmpeg或mp4box
        if (!myOption.SkipMux)
        {
            if (myOption.UseMP4box)
            {
                if (string.IsNullOrEmpty(BBDownMuxer.MP4BOX) || !File.Exists(BBDownMuxer.MP4BOX))
                {
                    var binPath = FindExecutable("mp4box") ?? FindExecutable("MP4box");
                    if (string.IsNullOrEmpty(binPath))
                        throw new Exception("找不到可执行的mp4box文件");
                    BBDownMuxer.MP4BOX = binPath;
                }
            }
            else if (string.IsNullOrEmpty(BBDownMuxer.FFMPEG) || !File.Exists(BBDownMuxer.FFMPEG))
            {
                var binPath = FindExecutable("ffmpeg");
                if (string.IsNullOrEmpty(binPath))
                    throw new Exception("找不到可执行的ffmpeg文件");
                BBDownMuxer.FFMPEG = binPath;
            }
        }

        //寻找aria2c
        if (myOption.UseAria2c)
        {
            if (string.IsNullOrEmpty(BBDownAria2c.ARIA2C) || !File.Exists(BBDownAria2c.ARIA2C))
            {
                var binPath = FindExecutable("aria2c");
                if (string.IsNullOrEmpty(binPath))
                    throw new Exception("找不到可执行的aria2c文件");
                BBDownAria2c.ARIA2C = binPath;
            }

        }
    }

    /// <summary>
    /// 处理有冲突的选项
    /// </summary>
    /// <param name="myOption"></param>
    private static void HandleConflictingOptions(MyOption myOption)
    {
        //手动选择时不能隐藏流
        if (myOption.Interactive)
        {
            myOption.HideStreams = false;
        }
        //audioOnly和videoOnly同时开启则全部忽视
        if (myOption.AudioOnly && myOption.VideoOnly)
        {
            myOption.AudioOnly = false;
            myOption.VideoOnly = false;
        }
        if (myOption.SkipSubtitle)
        {
            myOption.SubOnly = false;
        }
    }

    /// <summary>
    /// 设置用户输入的自定义工作目录
    /// </summary>
    /// <param name="myOption"></param>
    private static void ChangeWorkingDir(MyOption myOption)
    {
        if (!string.IsNullOrEmpty(myOption.WorkDir))
        {
            //解释环境变量
            myOption.WorkDir = Environment.ExpandEnvironmentVariables(myOption.WorkDir);
            var dir = Path.GetFullPath(myOption.WorkDir);
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
            //设置工作目录
            Environment.CurrentDirectory = dir;
            LogDebug("切换工作目录至：{0}", dir);
        }
    }

    /// <summary>
    /// 加载用户的认证信息（cookie或token）
    /// </summary>
    /// <param name="myOption"></param>
    private static void LoadCredentials(MyOption myOption)
    {
        if (string.IsNullOrEmpty(Config.COOKIE) && File.Exists(Path.Combine(APP_DIR, "BBDown.data")))
        {
            Log("加载本地cookie...");
            LogDebug("文件路径：{0}", Path.Combine(APP_DIR, "BBDown.data"));
            Config.COOKIE = File.ReadAllText(Path.Combine(APP_DIR, "BBDown.data"));
        }
        if (string.IsNullOrEmpty(Config.TOKEN) && File.Exists(Path.Combine(APP_DIR, "BBDownTV.data")) && myOption.UseTvApi)
        {
            Log("加载本地token...");
            LogDebug("文件路径：{0}", Path.Combine(APP_DIR, "BBDownTV.data"));
            Config.TOKEN = File.ReadAllText(Path.Combine(APP_DIR, "BBDownTV.data"));
            Config.TOKEN = Config.TOKEN.Replace("access_token=", "");
        }
        if (string.IsNullOrEmpty(Config.TOKEN) && File.Exists(Path.Combine(APP_DIR, "BBDownApp.data")) && myOption.UseAppApi)
        {
            Log("加载本地token...");
            LogDebug("文件路径：{0}", Path.Combine(APP_DIR, "BBDownApp.data"));
            Config.TOKEN = File.ReadAllText(Path.Combine(APP_DIR, "BBDownApp.data"));
            Config.TOKEN = Config.TOKEN.Replace("access_token=", "");
        }
    }

    private static object fileLock = new object();
    public static void SaveAidToFile(string aid)
    {
        lock (fileLock)
        {
            string filePath = Path.Combine(APP_DIR, "BBDown.archives");
            LogDebug("文件路径：{0}", filePath);
            File.AppendAllText(filePath, $"{aid}|");
        }
    }

    public static bool CheckAidFromFile(string aid)
    {
        lock (fileLock)
        {
            string filePath = Path.Combine(APP_DIR, "BBDown.archives");
            if (!File.Exists(filePath)) return false;
            LogDebug("文件路径：{0}", filePath);
            var text = File.ReadAllText(filePath);
            return text.Split('|').Any(item => item == aid);
        }
    }

    /// <summary>
    /// 获取选中的分P列表
    /// </summary>
    /// <param name="myOption"></param>
    /// <param name="vInfo"></param>
    /// <param name="input"></param>
    /// <returns></returns>
    private static List<string>? GetSelectedPages(MyOption myOption, VInfo vInfo, string input)
    {
        List<string>? selectedPages = null;
        List<Page> pagesInfo = vInfo.PagesInfo;
        string selectPage = myOption.SelectPage.ToUpper().Trim().Trim(',');

        if (string.IsNullOrEmpty(selectPage))
        {
            //如果用户没有选择分P, 根据epid或query param来确定某一集
            if (!string.IsNullOrEmpty(vInfo.Index))
            {
                selectedPages = [vInfo.Index];
                Log("程序已自动选择你输入的集数, 如果要下载其他集数请自行指定分P(如可使用-p ALL代表全部)");
            }
            else if (!string.IsNullOrEmpty(GetQueryString("p", input)))
            {
                selectedPages = [GetQueryString("p", input)];
                Log("程序已自动选择你输入的集数, 如果要下载其他集数请自行指定分P(如可使用-p ALL代表全部)");
            }
        }
        else if (selectPage != "ALL")
        {
            selectedPages = new List<string>();

            //选择最新分P
            string lastPage = pagesInfo.Count.ToString();
            foreach (string key in new[] { "LAST", "NEW", "LATEST" })
            {
                selectPage = selectPage.Replace(key, lastPage);
            }

            try
            {
                if (selectPage.Contains('-'))
                {
                    string[] tmp = selectPage.Split('-');
                    int start = int.Parse(tmp[0]);
                    int end = int.Parse(tmp[1]);
                    for (int i = start; i <= end; i++)
                    {
                        selectedPages.Add(i.ToString());
                    }
                }
                else
                {
                    foreach (var s in selectPage.Split(','))
                    {
                        selectedPages.Add(s);
                    }
                }
            }
            catch { LogError("解析分P参数时失败了~"); selectedPages = null; };
        }

        return selectedPages;
    }

    /// <summary>
    /// 处理CDN域名
    /// </summary>
    /// <param name="myOption"></param>
    /// <param name="video"></param>
    /// <param name="audio"></param>
    private static void HandlePcdn(MyOption myOption, Video? selectedVideo, Audio? selectedAudio)
    {
        if (myOption.UposHost == "")
        {
            //处理PCDN
            if (!myOption.AllowPcdn)
            {
                var pcdnReg = PcdnRegex();
                if (selectedVideo != null && pcdnReg.IsMatch(selectedVideo.baseUrl))
                {
                    LogWarn($"检测到视频流为PCDN, 尝试强制替换为{BACKUP_HOST}……");
                    selectedVideo.baseUrl = pcdnReg.Replace(selectedVideo.baseUrl, $"://{BACKUP_HOST}/");
                }
                if (selectedAudio != null && pcdnReg.IsMatch(selectedAudio.baseUrl))
                {
                    LogWarn($"检测到音频流为PCDN, 尝试强制替换为{BACKUP_HOST}……");
                    selectedAudio.baseUrl = pcdnReg.Replace(selectedAudio.baseUrl, $"://{BACKUP_HOST}/");
                }
            }

            var akamReg = AkamRegex();
            if (selectedVideo != null && Config.AREA != "" && selectedVideo.baseUrl.Contains("akamaized.net"))
            {
                LogWarn($"检测到视频流为外国源, 尝试强制替换为{BACKUP_HOST}……");
                selectedVideo.baseUrl = akamReg.Replace(selectedVideo.baseUrl, $"://{BACKUP_HOST}/");
            }
            if (selectedAudio != null && Config.AREA != "" && selectedAudio.baseUrl.Contains("akamaized.net"))
            {
                LogWarn($"检测到音频流为外国源, 尝试强制替换为{BACKUP_HOST}……");
                selectedAudio.baseUrl = akamReg.Replace(selectedAudio.baseUrl, $"://{BACKUP_HOST}/");
            }
        }
        else
        {
            if (selectedVideo != null)
            {
                LogWarn($"尝试将视频流强制替换为{myOption.UposHost}……");
                selectedVideo.baseUrl = UposRegex().Replace(selectedVideo.baseUrl, $"://{myOption.UposHost}/");
            }
            if (selectedAudio != null)
            {
                LogWarn($"尝试将音频流强制替换为{myOption.UposHost}……");
                selectedAudio.baseUrl = UposRegex().Replace(selectedAudio.baseUrl, $"://{myOption.UposHost}/");
            }
        }
    }

    /// <summary>
    /// 打印解析到的各个轨道信息
    /// </summary>
    /// <param name="parsedResult"></param>
    /// <param name="pageDur"></param>
    private static void PrintAllTracksInfo(ParsedResult parsedResult, int pageDur, bool onlyShowInfo)
    {
        if (parsedResult.BackgroundAudioTracks.Any() && parsedResult.RoleAudioList.Any())
        {
            Log($"共计{parsedResult.BackgroundAudioTracks.Count}条背景音频流.");
            int index = 0;
            foreach (var a in parsedResult.BackgroundAudioTracks)
            {
                int pDur = pageDur == 0 ? a.dur : pageDur;
                LogColor($"{index++}. [{a.codecs}] [{a.bandwith} kbps] [~{FormatFileSize(pDur * a.bandwith * 1024 / 8)}]", false);
            }
            Log($"共计{parsedResult.RoleAudioList.Count}条配音, 每条包含{parsedResult.RoleAudioList[0].audio.Count}条配音流.");
            index = 0;
            foreach (var a in parsedResult.RoleAudioList[0].audio)
            {
                int pDur = pageDur == 0 ? a.dur : pageDur;
                LogColor($"{index++}. [{a.codecs}] [{a.bandwith} kbps] [~{FormatFileSize(pDur * a.bandwith * 1024 / 8)}]", false);
            }
        }
        //展示所有的音视频流信息
        if (parsedResult.VideoTracks.Any())
        {
            Log($"共计{parsedResult.VideoTracks.Count}条视频流.");
            int index = 0;
            foreach (var v in parsedResult.VideoTracks)
            {
                int pDur = pageDur == 0 ? v.dur : pageDur;
                var size = v.size > 0 ? v.size : pDur * v.bandwith * 1024 / 8;
                LogColor($"{index++}. [{v.dfn}] [{v.res}] [{v.codecs}] [{v.fps}] [{v.bandwith} kbps] [~{FormatFileSize(size)}]".Replace("[] ", ""), false);
                if (onlyShowInfo) Console.WriteLine(v.baseUrl);
            }
        }
        if (parsedResult.AudioTracks.Any())
        {
            Log($"共计{parsedResult.AudioTracks.Count}条音频流.");
            int index = 0;
            foreach (var a in parsedResult.AudioTracks)
            {
                int pDur = pageDur == 0 ? a.dur : pageDur;
                LogColor($"{index++}. [{a.codecs}] [{a.bandwith} kbps] [~{FormatFileSize(pDur * a.bandwith * 1024 / 8)}]", false);
                if (onlyShowInfo) Console.WriteLine(a.baseUrl);
            }
        }
    }

    private static void PrintSelectedTrackInfo(Video? selectedVideo, Audio? selectedAudio, int pageDur)
    {
        if (selectedVideo != null)
        {
            int pDur = pageDur == 0 ? selectedVideo.dur : pageDur;
            var size = selectedVideo.size > 0 ? selectedVideo.size : pDur * selectedVideo.bandwith * 1024 / 8;
            LogColor($"[视频] [{selectedVideo.dfn}] [{selectedVideo.res}] [{selectedVideo.codecs}] [{selectedVideo.fps}] [{selectedVideo.bandwith} kbps] [~{FormatFileSize(size)}]".Replace("[] ", ""), false);
        }
        if (selectedAudio != null)
        {
            int pDur = pageDur == 0 ? selectedAudio.dur : pageDur;
            LogColor($"[音频] [{selectedAudio.codecs}] [{selectedAudio.bandwith} kbps] [~{FormatFileSize(pDur * selectedAudio.bandwith * 1024 / 8)}]", false);
        }
    }

    /// <summary>
    /// 引导用户进行手动选择轨道
    /// </summary>
    /// <param name="parsedResult"></param>
    /// <param name="vIndex"></param>
    /// <param name="aIndex"></param>
    private static void SelectTrackManually(ParsedResult parsedResult, ref int vIndex, ref int aIndex)
    {
        if (parsedResult.VideoTracks.Any())
        {
            Log("请选择一条视频流(输入序号): ", false);
            Console.ForegroundColor = ConsoleColor.Cyan;
            vIndex = Convert.ToInt32(Console.ReadLine());
            if (vIndex > parsedResult.VideoTracks.Count || vIndex < 0) vIndex = 0;
            Console.ResetColor();
        }
        if (parsedResult.AudioTracks.Any())
        {
            Log("请选择一条音频流(输入序号): ", false);
            Console.ForegroundColor = ConsoleColor.Cyan;
            aIndex = Convert.ToInt32(Console.ReadLine());
            if (aIndex > parsedResult.AudioTracks.Count || aIndex < 0) aIndex = 0;
            Console.ResetColor();
        }
    }

    /// <summary>
    /// 下载轨道
    /// </summary>
    /// <returns></returns>
    private static async Task DownloadTrackAsync(string url, string destPath, DownloadConfig downloadConfig, bool video)
    {
        if (downloadConfig.MultiThread && !url.Contains("-cmcc-"))
        {
            await MultiThreadDownloadFileAsync(url, destPath, downloadConfig);
            Log($"合并{(video ? "视频" : "音频")}分片...");
            CombineMultipleFilesIntoSingleFile(GetFiles(Path.GetDirectoryName(destPath)!, $".{(video ? "v" : "a")}clip"), destPath);
            Log("清理分片...");
            foreach (var file in new DirectoryInfo(Path.GetDirectoryName(destPath)!).EnumerateFiles("*.?clip")) file.Delete();
        }
        else
        {
            if (downloadConfig.MultiThread && url.Contains("-cmcc-"))
            {
                LogWarn("检测到cmcc域名cdn, 已经禁用多线程");
                downloadConfig.ForceHttp = false;
            }
            await DownloadFileAsync(url, destPath, downloadConfig);
        }
    }

    [GeneratedRegex("://.*:\\d+/")]
    private static partial Regex PcdnRegex();
    [GeneratedRegex("://.*akamaized\\.net/")]
    private static partial Regex AkamRegex();
    [GeneratedRegex("://[^/]+/")]
    private static partial Regex UposRegex();
}
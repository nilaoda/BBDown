using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using static BBDown.BBDownEntity;
using static BBDown.BBDownUtil;
using static BBDown.BBDownLogger;
using static BBDown.BBDownMuxer;
using System.Linq;
namespace BBDown
{


    public class MyOption
    {
        public string Url { get; set; }
        public bool UseTvApi { get; set; }
        public bool OnlyHevc { get; set; }
        public bool OnlyShowInfo { get; set; }
        public bool ShowAll { get; set; }
        public bool UseAria2c { get; set; }
        public bool Interactive { get; set; }
        public bool HideStreams { get; set; }
        public bool MultiThread { get; set; }
        public bool VideoOnly { get; set; }
        public bool AudioOnly { get; set; }
        public bool Debug { get; set; }
        public bool SkipMux { get; set; }
        public string SelectPage { get; set; } = "";
        public string Cookie { get; set; } = "";
        public string AccessToken { get; set; } = "";

        public override string ToString()
        {
            return $"{{Input={Url}, {nameof(UseTvApi)}={UseTvApi.ToString()}, " +
                $"{nameof(OnlyHevc)}={OnlyHevc.ToString()}, " +
                $"{nameof(OnlyShowInfo)}={OnlyShowInfo.ToString()}, " +
                $"{nameof(Interactive)}={Interactive.ToString()}, " +
                $"{nameof(HideStreams)}={HideStreams.ToString()}, " +
                $"{nameof(ShowAll)}={ShowAll.ToString()}, " +
                $"{nameof(UseAria2c)}={UseAria2c.ToString()}, " +
                $"{nameof(MultiThread)}={MultiThread.ToString()}, " +
                $"{nameof(VideoOnly)}={VideoOnly.ToString()}, " +
                $"{nameof(AudioOnly)}={AudioOnly.ToString()}, " +
                $"{nameof(Debug)}={Debug.ToString()}, " +
                $"{nameof(SelectPage)}={SelectPage}, " +
                $"{nameof(Cookie)}={Cookie}, " +
                $"{nameof(AccessToken)}={AccessToken}}}";
        }
    }

    partial class Program
    {

        public static (Video, Audio, bool) VideoSelector(List<Video> videoTracks, List<Audio> audioTracks, MyOption myOption, string outPath, int pDur)
        {

            /*debug 数据*/
            bool interactMode = myOption.Interactive;
            bool infoMode = myOption.OnlyShowInfo;
            bool tvApi = myOption.UseTvApi;
            bool hevc = myOption.OnlyHevc;
            bool hideStreams = myOption.HideStreams;
            bool multiThread = myOption.MultiThread;
            bool audioOnly = myOption.AudioOnly;
            bool videoOnly = myOption.VideoOnly;
            bool skipMux = myOption.SkipMux;
            bool showAll = myOption.ShowAll;
            bool useAria2c = myOption.UseAria2c;
            DEBUG_LOG = myOption.Debug;
            string input = myOption.Url;
            string selectPage = myOption.SelectPage.ToUpper();

            /**/
            //降序
            videoTracks.Sort(Compare);
            audioTracks.Sort(Compare);
            int vIndex = 0;
            int aIndex = 0;

            if (!hideStreams)
            {
                //展示所有的音视频流信息
                if (!audioOnly)
                {
                    Log($"共计{videoTracks.Count}条视频流.");
                    int index = 0;
                    foreach (var v in videoTracks)
                    {
                        LogColor($"{index++}. [{v.dfn}] [{v.res}] [{v.codecs}] [{v.fps}] [{v.bandwith} kbps] [~{FormatFileSize(pDur * v.bandwith * 1024 / 8)}]".Replace("[] ", ""), false);
                        if (infoMode) Console.WriteLine(v.baseUrl);
                    }
                }
                if (!videoOnly)
                {
                    Log($"共计{audioTracks.Count}条音频流.");
                    int index = 0;
                    foreach (var a in audioTracks)
                    {
                        LogColor($"{index++}. [{a.codecs}] [{a.bandwith} kbps] [~{FormatFileSize(pDur * a.bandwith * 1024 / 8)}]", false);
                        if (infoMode) Console.WriteLine(a.baseUrl);
                    }
                }
            }
            if (infoMode)
            {
                return (new Video(), new Audio(), false);
            }
            if (interactMode && !hideStreams)
            {
                Log("请选择一条视频流(输入序号): ", false);
                Console.ForegroundColor = ConsoleColor.Cyan;
                vIndex = Convert.ToInt32(Console.ReadLine());
                if (vIndex > videoTracks.Count || vIndex < 0) vIndex = 0;
                Console.ResetColor();
                Log("请选择一条音频流(输入序号): ", false);
                Console.ForegroundColor = ConsoleColor.Cyan;
                aIndex = Convert.ToInt32(Console.ReadLine());
                if (aIndex > audioTracks.Count || aIndex < 0) aIndex = 0;
                Console.ResetColor();
            }
            if (File.Exists(outPath) && new FileInfo(outPath).Length != 0)
            {
                Log($"{outPath}已存在, 跳过下载...");
                return (new Video(), new Audio(), false);
            }

            if (audioOnly) videoTracks.Clear();
            if (videoOnly) audioTracks.Clear();

            Log($"已选择的流:");
            if (videoTracks.Count > 0)
                LogColor($"[视频] [{videoTracks[vIndex].dfn}] [{videoTracks[vIndex].res}] [{videoTracks[vIndex].codecs}] [{videoTracks[vIndex].fps}] [{videoTracks[vIndex].bandwith} kbps] [~{FormatFileSize(pDur * videoTracks[vIndex].bandwith * 1024 / 8)}]".Replace("[] ", ""), false);
            if (audioTracks.Count > 0)
                LogColor($"[音频] [{audioTracks[aIndex].codecs}] [{audioTracks[aIndex].bandwith} kbps] [~{FormatFileSize(pDur * audioTracks[aIndex].bandwith * 1024 / 8)}]", false);

            return (videoTracks[aIndex], audioTracks[aIndex], true);
        }


        public static async Task DownLoadData(BBDownVInfo vInfo, List<Subtitle> subtitleInfo, Page p, List<Video> videoTracks, List<Audio> audioTracks, Video video, Audio audio, MyOption myOption, string indexStr)
        {

            string videoPath = $"{p.aid}/{p.aid}.P{indexStr}.{p.cid}.mp4";
            string audioPath = $"{p.aid}/{p.aid}.P{indexStr}.{p.cid}.m4a";
            /*debug 数据*/
            bool interactMode = myOption.Interactive;
            bool infoMode = myOption.OnlyShowInfo;
            bool tvApi = !myOption.UseTvApi;
            bool hevc = myOption.OnlyHevc;
            bool hideStreams = myOption.HideStreams;
            bool multiThread = myOption.MultiThread;
            bool audioOnly = myOption.AudioOnly;
            bool videoOnly = myOption.VideoOnly;
            bool skipMux = myOption.SkipMux;
            bool showAll = myOption.ShowAll;
            bool useAria2c = myOption.UseAria2c;
            DEBUG_LOG = myOption.Debug;
            string input = myOption.Url;
            string selectPage = myOption.SelectPage.ToUpper();

            /**/

            List<Page> pagesInfo = vInfo.PagesInfo;
            string title = vInfo.Title;
            string desc = vInfo.Desc;
            string pic = vInfo.Pic;
            string pubTime = vInfo.PubTime;
            bool bangumi = vInfo.IsBangumi;
            bool cheese = vInfo.IsCheese;
            //处理文件夹以.结尾导致的异常情况
            if (title.EndsWith(".")) title += "_fix";
            string outPath = GetValidFileName(title) + (vInfo.PagesInfo.Count > 1 ? $"/[P{indexStr}]{GetValidFileName(p.title)}" : (vInfo.PagesInfo.Count > 1 ? $"[P{indexStr}]{GetValidFileName(p.title)}" : "")) + ".mp4";

            #region  下载

            if (multiThread && !video.baseUrl.Contains("-cmcc-"))
            {
                if (videoTracks.Count > 0)
                {
                    Log($"开始多线程下载P{p.index}视频...");
                    await MultiThreadDownloadFileAsync(video.baseUrl, videoPath, useAria2c);
                    Log("合并视频分片...");
                    CombineMultipleFilesIntoSingleFile(GetFiles(Path.GetDirectoryName(videoPath), ".vclip"), videoPath);
                }
                if (audioTracks.Count > 0)
                {
                    Log($"开始多线程下载P{p.index}音频...");
                    await MultiThreadDownloadFileAsync(audio.baseUrl, audioPath, useAria2c);
                    Log("合并音频分片...");
                    CombineMultipleFilesIntoSingleFile(GetFiles(Path.GetDirectoryName(audioPath), ".aclip"), audioPath);
                }
                Log("清理分片...");
                foreach (var file in new DirectoryInfo(Path.GetDirectoryName(videoPath)).EnumerateFiles("*.?clip")) file.Delete();
            }
            else
            {
                if (multiThread && video.baseUrl.Contains("-cmcc-"))
                    LogError("检测到cmcc域名cdn, 已经禁用多线程");
                if (videoTracks.Count > 0)
                {
                    Log($"开始下载P{p.index}视频...");
                    await DownloadFile(video.baseUrl, videoPath, useAria2c);
                }
                if (audioTracks.Count > 0)
                {
                    Log($"开始下载P{p.index}音频...");
                    await DownloadFile(audio.baseUrl, audioPath, useAria2c);
                }
            }
            Log($"下载P{p.index}完毕");
            if (videoTracks.Count == 0) videoPath = "";
            if (audioTracks.Count == 0) audioPath = "";
            if (skipMux) return;
            Log("开始合并音视频" + (subtitleInfo.Count > 0 ? "和字幕" : "") + "...");
            int code = MuxAV(videoPath, audioPath, outPath,
                desc,
                title,
                vInfo.PagesInfo.Count > 1 ? ($"P{indexStr}.{p.title}") : "",
                File.Exists($"{p.aid}/{p.aid}.jpg") ? $"{p.aid}/{p.aid}.jpg" : "",
                subtitleInfo, audioOnly, videoOnly);
            if (code != 0 || !File.Exists(outPath) || new FileInfo(outPath).Length == 0)
            {
                LogError("合并失败"); return;
            }
            Log("清理临时文件...");
            if (videoTracks.Count > 0) File.Delete(videoPath);
            if (audioTracks.Count > 0) File.Delete(audioPath);
            foreach (var s in subtitleInfo) File.Delete(s.path);
            if (pagesInfo.Count == 1 || p.index == pagesInfo.Last().index || p.aid != pagesInfo.Last().aid)
                File.Delete($"{p.aid}/{p.aid}.jpg");
            if (Directory.Exists(p.aid) && Directory.GetFiles(p.aid).Length == 0) Directory.Delete(p.aid, true);
            #endregion
        }

        public static string SingleVideoSelector(Video video, MyOption myOption)
        {
            /*debug 数据*/
            bool interactMode = myOption.Interactive;

            /**/
            //降序

            string q = "125";
            int lastQn = 0;
            video.Dfns.ForEach(delegate (string key)
                {
                    //获取最高画质作为默认值
                    int tmpVal = 0;
                    bool b = int.TryParse(key, out tmpVal);
                    if (b && tmpVal > lastQn)
                    {
                        q = tmpVal.ToString();
                        lastQn = tmpVal;
                    }
                });

            if (interactMode)
            {
                int i = 0;
                video.Dfns.ForEach(delegate (string key)
                {
                    LogColor($"{i++}.{qualitys[key]}");
                });
                Log("请选择最想要的清晰度(输入序号): ", false);
                Console.ForegroundColor = ConsoleColor.Cyan;
                var vIndex = Convert.ToInt32(Console.ReadLine());
                if (vIndex > video.Dfns.Count || vIndex < 0) vIndex = 0;
                Console.ResetColor();
                // //重新解析
                // webJson = GetPlayJson(aidOri, p.aid, p.cid, p.epid, tvApi, v1.Dfns[vIndex]);
                q = video.Dfns[vIndex];

            }
            return q;
        }




        public static async Task DownloadFlvFile(BBDownVInfo vInfo, List<Subtitle> subtitleInfo, Page p, List<Video> videoTracks, Video video, MyOption myOption, string indexStr)
        {

            string videoPath = $"{p.aid}/{p.aid}.P{indexStr}.{p.cid}.mp4";

            bool multiThread = myOption.MultiThread;
            bool infoMode = myOption.OnlyShowInfo;
            bool useAria2c = myOption.UseAria2c;
            bool skipMux = myOption.SkipMux;
            bool audioOnly = myOption.AudioOnly;
            bool videoOnly = myOption.VideoOnly;

            List<Page> pagesInfo = vInfo.PagesInfo;
            string title = vInfo.Title;
            string desc = vInfo.Desc;
            string pic = vInfo.Pic;
            string pubTime = vInfo.PubTime;
            bool bangumi = vInfo.IsBangumi;
            bool cheese = vInfo.IsCheese;
            //处理文件夹以.结尾导致的异常情况
            if (title.EndsWith(".")) title += "_fix";
            string outPath = GetValidFileName(title) + (vInfo.PagesInfo.Count > 1 ? $"/[P{indexStr}]{GetValidFileName(p.title)}" : (vInfo.PagesInfo.Count > 1 ? $"[P{indexStr}]{GetValidFileName(p.title)}" : "")) + ".mp4";

            //降序
            videoTracks.Sort(Compare);
            //TODO 下载
            Log($"共计{videoTracks.Count}条流({video.Format}, 共有{video.Clips.Count}个分段).");
            int index = 0;
            foreach (var v in videoTracks)
            {
                LogColor($"{index++}. [{v.dfn}] [{v.res}] [{v.codecs}] [{v.fps}] [~{(video.Size / 1024 / (video.Length / 1000) * 8).ToString("00")} kbps] [{FormatFileSize(video.Size)}]".Replace("[] ", ""), false);
                if (infoMode)
                {
                    video.Clips.ForEach(delegate (string c) { Console.WriteLine(c); });
                }
            }
            if (infoMode) return;
            if (File.Exists(outPath) && new FileInfo(outPath).Length != 0)
            {
                Log($"{outPath}已存在, 跳过下载...");
                return;
            }
            var pad = string.Empty.PadRight(video.Clips.Count.ToString().Length, '0');
            for (int i = 0; i < video.Clips.Count; i++)
            {
                var link = video.Clips[i];
                videoPath = $"{p.aid}/{p.aid}.P{indexStr}.{p.cid}.{i.ToString(pad)}.mp4";
                if (multiThread && !link.Contains("-cmcc-"))
                {
                    if (videoTracks.Count != 0)
                    {
                        Log($"开始多线程下载P{p.index}视频, 片段({(i + 1).ToString(pad)}/{video.Clips.Count})...");
                        await MultiThreadDownloadFileAsync(link, videoPath, useAria2c);
                        Log("合并视频分片...");
                        CombineMultipleFilesIntoSingleFile(GetFiles(Path.GetDirectoryName(videoPath), ".vclip"), videoPath);
                    }
                    Log("清理分片...");
                    foreach (var file in new DirectoryInfo(Path.GetDirectoryName(videoPath)).EnumerateFiles("*.?clip")) file.Delete();
                }
                else
                {
                    if (multiThread && link.Contains("-cmcc-"))
                        LogError("检测到cmcc域名cdn, 已经禁用多线程");
                    if (videoTracks.Count != 0)
                    {
                        Log($"开始下载P{p.index}视频, 片段({(i + 1).ToString(pad)}/{video.Clips.Count})...");
                        await DownloadFile(link, videoPath, useAria2c);
                    }
                }
            }
            Log($"下载P{p.index}完毕");
            Log("开始合并分段...");
            var files = GetFiles(Path.GetDirectoryName(videoPath), ".mp4");
            videoPath = $"{p.aid}/{p.aid}.P{indexStr}.{p.cid}.mp4";
            MergeFLV(files, videoPath);
            if (skipMux) return;
            Log("开始混流视频" + (subtitleInfo.Count > 0 ? "和字幕" : "") + "...");
            int code = MuxAV(videoPath, "", outPath,
                desc,
                title,
                vInfo.PagesInfo.Count > 1 ? ($"P{indexStr}.{p.title}") : "",
                File.Exists($"{p.aid}/{p.aid}.jpg") ? $"{p.aid}/{p.aid}.jpg" : "",
                subtitleInfo, audioOnly, videoOnly);
            if (code != 0 || !File.Exists(outPath) || new FileInfo(outPath).Length == 0)
            {
                LogError("合并失败"); return;
            }
            Log("清理临时文件...");
            if (videoTracks.Count != 0) File.Delete(videoPath);
            foreach (var s in subtitleInfo) File.Delete(s.path);
            if (pagesInfo.Count == 1 || p.index == pagesInfo.Last().index || p.aid != pagesInfo.Last().aid)
                File.Delete($"{p.aid}/{p.aid}.jpg");
            if (Directory.Exists(p.aid) && Directory.GetFiles(p.aid).Length == 0) Directory.Delete(p.aid, true);


        }
    }
}
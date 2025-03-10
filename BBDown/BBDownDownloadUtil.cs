using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net;
using System.Threading.Tasks;
using static BBDown.Core.Entity.Entity;
using static BBDown.Core.Logger;
using static BBDown.Core.Util.HTTPUtil;
using System.Collections.Concurrent;

namespace BBDown;

internal static class BBDownDownloadUtil
{
    public class DownloadConfig
    {
        public bool UseAria2c { get; set; } = false;
        public string Aria2cArgs { get; set; } = string.Empty;
        public bool ForceHttp { get; set; } = false;
        public bool MultiThread { get; set; } = false;
        public DownloadTask? RelatedTask { get; set; } = null;
    }

    private static async Task RangeDownloadToTmpAsync(int id, string url, string tmpName, long fromPosition, long? toPosition, Action<int, long, long> onProgress, bool failOnRangeNotSupported = false)
    {
        DateTimeOffset? lastTime = File.Exists(tmpName) ? new FileInfo(tmpName).LastWriteTimeUtc : null;
        using var fileStream = new FileStream(tmpName, FileMode.Create);
        fileStream.Seek(0, SeekOrigin.End);
        var downloadedBytes = fromPosition + fileStream.Position;

        using var httpRequestMessage = new HttpRequestMessage();
        if (!url.Contains("platform=android_tv_yst") && !url.Contains("platform=android"))
            httpRequestMessage.Headers.TryAddWithoutValidation("Referer", "https://www.bilibili.com");
        httpRequestMessage.Headers.TryAddWithoutValidation("User-Agent", "Mozilla/5.0");
        httpRequestMessage.Headers.TryAddWithoutValidation("Cookie", Core.Config.COOKIE);
        httpRequestMessage.Headers.Range = new(downloadedBytes, toPosition);
        httpRequestMessage.Headers.IfRange = lastTime != null ? new(lastTime.Value) : null;
        httpRequestMessage.RequestUri = new(url);

        using var response = (await AppHttpClient.SendAsync(httpRequestMessage, HttpCompletionOption.ResponseHeadersRead)).EnsureSuccessStatusCode();

        if (response.StatusCode == HttpStatusCode.OK) // server doesn't response a partial content
        {
            if (failOnRangeNotSupported && (downloadedBytes > 0 || toPosition != null)) throw new NotSupportedException("Range request is not supported.");
            downloadedBytes = 0;
            fileStream.Seek(0, SeekOrigin.Begin);
        }

        using var stream = await response.Content.ReadAsStreamAsync();
        var totalBytes = downloadedBytes + (response.Content.Headers.ContentLength ?? long.MaxValue - downloadedBytes);

        const int blockSize = 1048576 / 4;
        var buffer = new byte[blockSize];

        while (downloadedBytes < totalBytes)
        {
            var recevied = await stream.ReadAsync(buffer);
            if (recevied == 0) break;
            await fileStream.WriteAsync(buffer.AsMemory(0, recevied));
            await fileStream.FlushAsync();
            downloadedBytes += recevied;
            onProgress(id, downloadedBytes - fromPosition, totalBytes);
        }

        if (response.Content.Headers.ContentLength != null && (response.Content.Headers.ContentLength != new FileInfo(tmpName).Length))
            throw new Exception("Retry...");
    }

    public static async Task DownloadFileAsync(string url, string path, DownloadConfig config)
    {
        if (string.IsNullOrEmpty(url)) return;
        if (config.ForceHttp) url = ReplaceUrl(url);
        LogDebug("Start downloading: {0}", url);
        string desDir = Path.GetDirectoryName(path)!;
        if (!string.IsNullOrEmpty(desDir) && !Directory.Exists(desDir)) Directory.CreateDirectory(desDir);
        if (config.UseAria2c)
        {
            await BBDownAria2c.DownloadFileByAria2cAsync(url, path, config.Aria2cArgs);
            if (File.Exists(path + ".aria2") || !File.Exists(path))
                throw new Exception("aria2下载可能存在错误");
            Console.WriteLine();
            return;
        }
        int retry = 0;
        string tmpName = Path.Combine(desDir, Path.GetFileNameWithoutExtension(path) + ".tmp");
        reDown:
        try
        {
            using var progress = new ProgressBar(config.RelatedTask);
            await RangeDownloadToTmpAsync(0, url, tmpName, 0, null, (_, downloaded, total) => progress.Report((double)downloaded / total, downloaded));
            File.Move(tmpName, path, true);
        }
        catch (Exception)
        {
            if (++retry == 3) throw;
            goto reDown;
        }
    }

    public static async Task MultiThreadDownloadFileAsync(string url, string path, DownloadConfig config)
    {
        if (config.ForceHttp) url = ReplaceUrl(url);
        LogDebug("Start downloading: {0}", url);
        if (config.UseAria2c)
        {
            await BBDownAria2c.DownloadFileByAria2cAsync(url, path, config.Aria2cArgs);
            if (File.Exists(path + ".aria2") || !File.Exists(path))
                throw new Exception("aria2下载可能存在错误");
            Console.WriteLine();
            return;
        }
        long fileSize = await GetFileSizeAsync(url);
        LogDebug("文件大小：{0} bytes", fileSize);
        //已下载过, 跳过下载
        if (File.Exists(path) && new FileInfo(path).Length == fileSize)
        {
            LogDebug("文件已下载过, 跳过下载");
            return;
        }
        List<Clip> allClips = GetAllClips(url, fileSize);
        int total = allClips.Count;
        LogDebug("分段数量：{0}", total);
        ConcurrentDictionary<int, long> clipProgress = new();
        foreach (var i in allClips) clipProgress[i.index] = 0;

        using var progress = new ProgressBar(config.RelatedTask);
        progress.Report(0);
        await Parallel.ForEachAsync(allClips, async (clip, _) =>
        {
            int retry = 0;
            string tmp = Path.Combine(Path.GetDirectoryName(path)!, clip.index.ToString("00000") + "_" + Path.GetFileNameWithoutExtension(path) + (Path.GetExtension(path).EndsWith(".mp4") ? ".vclip" : ".aclip"));
            reDown:
            try
            {
                await RangeDownloadToTmpAsync(clip.index, url, tmp, clip.from, clip.to == -1 ? null : clip.to, (index, downloaded, _) =>
                {
                    clipProgress[index] = downloaded;
                    progress.Report((double)clipProgress.Values.Sum() / fileSize, clipProgress.Values.Sum());
                }, true);
            }
            catch (NotSupportedException)
            {
                if (++retry == 3) throw new Exception($"服务器可能并不支持多线程下载, 请使用 --multi-thread false 关闭多线程");
                goto reDown;
            }
            catch (Exception)
            {
                if (++retry == 3) throw new Exception($"Failed to download clip {clip.index}");
                goto reDown;
            }
        });
    }

    //此函数主要是切片下载逻辑
    private static List<Clip> GetAllClips(string url, long fileSize)
    {
        List<Clip> clips = [];
        int index = 0;
        long counter = 0;
        int perSize = 20 * 1024 * 1024;
        while (fileSize > 0)
        {
            Clip c = new()
            {
                index = index,
                from = counter,
                to = counter + perSize
            };
            //没到最后
            if (fileSize - perSize > 0)
            {
                fileSize -= perSize;
                counter += perSize + 1;
                index++;
                clips.Add(c);
            }
            //已到最后
            else
            {
                c.to = -1;
                clips.Add(c);
                break;
            }
        }
        return clips;
    }

    private static async Task<long> GetFileSizeAsync(string url)
    {
        using var httpRequestMessage = new HttpRequestMessage();
        if (!url.Contains("platform=android_tv_yst") && !url.Contains("platform=android"))
            httpRequestMessage.Headers.TryAddWithoutValidation("Referer", "https://www.bilibili.com");
        httpRequestMessage.Headers.TryAddWithoutValidation("User-Agent", "Mozilla/5.0");
        httpRequestMessage.Headers.TryAddWithoutValidation("Cookie", Core.Config.COOKIE);
        httpRequestMessage.RequestUri = new(url);
        var response = (await AppHttpClient.SendAsync(httpRequestMessage, HttpCompletionOption.ResponseHeadersRead)).EnsureSuccessStatusCode();
        long totalSizeBytes = response.Content.Headers.ContentLength ?? 0;

        return totalSizeBytes;
    }

    /// <summary>
    /// 将下载地址强制转换为HTTP
    /// </summary>
    /// <param name="url"></param>
    /// <returns></returns>
    private static string ReplaceUrl(string url)
    {
        if (url.Contains(".mcdn.bilivideo.cn:"))
        {
            LogDebug("对[*.mcdn.bilivideo.cn:xxx]域名不做处理");
            return url;
        }

        LogDebug("将https更改为http");
        return url.Replace("https:", "http:");
    }
}
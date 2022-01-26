using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using static BBDown.BBDownEntity;
using static BBDown.BBDownLogger;
using static BBDown.BBDownUtil;

namespace BBDown
{
    /// <summary>
    /// 列表解析
    /// https://space.bilibili.com/23630128/channel/seriesdetail?sid=340933
    /// </summary>
    internal class BBDownSeriesListFetcher : IFetcher
    {
        public async Task<BBDownVInfo> FetchAsync(string id)
        {
            id = id.Substring(12);
            var seriesId = id.Split(':')[0];
            var mid = id.Split(':')[1];
            var aids = new List<string>();
            int pageSize = 30;
            var api = $"https://api.bilibili.com/x/series/archives?mid={mid}&series_id={seriesId}&only_normal=true&sort=desc&pn=1&ps={pageSize}";
            var json = await GetWebSourceAsync(api);
            using var infoJson = JsonDocument.Parse(json);
            var data = infoJson.RootElement.GetProperty("data");
            int totalCount = data.GetProperty("page").GetProperty("total").GetInt32();
            int totalPage = (int)Math.Ceiling((double)totalCount / pageSize);
            aids.AddRange(data.GetProperty("aids").EnumerateArray().ToArray().Select(o => o.ToString()));
            for (int page = 2; page <= totalPage; page++)
            {
                api = $"https://api.bilibili.com/x/series/archives?mid={mid}&series_id={seriesId}&only_normal=true&sort=desc&pn={page}&ps={pageSize}";
                json = await GetWebSourceAsync(api);
                using var jsonDoc = JsonDocument.Parse(json);
                data = jsonDoc.RootElement.GetProperty("data");
                aids.AddRange(data.GetProperty("aids").EnumerateArray().ToArray().Select(o => o.ToString()));
            }
            var urls = aids.Select(o => $"https://www.bilibili.com/video/av{o}");
            File.WriteAllText($"用户{mid}所创建的列表{seriesId}的所有视频.txt", string.Join(Environment.NewLine, urls));
            Log("目前下载器不支持下载用户创建的列表视频，不过程序已经获取到了该列表的全部投稿视频地址，你可以自行使用批处理脚本等手段调用本程序进行批量下载。如在Windows系统你可以使用如下代码：");
            Console.WriteLine();
            Console.WriteLine(@"@echo Off
For /F %%a in (urls.txt) Do (BBDown.exe ""%%a"")
pause");
            Console.WriteLine();
            throw new Exception("暂不支持该功能");
        }
    }
}

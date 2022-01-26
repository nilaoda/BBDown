using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using static BBDown.BBDownLogger;
using static BBDown.BBDownUtil;


namespace BBDown
{
    /// <summary>
    /// 收藏夹解析
    /// https://space.bilibili.com/4743331/favlist?spm_id_from=333.1007.0.0
    /// 
    /// </summary>
    internal class BBDownFavListFetcher : IFetcher
    {
        public async Task<BBDownVInfo> FetchAsync(string id)
        {
            id = id.Substring(6);
            var favId = id.Split(':')[0];
            var mid = id.Split(':')[1];
            //查找默认收藏夹
            if (favId == "")
            {
                var favListApi = $"https://api.bilibili.com/x/v3/fav/folder/created/list-all?up_mid={mid}&jsonp=jsonp";
                favId = JsonDocument.Parse(await GetWebSourceAsync(favListApi)).RootElement.GetProperty("data").GetProperty("list").EnumerateArray().First().GetProperty("id").ToString();
            }

            int pageSize = 20;
            var aids = new List<string>();

            var api = $"https://api.bilibili.com/x/v3/fav/resource/list?media_id={favId}&pn=1&ps={pageSize}&keyword=&order=mtime&type=0&tid=0&platform=web&jsonp=jsonp";
            var json = await GetWebSourceAsync(api);
            using var infoJson = JsonDocument.Parse(json);
            var data = infoJson.RootElement.GetProperty("data");
            int totalCount = data.GetProperty("info").GetProperty("media_count").GetInt32();
            int totalPage = (int)Math.Ceiling((double)totalCount / pageSize);
            var title = GetValidFileName(data.GetProperty("info").GetProperty("title").ToString());
            var userName = GetValidFileName(data.GetProperty("info").GetProperty("upper").GetProperty("name").ToString());
            aids.AddRange(data.GetProperty("medias").EnumerateArray().ToArray().Select(o => o.GetProperty("id").ToString()));
            for (int page = 2; page <= totalPage; page++)
            {
                api = $"https://api.bilibili.com/x/v3/fav/resource/list?media_id={favId}&pn={page}&ps={pageSize}&keyword=&order=mtime&type=0&tid=0&platform=web&jsonp=jsonp";
                json = await GetWebSourceAsync(api);
                using var jsonDoc = JsonDocument.Parse(json);
                data = jsonDoc.RootElement.GetProperty("data");
                aids.AddRange(data.GetProperty("medias").EnumerateArray().ToArray().Select(o => o.GetProperty("id").ToString()));
            }

            var urls = aids.Select(o => $"https://www.bilibili.com/video/av{o}");
            File.WriteAllText($"{userName}所创建的收藏夹[{title}]的所有视频.txt", string.Join(Environment.NewLine, urls));
            Log("目前下载器不支持下载用户创建的收藏夹，不过程序已经获取到了该收藏夹的全部投稿视频地址，你可以自行使用批处理脚本等手段调用本程序进行批量下载。如在Windows系统你可以使用如下代码：");
            Console.WriteLine();
            Console.WriteLine(@"@echo Off
For /F %%a in (urls.txt) Do (BBDown.exe ""%%a"")
pause");
            Console.WriteLine();
            throw new Exception("暂不支持该功能");
        }
    }
}

using BBDown.Core.Entity;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using static BBDown.Core.Entity.Entity;
using static BBDown.Core.Logger;
using static BBDown.Core.Util.HTTPUtil;


namespace BBDown.Core.Fetcher
{
    /// <summary>
    /// 收藏夹解析
    /// https://space.bilibili.com/4743331/favlist?spm_id_from=333.1007.0.0
    /// 
    /// </summary>
    public class FavListFetcher : IFetcher
    {
        public async Task<VInfo> FetchAsync(string id)
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
            int index = 1;
            List<Page> pagesInfo = new List<Page>();

            var api = $"https://api.bilibili.com/x/v3/fav/resource/list?media_id={favId}&pn=1&ps={pageSize}&keyword=&order=mtime&type=0&tid=0&platform=web&jsonp=jsonp";
            var json = await GetWebSourceAsync(api);
            using var infoJson = JsonDocument.Parse(json);
            var data = infoJson.RootElement.GetProperty("data");
            int totalCount = data.GetProperty("info").GetProperty("media_count").GetInt32();
            int totalPage = (int)Math.Ceiling((double)totalCount / pageSize);
            var title = data.GetProperty("info").GetProperty("title").ToString();
            var intro = data.GetProperty("info").GetProperty("intro").GetString();
            string pubTime = data.GetProperty("info").GetProperty("ctime").ToString();
            pubTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc).AddSeconds(Convert.ToDouble(pubTime)).ToLocalTime().ToString();
            var userName = data.GetProperty("info").GetProperty("upper").GetProperty("name").ToString();
            var medias = data.GetProperty("medias").EnumerateArray().ToList();
            
            for (int page = 2; page <= totalPage; page++)
            {
                api = $"https://api.bilibili.com/x/v3/fav/resource/list?media_id={favId}&pn={page}&ps={pageSize}&keyword=&order=mtime&type=0&tid=0&platform=web&jsonp=jsonp";
                json = await GetWebSourceAsync(api);
                var jsonDoc = JsonDocument.Parse(json);
                data = jsonDoc.RootElement.GetProperty("data");
                medias.AddRange(data.GetProperty("medias").EnumerateArray().ToList());
            }

            foreach (var m in medias)
            {
                var pageCount = m.GetProperty("page").GetInt32();
                var tmpInfo = await new NormalInfoFetcher().FetchAsync(m.GetProperty("id").ToString());
                foreach (var item in tmpInfo.PagesInfo)
                {
                    Page p = new Page(index++, item);
                    if (!pagesInfo.Contains(p)) pagesInfo.Add(p);
                }
            }

            var info = new VInfo();
            // 收藏夹标题
            info.Title = title;
            info.PubTime = pubTime;
            info.PagesInfo = pagesInfo;
            info.IsBangumi = false;
            info.IsFavList = true;

            return info;
        }
    }
}

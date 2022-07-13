using BBDown.Core.Entity;
using System.Text.Json;
using static BBDown.Core.Entity.Entity;
using static BBDown.Core.Util.HTTPUtil;

namespace BBDown.Core.Fetcher
{
    /// <summary>
    /// 合集解析
    /// https://space.bilibili.com/23630128/channel/collectiondetail?sid=2045
    /// https://www.bilibili.com/medialist/play/23630128?from=space&business=space_collection&business_id=2045&desc=0
    /// </summary>
    public class MediaListFetcher : IFetcher
    {
        public async Task<VInfo> FetchAsync(string id)
        {
            id = id[10..];
            var api = $"https://api.bilibili.com/x/v1/medialist/info?type=8&biz_id={id}&tid=0";
            var json = await GetWebSourceAsync(api);
            using var infoJson = JsonDocument.Parse(json);
            var data = infoJson.RootElement.GetProperty("data");
            var listTitle = data.GetProperty("title").GetString()!;
            var intro = data.GetProperty("intro").GetString()!;
            string pubTime = data.GetProperty("ctime").ToString()!;
            pubTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc).AddSeconds(Convert.ToDouble(pubTime)).ToLocalTime().ToString();

            List<Page> pagesInfo = new();
            bool hasMore = true;
            var oid = "";
            int index = 1;
            while (hasMore)
            {
                var listApi = $"https://api.bilibili.com/x/v2/medialist/resource/list?type=8&oid={oid}&otype=2&biz_id={id}&bvid=&with_current=true&mobi_app=web&ps=20&direction=false&sort_field=1&tid=0&desc=false";
                json = await GetWebSourceAsync(listApi);
                using var listJson = JsonDocument.Parse(json);
                data = listJson.RootElement.GetProperty("data");
                hasMore = data.GetProperty("has_more").GetBoolean();
                foreach (var m in data.GetProperty("media_list").EnumerateArray())
                {
                    var pageCount = m.GetProperty("page").GetInt32();
                    var desc = m.GetProperty("intro").GetString()!;
                    var ownerName = m.GetProperty("upper").GetProperty("name").ToString();
                    var ownerMid = m.GetProperty("upper").GetProperty("mid").ToString();
                    foreach (var page in m.GetProperty("pages").EnumerateArray())
                    {
                        Page p = new(index++,
                        m.GetProperty("id").ToString(),
                        page.GetProperty("id").ToString(),
                        "", //epid
                        pageCount == 1 ? m.GetProperty("title").ToString() : $"{m.GetProperty("title")}_P{page.GetProperty("page")}_{page.GetProperty("title")}", //单P使用外层标题 多P则拼接内层子标题
                        page.GetProperty("duration").GetInt32(),
                        page.GetProperty("dimension").GetProperty("width").ToString() + "x" + page.GetProperty("dimension").GetProperty("height").ToString(),
                        m.GetProperty("cover").ToString(),
                        desc,
                        ownerName,
                        ownerMid);
                        if (!pagesInfo.Contains(p)) pagesInfo.Add(p);
                        else index--;
                    }
                    oid = m.GetProperty("id").ToString();
                }
            }

            var info = new VInfo
            {
                Title = listTitle.Trim(),
                Desc = intro.Trim(),
                Pic = "",
                PubTime = pubTime,
                PagesInfo = pagesInfo,
                IsBangumi = false
            };

            return info;
        }
    }
}

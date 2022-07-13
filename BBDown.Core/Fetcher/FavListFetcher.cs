using BBDown.Core.Entity;
using System.Text.Json;
using static BBDown.Core.Entity.Entity;
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
            id = id[6..];
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
            List<Page> pagesInfo = new();

            var api = $"https://api.bilibili.com/x/v3/fav/resource/list?media_id={favId}&pn=1&ps={pageSize}&keyword=&order=mtime&type=0&tid=0&platform=web&jsonp=jsonp";
            var json = await GetWebSourceAsync(api);
            using var infoJson = JsonDocument.Parse(json);
            var data = infoJson.RootElement.GetProperty("data");
            int totalCount = data.GetProperty("info").GetProperty("media_count").GetInt32();
            int totalPage = (int)Math.Ceiling((double)totalCount / pageSize);
            var title = data.GetProperty("info").GetProperty("title").GetString()!;
            var intro = data.GetProperty("info").GetProperty("intro").GetString()!;
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
                if (pageCount > 1)
                {
                    var tmpInfo = await new NormalInfoFetcher().FetchAsync(m.GetProperty("id").ToString());
                    foreach (var item in tmpInfo.PagesInfo)
                    {
                        Page p = new(index++, item)
                        {
                            title = m.GetProperty("title").ToString() + $"_P{item.index}_{item.title}",
                            cover = tmpInfo.Pic,
                            desc = m.GetProperty("intro").ToString()
                        };
                        if (!pagesInfo.Contains(p)) pagesInfo.Add(p);
                    }
                }
                else
                {
                    Page p = new(index++,
                        m.GetProperty("id").ToString(),
                        m.GetProperty("ugc").GetProperty("first_cid").ToString(),
                        "", //epid
                        m.GetProperty("title").ToString(),
                        m.GetProperty("duration").GetInt32(),
                        "",
                        m.GetProperty("cover").ToString(),
                        m.GetProperty("intro").ToString(),
                        m.GetProperty("upper").GetProperty("name").ToString(),
                        m.GetProperty("upper").GetProperty("mid").ToString());
                    if (!pagesInfo.Contains(p)) pagesInfo.Add(p);
                }
            }

            var info = new VInfo
            {
                Title = title.Trim(),
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

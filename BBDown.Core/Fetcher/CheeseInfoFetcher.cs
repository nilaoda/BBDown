using BBDown.Core.Entity;
using System.Text.Json;
using static BBDown.Core.Entity.Entity;
using static BBDown.Core.Util.HTTPUtil;

namespace BBDown.Core.Fetcher;

public class CheeseInfoFetcher : IFetcher
{
    public async Task<VInfo> FetchAsync(string id)
    {
        id = id[7..];
        string index = "";
        string api = $"https://api.bilibili.com/pugv/view/web/season?ep_id={id}";
        string json = await GetWebSourceAsync(api);
        using var infoJson = JsonDocument.Parse(json);
        var data = infoJson.RootElement.GetProperty("data");
        string cover = data.GetProperty("cover").ToString();
        string title = data.GetProperty("title").ToString();
        string desc = data.GetProperty("subtitle").ToString();
        string ownerName = data.GetProperty("up_info").GetProperty("uname").ToString();
        string ownerMid = data.GetProperty("up_info").GetProperty("mid").ToString();
        var pages = data.GetProperty("episodes").EnumerateArray();
        List<Page> pagesInfo = new();
        foreach (var page in pages)
        {
            Page p = new(page.GetProperty("index").GetInt32(),
                page.GetProperty("aid").ToString(),
                page.GetProperty("cid").ToString(),
                page.GetProperty("id").ToString(),
                page.GetProperty("title").ToString().Trim(),
                page.GetProperty("duration").GetInt32(),
                "",
                page.GetProperty("release_date").GetInt64(),
                "",
                "",
                ownerName,
                ownerMid);
            if (p.epid == id) index = p.index.ToString();
            pagesInfo.Add(p);
        }
        long pubTime = pagesInfo.Any() ? pagesInfo[0].pubTime : 0;

        var info = new VInfo
        {
            Title = title.Trim(),
            Desc = desc.Trim(),
            Pic = cover,
            PubTime = pubTime,
            PagesInfo = pagesInfo,
            IsBangumi = true,
            IsCheese = true,
            Index = index
        };

        return info;
    }
}
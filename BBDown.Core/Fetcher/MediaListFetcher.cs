using BBDown.Core.Entity;
using System.Text.Json;
using static BBDown.Core.Entity.Entity;
using static BBDown.Core.Util.HTTPUtil;

namespace BBDown.Core.Fetcher;

/// <summary>
/// 合集解析
/// https://space.bilibili.com/23630128/channel/collectiondetail?sid=2045
/// https://www.bilibili.com/medialist/play/23630128?business=space_collection&business_id=2045 (无法从该链接打开合集)
/// </summary>
public class MediaListFetcher : IFetcher
{
    public async Task<VInfo> FetchAsync(string id)
    {
        id = id[10..];
        var api = $"https://api.bilibili.com/x/v1/medialist/info?type=8&biz_id={id}&tid=0";
        var json = await GetWebSourceAsync(api);
        using var infoJson = JsonDocument.Parse(json);
        var root = infoJson.RootElement;
        var data = root.GetProperty("data");
        if (data.ValueKind != JsonValueKind.Object)
        {
            // 部分情况下（合集被删除、设为私密或无权访问）data 会是 null
            // 也有可能是“系列”却被误识别为合集，这里优先尝试按系列解析
            try
            {
                return await new SeriesListFetcher().FetchAsync($"seriesBizId:{id}");
            }
            catch
            {
                var code = root.TryGetProperty("code", out var codeElem) && codeElem.ValueKind == JsonValueKind.Number
                    ? codeElem.GetInt32()
                    : 0;
                var message = root.TryGetProperty("message", out var msgElem) && msgElem.ValueKind == JsonValueKind.String
                    ? msgElem.GetString()
                    : "未知错误";
                throw new Exception($"获取合集信息失败(code={code}): {message}");
            }
        }
        var listTitle = data.GetProperty("title").GetString()!;
        var intro = data.GetProperty("intro").GetString()!;
        long pubTime = data.GetProperty("ctime").GetInt64()!;

        List<Page> pagesInfo = new();
        bool hasMore = true;
        var oid = "";
        int index = 1;
        while (hasMore)
        {
            var listApi = $"https://api.bilibili.com/x/v2/medialist/resource/list?type=8&oid={oid}&otype=2&biz_id={id}&with_current=true&mobi_app=web&ps=20&direction=false&sort_field=1&tid=0&desc=false";
            json = await GetWebSourceAsync(listApi);
            using var listJson = JsonDocument.Parse(json);
            var listRoot = listJson.RootElement;
            data = listRoot.GetProperty("data");
            if (data.ValueKind != JsonValueKind.Object)
            {
                var code = listRoot.TryGetProperty("code", out var codeElem) && codeElem.ValueKind == JsonValueKind.Number
                    ? codeElem.GetInt32()
                    : 0;
                var message = listRoot.TryGetProperty("message", out var msgElem) && msgElem.ValueKind == JsonValueKind.String
                    ? msgElem.GetString()
                    : "未知错误";
                throw new Exception($"获取合集视频列表失败(code={code}): {message}");
            }
            hasMore = data.GetProperty("has_more").GetBoolean();
            foreach (var m in data.GetProperty("media_list").EnumerateArray())
            {
                // 只处理未失效的视频条目（与收藏夹解析逻辑保持一致）
                if (m.TryGetProperty("attr", out var attrElem) && attrElem.GetInt32() != 0)
                    continue;

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
                        m.GetProperty("pubtime").GetInt64(),
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
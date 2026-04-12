using BBDown.Core.Entity;
using System.Text.Json;
using static BBDown.Core.Entity.Entity;
using static BBDown.Core.Util.HTTPUtil;
using static BBDown.Core.Logger;

namespace BBDown.Core.Fetcher;

public class SpaceVideoFetcher : IFetcher
{
    public async Task<VInfo> FetchAsync(string id)
    {
        id = id[4..];
        // 使用直播API获取用户名（可绕过w_rid签名要求）
        string userInfoApi = $"https://api.live.bilibili.com/live_user/v1/Master/info?uid={id}";
        string userName = JsonDocument.Parse(await GetWebSourceAsync(userInfoApi)).RootElement
            .GetProperty("data").GetProperty("info").GetProperty("uname").ToString();

        Log($"正在获取用户 {userName} 的投稿视频列表...");

        // 分页收集所有投稿视频信息
        List<string> aidList = new();
        int pageSize = 50;
        int pageNumber = 1;
        int totalCount;

        do
        {
            var api = Parser.WbiSign($"mid={id}&order=pubdate&pn={pageNumber}&ps={pageSize}&tid=0&wts={DateTimeOffset.Now.ToUnixTimeSeconds()}");
            api = $"https://api.bilibili.com/x/space/wbi/arc/search?{api}";
            string json = await GetWebSourceAsync(api);
            using var infoJson = JsonDocument.Parse(json);
            var data = infoJson.RootElement.GetProperty("data");
            totalCount = data.GetProperty("page").GetProperty("count").GetInt32();

            foreach (var v in data.GetProperty("list").GetProperty("vlist").EnumerateArray())
            {
                aidList.Add(v.GetProperty("aid").ToString());
            }

            pageNumber++;
        } while (aidList.Count < totalCount);

        Log($"共找到 {aidList.Count} 个投稿视频，正在获取详细信息...");

        // 逐个获取视频详细信息（含cid、分P等）
        List<Page> pagesInfo = new();
        int index = 1;

        for (int i = 0; i < aidList.Count; i++)
        {
            var aid = aidList[i];
            Log($"正在解析视频信息 ({i + 1}/{aidList.Count}): av{aid}");

            try
            {
                var tmpInfo = await new NormalInfoFetcher().FetchAsync(aid);
                foreach (var item in tmpInfo.PagesInfo)
                {
                    Page p = new(index++, item);
                    if (tmpInfo.PagesInfo.Count > 1)
                    {
                        p.title = $"{tmpInfo.Title}_P{item.index}_{item.title}";
                    }
                    pagesInfo.Add(p);
                }
            }
            catch (Exception ex)
            {
                LogError($"获取视频 av{aid} 信息失败: {ex.Message}");
            }
        }

        Log($"共解析出 {pagesInfo.Count} 个视频（含分P）");

        var info = new VInfo
        {
            Title = $"{userName}的投稿视频",
            Desc = "",
            Pic = "",
            PubTime = 0,
            PagesInfo = pagesInfo,
            IsBangumi = false
        };

        return info;
    }
}

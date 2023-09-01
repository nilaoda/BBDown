using BBDown.Core.Entity;
using System.Text.Json;
using System.Text.RegularExpressions;
using static BBDown.Core.Entity.Entity;
using static BBDown.Core.Util.HTTPUtil;

namespace BBDown.Core.Fetcher
{
    public partial class NormalInfoFetcher : IFetcher
    {
        public async Task<VInfo> FetchAsync(string id)
        {
            string api = $"https://api.bilibili.com/x/web-interface/view?aid={id}";
            string json = await GetWebSourceAsync(api);
            using var infoJson = JsonDocument.Parse(json);
            var data = infoJson.RootElement.GetProperty("data");
            string title = data.GetProperty("title").ToString();
            string desc = data.GetProperty("desc").ToString();
            string pic = data.GetProperty("pic").ToString();
            var owner = data.GetProperty("owner");
            string ownerMid = owner.GetProperty("mid").ToString();
            string ownerName = owner.GetProperty("name").ToString();
            long pubTime = data.GetProperty("pubdate").GetInt64();
            bool bangumi = false;

            var pages = data.GetProperty("pages").EnumerateArray().ToList();
            List<Page> pagesInfo = new();
            foreach (var page in pages)
            {
                Page p = new(page.GetProperty("page").GetInt32(),
                    id,
                    page.GetProperty("cid").ToString(),
                    "", //epid
                    page.GetProperty("part").ToString().Trim(),
                    page.GetProperty("duration").GetInt32(),
                    page.GetProperty("dimension").GetProperty("width").ToString() + "x" + page.GetProperty("dimension").GetProperty("height").ToString(),
                    pubTime, //分p视频没有发布时间
                    "",
                    "",
                    ownerName,
                    ownerMid
                    );
                pagesInfo.Add(p);
            }

            try
            {
                if (data.GetProperty("redirect_url").ToString().Contains("bangumi"))
                {
                    bangumi = true;
                    string epId = EpIdRegex().Match(data.GetProperty("redirect_url").ToString()).Groups[1].Value;
                    //番剧内容通常不会有分P，如果有分P则不需要epId参数
                    if (pages.Count == 1)
                    {
                        pagesInfo.ForEach(p => p.epid = epId);
                    }
                }
            }
            catch { }

            var info = new VInfo
            {
                Title = title.Trim(),
                Desc = desc.Trim(),
                Pic = pic,
                PubTime = pubTime,
                PagesInfo = pagesInfo,
                IsBangumi = bangumi
            };

            return info;
        }

        [GeneratedRegex("ep(\\d+)")]
        private static partial Regex EpIdRegex();
    }
}

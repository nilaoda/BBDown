using BBDown.Core.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using static BBDown.Core.Entity.Entity;
using static BBDown.Core.Util.HTTPUtil;

namespace BBDown.Core.Fetcher
{
    public class BangumiInfoFetcher : IFetcher
    {
        public async Task<VInfo> FetchAsync(string id)
        {
            id = id.Substring(3);
            string index = "";
            string api = $"https://api.bilibili.com/pgc/view/web/season?ep_id={id}";
            string json = await GetWebSourceAsync(api);
            using var infoJson = JsonDocument.Parse(json);
            var result = infoJson.RootElement.GetProperty("result");
            string cover = result.GetProperty("cover").ToString();
            string title = result.GetProperty("title").ToString();
            string desc = result.GetProperty("evaluate").ToString();
            string pubTime = result.GetProperty("publish").GetProperty("pub_time").ToString();
            var pages = result.GetProperty("episodes").EnumerateArray().ToList();
            List<Page> pagesInfo = new List<Page>();
            int i = 1;

            //episodes为空; 或者未包含对应epid，番外/花絮什么的
            if (pages.Count == 0 || !result.GetProperty("episodes").ToString().Contains($"/ep{id}")) 
            {
                JsonElement sections;
                if (result.TryGetProperty("section", out sections))
                {
                    foreach (var section in sections.EnumerateArray())
                    {
                        if (section.ToString().Contains($"/ep{id}"))
                        {
                            title += "[" + section.GetProperty("title").ToString() + "]";
                            pages = section.GetProperty("episodes").EnumerateArray().ToList();
                            break;
                        }
                    }
                }
            }

            foreach (var page in pages)
            {
                //跳过预告
                JsonElement badge;
                if (page.TryGetProperty("badge", out badge) && badge.ToString() == "预告") continue;
                string res = "";
                try
                {
                    res = page.GetProperty("dimension").GetProperty("width").ToString() + "x" + page.GetProperty("dimension").GetProperty("height").ToString();
                }
                catch (Exception) { }
                string _title = page.GetProperty("title").ToString() + " " + page.GetProperty("long_title").ToString();
                _title = _title.Trim();
                Page p = new Page(i++,
                    page.GetProperty("aid").ToString(),
                    page.GetProperty("cid").ToString(),
                    page.GetProperty("id").ToString(),
                    _title,
                    0, res);
                if (p.epid == id) index = p.index.ToString();
                pagesInfo.Add(p);
            }


            var info = new VInfo();
            info.Title = title.Trim();
            info.Desc = desc.Trim();
            info.Pic = cover;
            info.PubTime = pubTime;
            info.PagesInfo = pagesInfo;
            info.IsBangumi = true;
            info.IsCheese = true;
            info.Index = index;

            return info;
        }
    }
}

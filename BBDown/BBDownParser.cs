using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using static BBDown.BBDownUtil;

namespace BBDown
{
    class BBDownParser
    {
        public static string GetPlayJson(string aid, string cid, string epId, bool tvApi, bool bangumi, string qn = "0")
        {
            string prefix = tvApi ? (bangumi ? "api.snm0516.aisee.tv/pgc/player/api/playurltv" : "api.snm0516.aisee.tv/x/tv/ugc/playurl")
                        : (bangumi ? "api.bilibili.com/pgc/player/web/playurl" : "api.bilibili.com/x/player/playurl");
            string api = $"https://{prefix}?avid={aid}&cid={cid}&qn={qn}&type=&otype=json" + (tvApi ? "" : "&fourk=1") +
                "&fnver=0&fnval=16" + (tvApi ? "&device=android&platform=android&mobi_app=android_tv_yst&npcybs=0&force_host=0&build=102801" : "") +
                (bangumi ? $"&module=bangumi&ep_id={epId}&fourk=1" + "&session=" : "");
            //Console.WriteLine(api);
            string webJson = GetWebSource(api);
            //以下情况从网页源代码尝试解析
            if (webJson.Contains("\"大会员专享限制\""))
            {
                string webUrl = "https://www.bilibili.com/bangumi/play/ep" + epId;
                string webSource = GetWebSource(webUrl);
                webJson = Regex.Match(webSource, @"window.__playinfo__=([\s\S]*?)<\/script>").Groups[1].Value;
            }
            return webJson;
        }
    }
}

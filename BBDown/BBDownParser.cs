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
                "&fnver=0&fnval=16" + (tvApi ? "&device=android&platform=android" +
                "&mobi_app=android_tv_yst&npcybs=0&force_host=0&build=102801" +
                (Program.TOKEN != "" ? $"&access_key={GetQueryString("access_token", Program.TOKEN)}" : "") : "") +
                (bangumi ? $"&module=bangumi&ep_id={epId}&fourk=1" + "&session=" : "");
            if (tvApi && bangumi)
            {
                api = (Program.TOKEN != "" ? $"access_key={GetQueryString("access_token", Program.TOKEN)}&" : "") +
                    $"aid={aid}&appkey=4409e2ce8ffd12b8&build=102801" +
                    $"&cid={cid}&device=android&ep_id={epId}&expire=0" +
                    $"&fnval=16&fnver=0&fourk=1" +
                    $"&mid=0&mobi_app=android_tv_yst" +
                    $"&module=bangumi&npcybs=0&otype=json&platform=android" +
                    $"&qn={qn}&ts={GetTimeStamp(true)}";
                api = $"https://{prefix}?" + api + (bangumi ? $"&sign={GetSign(api)}" : "");
            }
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

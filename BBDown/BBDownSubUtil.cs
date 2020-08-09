using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using static BBDown.BBDownUtil;

namespace BBDown
{
    class BBDownSubUtil
    {
        public static void SaveSubtitle(string url, string path)
        {
            File.WriteAllText(path, ConvertSubFromJson(GetWebSource(url)), new UTF8Encoding());
        }

        private static string ConvertSubFromJson(string jsonString)
        {
            StringBuilder lines = new StringBuilder();
            JObject json = JObject.Parse(jsonString);
            JArray sub = JArray.Parse(json["body"].ToString());
            for(int i = 0; i < sub.Count; i++)
            {
                lines.AppendLine((i + 1).ToString());
                lines.AppendLine($"{FormatTime(sub[i]["from"].ToString())} --> {FormatTime(sub[i]["to"].ToString())}");
                lines.AppendLine(sub[i]["content"].ToString());
                lines.AppendLine();
            }
            return lines.ToString();
        }

        private static string FormatTime(string sec) //64.13
        {
            string[] v = { sec, "" };
            if (sec.Contains("."))
                v = sec.Split('.');
            v[1] = v[1].PadRight(3, '0').Substring(0, 3);
            int secs = Convert.ToInt32(v[0]);
            TimeSpan ts = new TimeSpan(0, 0, secs);
            string str = "";
            str = ts.Hours.ToString("00") + ":" + ts.Minutes.ToString("00") + ":" + ts.Seconds.ToString("00") + "," + v[1];
            return str;
        }

        public static Dictionary<string, string> SubDescDic = new Dictionary<string, string>
        {
            {"ar", "العربية"}, {"ar-eg", "العربية"},
            {"bg", "български"}, {"cmn-hans", "国语 (简体)"},
            {"cmn-hant", "國語 (繁體)"}, {"cs", "čeština"},
            {"da", "Dansk"}, {"da-dk", "Dansk"},
            {"de", "Deutsch"}, {"de-de", "Deutsch"},
            {"el", "Ελληνικά"}, {"en", "English"},
            {"en-US", "English"}, {"es", "Español (Latinoamérica)"},
            {"es-419", "Español (Latinoamérica)"}, {"es-es", "Español (España)"},
            {"es-ES", "Español (España)"}, {"fi", "Suomi"},
            {"fi-fi", "Suomi"}, {"fr", "Français"},
            {"fr-fr", "Français"}, {"he", "עברית"},
            {"he-il", "עברית"}, {"hi", "हिन्दी"},
            {"hi-in", "हिन्दी"}, {"hr", "Hrvatska"},
            {"id", "Indonesia"}, {"id-id", "Indonesia"},
            {"it", "Italiano"}, {"it-it", "Italiano"},
            {"ja", "日本語"}, {"ja-ja", "日本語"},
            {"jp", "日本語"}, {"jp-jp", "日本語"},
            {"ko", "한국어"}, {"ko-kr", "한국어"},
            {"ms", "Melayu"}, {"nb", "Norsk Bokmål"},
            {"nb-no", "Norsk Bokmål"}, {"nl", "Nederlands"},
            {"nl-BE", "Nederlands"}, {"nl-be", "Nederlands"},
            {"nl-nl", "Nederlands"}, {"nob", "norsk"},
            {"pl", "Polski"}, {"pl-pl", "Polski"},
            {"pt", "Português"}, {"pt-BR", "Português"},
            {"pt-br", "Português"}, {"ro", "Română"},
            {"ru", "Русский"}, {"ru-ru", "Русский"},
            {"sk", "slovenský"}, {"sv", "Svenska"},
            {"sv-se", "Svenska"}, {"ta-in", "தமிழ்"},
            {"te-in", "తెలుగు"}, {"th", "ไทย"},
            {"tl", "Tagalog"}, {"tr", "Türkçe"},
            {"tr-tr", "Türkçe"}, {"uk", "Українська"},
            {"vi", "Tiếng Việt"}, {"zxx", "zxx"},
            {"zh-hans", "中文（简体）"},
            {"zh-Hans", "中文（简体）"},
            {"zh-CN", "中文（简体）"},
            {"zh-TW", "中文（繁體）"},
            {"zh-HK", "中文（繁體）"},
            {"zh-MO", "中文（繁體）"},
            {"zh-Hant", "中文（繁體）"},
            {"zh-hant", "中文（繁體）"},
            {"yue", "中文（粤语）"},
            {"hu", "Magyar"},
            {"et", "Eestlane"}, {"bn", "বাংলা ভাষার"},
            {"iw", "שפה עברית"}, {"sr", "српски језик"},
            {"hy", "հայերեն"}, {"az", "Azərbaycan"},
            {"kk", "Қазақ тілі"}, {"is", "icelandic"},
            {"fil", "Pilipino"}, {"ku", "Kurdî"},
            {"ca", "català"}, {"no", "norsk språk"}
        };

        public static Dictionary<string, string> SubLangDic = new Dictionary<string, string> 
        {
            {"ar","ara"}, {"ar-eg","ara"},
            {"bg","bul"}, {"cmn-hans","chi"},
            {"cmn-hant","chi"}, {"cs","cze"},
            {"da","dan"}, {"da-dk","dan"},
            {"de","ger"}, {"de-de","ger"},
            {"el","gre"}, {"en","eng"},
            {"en-US","eng"}, {"es","spa"},
            {"es-419","spa"}, {"es-ES","spa"},
            {"es-es","spa"}, {"fi","fin"},
            {"fi-fi","fin"}, {"fr","fre"},
            {"fr-fr","fre"}, {"he","heb"},
            {"he-il","heb"}, {"hi","hin"},
            {"hi-in","hin"}, {"hr","hrv"},
            {"id","ind"}, {"id-id","ind"},
            {"it","ita"}, {"it-it","ita"},
            {"ja","jpn"}, {"ja-ja","jpn"},
            {"jp","jpn"}, {"jp-jp","jpn"},
            {"ko","kor"}, {"ko-kr","kor"},
            {"ms","may"}, {"nb","nor"},
            {"nb-no","nor"}, {"nl","dut"},
            {"nl-BE","dut"}, {"nl-be","dut"},
            {"nl-nl","dut"}, {"nob","nor"},
            {"pl","pol"}, {"pl-pl","pol"},
            {"pt","por"}, {"pt-BR","por"},
            {"pt-br","por"}, {"ro","rum"},
            {"ru","rus"}, {"ru-ru","rus"},
            {"sk","slo"}, {"sv","swe"},
            {"sv-se","swe"}, {"ta-in","tam"},
            {"te-in","tel"}, {"th","tha"},
            {"tl","tgl"}, {"tr","tur"},
            {"tr-tr","tur"}, {"uk","ukr"},
            {"vi","vie"}, {"zh-hans","chi"},
            {"zh-Hans","chi"}, {"zh-Hant","chi"},
            {"zh-hant","chi"}, {"zh-CN","chi"},
            {"zh-TW","chi"}, {"zh-HK","chi"},
            {"zh-MO","chi"}, {"zh-CHS","chi"},
            {"zh-CHT","chi"}, {"zh-SG","chi"},
            {"et", "est"}, {"bn", "ben"},
            {"iw", "heb"}, {"sr", "srp"},
            {"hy", "arm"}, {"az", "aze"},
            {"kk", "kaz"}, {"is", "ice"},
            {"fil", "phi"}, {"ku", "kur"},
            {"ca", "cat"}, {"no", "nor"},
            {"hu", "hun"}
        };
    }
}

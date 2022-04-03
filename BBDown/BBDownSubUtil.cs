using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using static BBDown.BBDownEntity;
using static BBDown.BBDownUtil;
using static BBDown.BBDownLogger;
using System.Text.RegularExpressions;
using System.Text.Json;
using System.Linq;
using System.Threading.Tasks;

namespace BBDown
{
    class BBDownSubUtil
    {
        //https://i0.hdslb.com/bfs/subtitle/subtitle_lan.json
        public static (string, string) GetSubtitleCode(string key)
        {
            //zh-hans => zh-Hans
            if (Regex.IsMatch(key, "-[a-z]"))
            {
                var v = Regex.Match(key, "-[a-z]").Value;
                key = key.Replace(v, v.ToUpper());
            }

            return key switch
            {
                "zh-CN"             => ("chi", "中文（简体）"),
                "zh-HK"             => ("chi", "中文（香港繁體）"),
                "zh-Hans"           => ("chi", "中文（简体）"),
                "zh-TW"             => ("chi", "中文（台灣繁體）"),
                "zh-Hant"           => ("chi", "中文（繁體）"),
                "en-US"             => ("eng", "English(USA)"),
                "ja"                => ("jpn", "日本語"),
                "ko"                => ("kor", "한국어"),
                "zh-SG"             => ("chi", "中文（新加坡）"),
                "ab"                => ("abk", "Аҳәынҭқарра"),
                "aa"                => ("aar", "Qafár af"),
                "af"                => ("afr", "Afrikaans"),
                "sq"                => ("alb", "Gjuha shqipe"),
                "ase"               => ("ase", "American Sign Language"),
                "am"                => ("amh", "አማርኛ"),
                "arc"               => ("arc", "ܐܪܡܝܐ"),
                "hy"                => ("arm", "հայերեն"),
                "as"                => ("asm", "অসমীয়া"),
                "ay"                => ("aym", "Aymar aru"),
                "az"                => ("aze", "Azərbaycan"),
                "bn"                => ("ben", "বাংলা ভাষার"),
                "ba"                => ("bak", "Башҡорттеле"),
                "eu"                => ("baq", "Euskara"),
                "be"                => ("bel", "беларуская мова biełaruskaja mova"),
                "bh"                => ("bih", "Bihar"),
                "bi"                => ("bis", "Bislama"),
                "bs"                => ("bos", "босански"),
                "br"                => ("bre", "Breton"),
                "bg"                => ("bul", "български"),
                "yue"               => ("chi", "粵語"),
                "yue-HK"            => ("chi", "粵語（中國香港）"),
                "ca"                => ("cat", "català"),
                "chr"               => ("chr", "ᏣᎳᎩ ᎦᏬᏂᎯᏍᏗ"),
                "cho"               => ("cho", "Chahta'"),
                "co"                => ("cos", "lingua corsa"),
                "hr"                => ("hrv", "Hrvatska"),
                "cs"                => ("cze", "čeština"),
                "da"                => ("dan", "Dansk"),
                "nl"                => ("dut", "Nederlands"),
                "nl-BE"             => ("dut", "Nederlands(Belgisch)"),
                "nl-NL"             => ("dut", "Nederlands(Nederlands)"),
                "dz"                => ("dzo", "རྫོང་ཁ།"),
                "en"                => ("eng", "English"),
                "en-CA"             => ("eng", "English(Canada)"),
                "en-IE"             => ("eng", "English(Ireland)"),
                "en-GB"             => ("eng", "English(UK)"),
                "eo"                => ("epo", "Esperanto"),
                "et"                => ("est", "Eestlane"),
                "fo"                => ("fao", "føroyskt"),
                "fj"                => ("fij", "Vakaviti"),
                "fil"               => ("phi", "Pilipino"),
                "fi"                => ("fin", "Suomi"),
                "fr"                => ("fre", "Français"),
                "fr-BE"             => ("fre", "Français(Belgique)"),
                "fr-CA"             => ("fre", "Français(Canada)"),
                "fr-FR"             => ("fre", "Français(La France)"),
                "fr-CH"             => ("fre", "Français(Suisse)"),
                "ff"                => ("ful", "Fulani"),
                "gl"                => ("glg", "galego"),
                "ka"                => ("geo", "ქართული ენა"),
                "de"                => ("ger", "Deutsch"),
                "de-AT"             => ("ger", "Deutsch(Österreich)"),
                "de-DE"             => ("ger", "Deutsch(Deutschland)"),
                "de-CH"             => ("ger", "Deutsch(Schweiz)"),
                "el"                => ("gre", "Ελληνικά"),
                "kl"                => ("kal", "Kalaallisut"),
                "gn"                => ("grn", "avañe'ẽ"),
                "gu"                => ("guj", "ગુજરાતી"),
                "hak"               => ("hak", "Hak-kâ-fa"),
                "hak-TW"            => ("hak", "Hak-kâ-fa"),
                "ha"                => ("hau", "هَوُسَ"),
                "iw"                => ("heb", "שפה עברית"),
                "hi"                => ("hin", "हिन्दी"),
                "hi-Latn"           => ("hin", "हिंदी(फोनेटिक)"),
                "hu"                => ("hun", "Magyar"),
                "is"                => ("ice", "icelandic"),
                "ig"                => ("ibo", "Asụsụ Igbo"),
                "id"                => ("ind", "Indonesia"),
                "ia"                => ("ina", "Interlingua"),
                "iu"                => ("iku", "ᐃᓄᒃᑎᑐᑦ"),
                "ik"                => ("ipk", "Inupiat"),
                "ga"                => ("gle", "Gaeilge na hÉireann"),
                "it"                => ("ita", "Italiano"),
                "jv"                => ("jav", "ꦧꦱꦗꦮ"),
                "kn"                => ("kan", "ಕನ್ನಡ"),
                "ks"                => ("kas", "कॉशुर"),
                "kk"                => ("kaz", "Қазақ тілі"),
                "km"                => ("khm", "ភាសាខ្មែរ"),
                "rw"                => ("kin", "Ikinyarwanda"),
                "tlh"               => ("tlh", "tlhIngan Hol"),
                "ku"                => ("kur", "Kurdî"),
                "ky"                => ("kir", "кыргыз тили"),
                "lo"                => ("lao", "ພາສາລາວ"),
                "la"                => ("lat", "latīna"),
                "lv"                => ("lav", "latviešu valoda"),
                "ln"                => ("lin", "Lingála"),
                "lt"                => ("lit", "lietuvių kalba"),
                "lb"                => ("ltz", "Lëtzebuergesch"),
                "mk"                => ("mac", "Македонски јазик"),
                "mg"                => ("mlg", "maa.laa.gaas"),
                "ms"                => ("may", "Melayu"),
                "ml"                => ("mal", "മലയാളം"),
                "mt"                => ("mlt", "Lingwa Maltija"),
                "mi"                => ("mao", "Māori"),
                "mr"                => ("mar", "मराठी Marāṭhī"),
                "mas"               => ("mas", "Maasai"),
                "nan"               => ("nan", "閩南語"),
                "nan-TW"            => ("nan", "閩南語(台灣)"),
                "lus"               => ("lus", "Mizo ṭawng"),
                "mo"                => ("mol", "Limba moldovenească"),
                "mn"                => ("mon", "монгол хэл"),
                "my"                => ("bur", "မြန်မာဘာသာ"),
                "na"                => ("nau", "Dorerin Naoero"),
                "nv"                => ("nav", "Diné bizaad"),
                "ne"                => ("nep", "नेपाली Nepālī"),
                "no"                => ("nor", "norsk språk"),
                "fa"                => ("per", "فارسی‎"),
                "fa-AF"             => ("per", "فارسی"),
                "fa-IR"             => ("per", "فارسی"),
                "pl"                => ("pol", "Polski"),
                "pt"                => ("por", "Português"),
                "pt-BR"             => ("por", "Português(brasil)"),
                "pt-PT"             => ("por", "Português(portugal)"),
                "ro"                => ("rum", "Română"),
                "ru"                => ("rus", "Русский"),
                "ru-Latn"           => ("rus", "Русский(фонетический)"),
                "sr"                => ("srp", "Српски"),
                "sr-Cyrl"           => ("srp", "Српски(ћирилица)"),
                "sr-Latn"           => ("srp", "Српски(латиница)"),
                "sh"                => ("scr", "srpskohrvatski"),
                "sk"                => ("slo", "slovenský"),
                "es"                => ("spa", "Español"),
                "es-419"            => ("spa", "Español(Latinoamérica)"),
                "es-MX"             => ("spa", "Español(México)"),
                "es-ES"             => ("spa", "Español(España)"),
                "es-US"             => ("spa", "Español(Estados Unidos)"),
                "sv"                => ("swe", "Svenska"),
                "tl"                => ("tgl", "Tagalog"),
                "th"                => ("tha", "ไทย"),
                "tr"                => ("tur", "Türkçe"),
                "uk"                => ("ukr", "Українська"),
                "ur"                => ("urd", "Urdu"),
                "vi"                => ("vie", "Tiếng Việt"),
                //太多了，我蚌埠住了，后面懒得查
                //"ie"                => ("", ""),
                //"oc"                => ("",   ""),
                //"or"                => ("",   ""),
                //"om"                => ("",   ""),
                //"ps"                => ("",   ""),
                //"pa"                => ("",   ""),
                //"qu"                => ("",   ""),
                //"rm"                => ("",   ""),
                //"rn"                => ("",   ""),
                //"sm"                => ("",   ""),
                //"sg"                => ("",   ""),
                //"sa"                => ("",   ""),
                //"gd"                => ("",   ""),
                //"sdp"               => ("",   ""),
                //"sn"                => ("",   ""),
                //"scn"               => ("",   ""),
                //"sd"                => ("",   ""),
                //"si"                => ("",   ""),
                //"sl"                => ("",   ""),
                //"so"                => ("",   ""),
                //"st"                => ("",   ""),
                //"su"                => ("",   ""),
                //"sw"                => ("",   ""),
                //"ss"                => ("",   ""),
                //"tg"                => ("",   ""),
                //"ta"                => ("",   ""),
                //"tt"                => ("",   ""),
                //"te"                => ("",   ""),
                //"ti"                => ("",   ""),
                //"to"                => ("",   ""),
                //"ts"                => ("",   ""),
                //"tn"                => ("",   ""),
                //"tk"                => ("",   ""),
                //"tw"                => ("",   ""),
                //"uz"                => ("",   ""),
                //"vo"                => ("",   ""),
                //"cy"                => ("",   ""),
                //"fy"                => ("",   ""),
                //"wo"                => ("",   ""),
                //"xh"                => ("",   ""),
                //"yi"                => ("",   ""),
                //"yo"                => ("",   ""),
                //"zu"                => ("",   ""),
                _ => ("und", "Undetermined")
            };
        }

        public static async Task<List<Subtitle>> GetSubtitlesAsync(string aid, string cid, string epId, bool intl)
        {
            List<Subtitle> subtitles = new List<Subtitle>();
            if (intl)
            {
                try
                {
                    string api = $"https://api.bilibili.tv/intl/gateway/web/v2/subtitle?&episode_id={epId}";
                    string json = await GetWebSourceAsync(api);
                    using var infoJson = JsonDocument.Parse(json);
                    var subs = infoJson.RootElement.GetProperty("data").GetProperty("subtitles").EnumerateArray();
                    foreach (var sub in subs)
                    {
                        Subtitle subtitle = new Subtitle();
                        subtitle.url = sub.GetProperty("url").ToString();
                        subtitle.lan = sub.GetProperty("lang_key").ToString();
                        subtitle.path = $"{aid}/{aid}.{cid}.{subtitle.lan}{(subtitle.url.Contains(".json") ? ".srt" : ".ass")}";
                        subtitles.Add(subtitle);
                    }
                    return subtitles;
                }
                catch (Exception) { return subtitles; } //返回空列表
            }

            try
            {
                string api = $"https://api.bilibili.com/x/web-interface/view?aid={aid}&cid={cid}";
                string json = await GetWebSourceAsync(api);
                using var infoJson = JsonDocument.Parse(json);
                var subs = infoJson.RootElement.GetProperty("data").GetProperty("subtitle").GetProperty("list").EnumerateArray();
                foreach (var sub in subs)
                {
                    Subtitle subtitle = new Subtitle();
                    subtitle.url = sub.GetProperty("subtitle_url").ToString();
                    subtitle.lan = sub.GetProperty("lan").ToString();
                    subtitle.path = $"{aid}/{aid}.{cid}.{subtitle.lan}.srt";
                    subtitles.Add(subtitle);
                }
                //无字幕片源 但是字幕没上导致的空列表，尝试从国际接口获取
                if (subtitles.Count == 0)
                {
                    return await GetSubtitlesAsync(aid, cid, epId, true);
                }
                return subtitles;
            }
            catch (Exception)
            {
                try
                {
                    //grpc调用接口 protobuf
                    string api = "https://app.biliapi.net/bilibili.community.service.dm.v1.DM/DmView";
                    int _aid = Convert.ToInt32(aid);
                    int _cid = Convert.ToInt32(cid);
                    int _type = 1;
                    byte[] data = new byte[18];
                    data[0] = 0x0; data[1] = 0x0; data[2] = 0x0; data[3] = 0x0; data[4] = 0xD; //先固定死了
                    int i = 5;
                    data[i++] = Convert.ToByte(1 << 3 | 0); // index=1
                    while ((_aid & -128) != 0)
                    {
                        data[i++] = Convert.ToByte((_aid & 127) | 128);
                        _aid = _aid >> 7;
                    }
                    data[i++] = Convert.ToByte(_aid);
                    data[i++] = Convert.ToByte(2 << 3 | 0); // index=2
                    while ((_cid & -128) != 0)
                    {
                        data[i++] = Convert.ToByte((_cid & 127) | 128);
                        _cid = _cid >> 7;
                    }
                    data[i++] = Convert.ToByte(_cid);
                    data[i++] = Convert.ToByte(3 << 3 | 0); // index=3
                    data[i++] = Convert.ToByte(_type);
                    string t = await GetPostResponseAsync(api, data);
                    Regex reg = new Regex("(zh-Han[st]).*?(http.*?\\.json)");
                    foreach (Match m in reg.Matches(t))
                    {
                        Subtitle subtitle = new Subtitle();
                        subtitle.url = m.Groups[2].Value;
                        subtitle.lan = m.Groups[1].Value;
                        subtitle.path = $"{aid}/{aid}.{cid}.{subtitle.lan}.srt";
                        subtitles.Add(subtitle);
                    }
                    return subtitles;
                }
                catch (Exception) { return subtitles; } //返回空列表
            }
        }

        public static async Task SaveSubtitleAsync(string url, string path)
        {
            if (path.EndsWith(".srt"))
                await File.WriteAllTextAsync(path, ConvertSubFromJson(await GetWebSourceAsync(url)), new UTF8Encoding());
            else
                await File.WriteAllTextAsync(path, await GetWebSourceAsync(url), new UTF8Encoding());
        }

        private static string ConvertSubFromJson(string jsonString)
        {
            StringBuilder lines = new StringBuilder();
            var json = JsonDocument.Parse(jsonString);
            var sub = json.RootElement.GetProperty("body").EnumerateArray().ToList();
            for(int i = 0; i < sub.Count; i++)
            {
                var line = sub[i];
                lines.AppendLine((i + 1).ToString());
                JsonElement from;
                JsonElement content;
                if (line.TryGetProperty("from", out from)) 
                {
                    lines.AppendLine($"{FormatTime(from.ToString())} --> {FormatTime(line.GetProperty("to").ToString())}");
                }
                else
                {
                    lines.AppendLine($"{FormatTime("0")} --> {FormatTime(line.GetProperty("to").ToString())}");
                }
                //有的没有内容
                if (line.TryGetProperty("content", out content))
                    lines.AppendLine(content.ToString());
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
    }
}

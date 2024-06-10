using QRCoder;
using System;
using System.IO;
using System.Threading.Tasks;
using static BBDown.BBDownUtil;
using static BBDown.Core.Logger;
using System.Text;
using System.Text.Json;
using System.Net.Http;
using BBDown.Core.Util;

namespace BBDown
{
    internal class BBDownLoginUtil
    {
        public static async Task<string> GetLoginStatusAsync(string qrcodeKey)
        {
            string queryUrl = $"https://passport.bilibili.com/x/passport-login/web/qrcode/poll?qrcode_key={qrcodeKey}&source=main-fe-header";
            return await HTTPUtil.GetWebSourceAsync(queryUrl);
        }

        public static async Task LoginWEB()
        {
            try
            {
                Log("获取登录地址...");
                string loginUrl = "https://passport.bilibili.com/x/passport-login/web/qrcode/generate?source=main-fe-header";
                string url = JsonDocument.Parse(await HTTPUtil.GetWebSourceAsync(loginUrl)).RootElement.GetProperty("data").GetProperty("url").ToString();
                string qrcodeKey = GetQueryString("qrcode_key", url);
                //Log(oauthKey);
                //Log(url);
                bool flag = false;
                Log("生成二维码...");
                QRCodeGenerator qrGenerator = new();
                QRCodeData qrCodeData = qrGenerator.CreateQrCode(url, QRCodeGenerator.ECCLevel.Q);
                PngByteQRCode pngByteCode = new(qrCodeData);
                File.WriteAllBytes("qrcode.png", pngByteCode.GetGraphic(7));
                Log("生成二维码成功: qrcode.png, 请打开并扫描, 或扫描打印的二维码");
                var consoleQRCode = new ConsoleQRCode(qrCodeData);
                consoleQRCode.GetGraphic();

                while (true)
                {
                    await Task.Delay(1000);
                    string w = await GetLoginStatusAsync(qrcodeKey);
                    int code = JsonDocument.Parse(w).RootElement.GetProperty("data").GetProperty("code").GetInt32();
                    if (code == 86038)
                    {
                        LogColor("二维码已过期, 请重新执行登录指令.");
                        break;
                    }
                    else if (code == 86101) //等待扫码
                    {
                        continue;
                    }
                    else if (code == 86090) //等待确认
                    {
                        if (!flag)
                        {
                            Log("扫码成功, 请确认...");
                            flag = !flag;
                        }
                    }
                    else
                    {
                        string cc = JsonDocument.Parse(w).RootElement.GetProperty("data").GetProperty("url").ToString();
                        Log("登录成功: SESSDATA=" + GetQueryString("SESSDATA", cc));
                        //导出cookie, 转义英文逗号 否则部分场景会出问题
                        File.WriteAllText(Path.Combine(Program.APP_DIR, "BBDown.data"), cc[(cc.IndexOf('?') + 1)..].Replace("&", ";").Replace(",", "%2C"));
                        File.Delete("qrcode.png");
                        break;
                    }
                }
            }
            catch (Exception e) { LogError(e.Message); }
        }

        public static async Task LoginTV()
        {
            try
            {
                string loginUrl = "https://passport.snm0516.aisee.tv/x/passport-tv-login/qrcode/auth_code";
                string pollUrl = "https://passport.bilibili.com/x/passport-tv-login/qrcode/poll";
                var parms = GetTVLoginParms();
                Log("获取登录地址...");
                byte[] responseArray = await (await HTTPUtil.AppHttpClient.PostAsync(loginUrl, new FormUrlEncodedContent(parms.ToDictionary()))).Content.ReadAsByteArrayAsync();
                string web = Encoding.UTF8.GetString(responseArray);
                string url = JsonDocument.Parse(web).RootElement.GetProperty("data").GetProperty("url").ToString();
                string authCode = JsonDocument.Parse(web).RootElement.GetProperty("data").GetProperty("auth_code").ToString();
                Log("生成二维码...");
                QRCodeGenerator qrGenerator = new();
                QRCodeData qrCodeData = qrGenerator.CreateQrCode(url, QRCodeGenerator.ECCLevel.Q);
                PngByteQRCode pngByteCode = new(qrCodeData);
                File.WriteAllBytes("qrcode.png", pngByteCode.GetGraphic(7));
                Log("生成二维码成功: qrcode.png, 请打开并扫描, 或扫描打印的二维码");
                var consoleQRCode = new ConsoleQRCode(qrCodeData);
                consoleQRCode.GetGraphic();
                parms.Set("auth_code", authCode);
                parms.Set("ts", GetTimeStamp(true));
                parms.Remove("sign");
                parms.Add("sign", GetSign(ToQueryString(parms)));
                while (true)
                {
                    await Task.Delay(1000);
                    responseArray = await (await HTTPUtil.AppHttpClient.PostAsync(pollUrl, new FormUrlEncodedContent(parms.ToDictionary()))).Content.ReadAsByteArrayAsync();
                    web = Encoding.UTF8.GetString(responseArray);
                    string code = JsonDocument.Parse(web).RootElement.GetProperty("code").ToString();
                    if (code == "86038")
                    {
                        LogColor("二维码已过期, 请重新执行登录指令.");
                        break;
                    }
                    else if (code == "86039") //等待扫码
                    {
                        continue;
                    }
                    else
                    {
                        string cc = JsonDocument.Parse(web).RootElement.GetProperty("data").GetProperty("access_token").ToString();
                        Log("登录成功: AccessToken=" + cc);
                        //导出cookie
                        File.WriteAllText(Path.Combine(Program.APP_DIR, "BBDownTV.data"), "access_token=" + cc);
                        File.Delete("qrcode.png");
                        break;
                    }
                }
            }
            catch (Exception e) { LogError(e.Message); }
        }
    }
}

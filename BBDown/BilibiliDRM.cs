using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using static BBDown.Core.Logger;
namespace BBDown
{
    public class BiliDRM
    {
        string ToHexLower(byte[] data)
        {
            var sb = new StringBuilder(data.Length * 2);
            foreach (var b in data)
                sb.Append(b.ToString("x2")); // "x2" -> 小写，始终补齐2位
            return sb.ToString();
        }

        private readonly byte[] _key;
        private readonly byte[] _iv;
        private readonly byte[] _kid;
        private readonly byte[] _publicKey;
        // 存储kid和对应的key的字典
        private readonly Dictionary<string, string> _keyCache = new Dictionary<string, string>();
        private static readonly byte[] Bi = Encoding.ASCII.GetBytes("bilibili");
        private static readonly byte[] Header = new byte[] { 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };
        private const string ApiUrl = "https://bvc-drm.bilivideo.com/bilidrm";
        private const string PubKeyUrl = "https://bvc-drm.bilivideo.com/cer/bilidrm_pub.key";
        private static readonly HttpClient HttpClient;

        static BiliDRM()
        {
            HttpClient = new HttpClient();
            HttpClient.DefaultRequestHeaders.Add("origin", "https://www.bilibili.com");
            HttpClient.DefaultRequestHeaders.Add("referer", "https://www.bilibili.com/");
            HttpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/114.0.0.0 Safari/537.36");
        }
        public BiliDRM(string kid = null, byte[] aesKey = null)
        {
            _key = aesKey ?? GenerateRandomBytes(16);
            _iv = GenerateRandomBytes(16);
            _kid = kid != null ? Encoding.UTF8.GetBytes(kid) : null;
            //Console.WriteLine($"初始化 BiliDRM 实例，KID={kid}, AES Key={BitConverter.ToString(_key).Replace("-", "")}, IV={BitConverter.ToString(_iv).Replace("-", "")}")
            _publicKey = GetPublicKeyAsync().GetAwaiter().GetResult();
        }
        private static int RunExe(string app, string parms, bool customBin = false)
        {
            int code = 0;
            Process p = new();
            p.StartInfo.FileName = app;
            p.StartInfo.Arguments = parms;
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.RedirectStandardError = true;
            p.StartInfo.CreateNoWindow = false;
            p.ErrorDataReceived += delegate (object sendProcess, DataReceivedEventArgs output) {
                if (!string.IsNullOrWhiteSpace(output.Data))
                    Log(output.Data);
            };
            p.StartInfo.StandardErrorEncoding = Encoding.UTF8;
            p.Start();
            p.BeginErrorReadLine();
            p.WaitForExit();
            p.Close();
            p.Dispose();
            return code;
        }
        private async Task<byte[]> GetPublicKeyAsync()
        {
            //Console.WriteLine("获取 Bilibili DRM 公钥 ...");
            var response = await HttpClient.GetAsync(PubKeyUrl);
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsByteArrayAsync();
            try
            {
                string pemKey = Encoding.UTF8.GetString(content);
                if (pemKey.Contains("-----BEGIN"))
                {
                    return content;
                }

                try
                {
                    using var rsa = RSA.Create();
                    rsa.ImportRSAPublicKey(content, out _);
                    return content;
                }
                catch
                {
                    try
                    {
                        using var rsa = RSA.Create();
                        rsa.ImportSubjectPublicKeyInfo(content, out _);
                        return content;
                    }
                    catch
                    {
                        Console.WriteLine("公钥格式无法直接识别，将使用备选方法处理");
                    }
                }

                return content;
            }
            catch (Exception ex)
            {
                return content; // 返回原始内容，后续处理中会尝试其他方法
            }
        }

        private byte[] EncryptKid(byte[] kid)
        {
            //Console.WriteLine("加密 KID ...");
            using var aes = Aes.Create();
            aes.Mode = CipherMode.ECB;
            aes.Padding = PaddingMode.None;
            aes.Key = _key;
            var encryptor = aes.CreateEncryptor();
            var kidFirst16 = new byte[16];
            Array.Copy(kid, 0, kidFirst16, 0, 16);
            var encKid = encryptor.TransformFinalBlock(kidFirst16, 0, 16);
            var salt = new byte[] { 0x1b, 0xf7, 0xf5, 0x3f, 0x5d, 0x5d, 0x5a, 0x1f, 0x00, 0x00, 0x00, 0x20, 0x00, 0x00, 0x00, 0x20 };
            var kidLast16 = new byte[16];
            Array.Copy(kid, 16, kidLast16, 0, 16);
            var kidBytes = new byte[48]; // 16 + 16 + 16
            Array.Copy(salt, 0, kidBytes, 0, 16);
            Array.Copy(encKid, 0, kidBytes, 16, 16);
            Array.Copy(kidLast16, 0, kidBytes, 32, 16);
            aes.Mode = CipherMode.CBC;
            aes.IV = _iv;
            encryptor = aes.CreateEncryptor();
            var encryptedKid = encryptor.TransformFinalBlock(kidBytes, 0, kidBytes.Length);
            //Console.WriteLine($"加密后的 KID (len={encryptedKid.Length}) -> {BitConverter.ToString(encryptedKid).Replace("-", "")}");
            return encryptedKid;
        }
        private byte[] EncryptKey()
        {
            using var rsa = RSA.Create();
            string pemKey = Encoding.UTF8.GetString(_publicKey);
                pemKey = pemKey.Replace("-----BEGIN PUBLIC KEY-----", "")
                    .Replace("-----END PUBLIC KEY-----", "")
                    .Replace("\n", "")
                    .Replace("\r", "");
                byte[] derKey = Convert.FromBase64String(pemKey);
                rsa.ImportSubjectPublicKeyInfo(derKey, out _);
                var encrypted = rsa.Encrypt(_key, RSAEncryptionPadding.OaepSHA1);
                //Console.WriteLine($"RSA 加密后的 AES Key 长度: {encrypted.Length}");
                return encrypted;

        }
        private string EncryptSpc(byte[] kid)
        {
            var contentKeyCtx = EncryptKid(kid);
            using var sha1 = SHA1.Create();
            var shaDigest = sha1.ComputeHash(_publicKey);
            var spcBytes = new MemoryStream();
            spcBytes.Write(Bi, 0, Bi.Length); // bilibili
            spcBytes.Write(Header, 0, Header.Length); // header
            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var timeBytes = BitConverter.GetBytes((int)timestamp);
            if (BitConverter.IsLittleEndian)
                Array.Reverse(timeBytes);
            spcBytes.Write(timeBytes, 0, timeBytes.Length); // time
            spcBytes.Write(_iv, 0, _iv.Length); // iv
            var cipherKey = EncryptKey();
            spcBytes.Write(cipherKey, 0, cipherKey.Length); // cipher_key
            spcBytes.Write(shaDigest, 0, shaDigest.Length); // sha
            var sizeBytes = BitConverter.GetBytes(contentKeyCtx.Length);
            if (BitConverter.IsLittleEndian)
                Array.Reverse(sizeBytes);
            spcBytes.Write(sizeBytes, 0, sizeBytes.Length);
            spcBytes.Write(contentKeyCtx, 0, contentKeyCtx.Length); // contentKeyCtx
            var spcB64 = Convert.ToBase64String(spcBytes.ToArray());
            //Console.WriteLine($"SPC Base64 长度: {spcB64.Length}");
            return spcB64;
        }
        public async Task<string> GetKeyAsync(string kid = null)
        {
            byte[] kidBytes;
            string kidStr;

            if (kid == null)
            {
                if (_kid == null)
                {
                    throw new ArgumentNullException("kid", "必须提供KID参数或在构造函数中初始化KID");
                }
                kidBytes = _kid;
                kidStr = Encoding.UTF8.GetString(_kid);
            }
            else
            {
                kidBytes = Encoding.UTF8.GetBytes(kid);
                kidStr = kid;
            }

            if (_keyCache.TryGetValue(kidStr, out var cachedKey))
            {
                return cachedKey;
            }
            var spc = EncryptSpc(kidBytes);
            var content = new StringContent($"{{\"spc\":\"{spc}\"}}", Encoding.UTF8, "application/json");
            var response = await HttpClient.PostAsync(ApiUrl, content);
            response.EnsureSuccessStatusCode();
            var responseJson = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(responseJson);
            string ckc = doc.RootElement.GetProperty("ckc").GetString() ?? throw new Exception("未知错误");
            if (ckc == null)
            {
                var message = "未知错误";
                Console.WriteLine($"请求失败: {message}");
                throw new Exception(message);
            }
            var ckcBytes = Convert.FromBase64String(ckc);
            var ms = new MemoryStream(ckcBytes);
            var reader = new BinaryReader(ms);
            reader.ReadBytes(12);
            var timeBytes = reader.ReadBytes(4);
            if (BitConverter.IsLittleEndian)
                Array.Reverse(timeBytes);
            var iv = reader.ReadBytes(16);
            var dataLenBytes = reader.ReadBytes(4);
            if (BitConverter.IsLittleEndian)
                Array.Reverse(dataLenBytes);
            var dataLen = BitConverter.ToInt32(dataLenBytes, 0);
            var data = reader.ReadBytes(dataLen);
            //Console.WriteLine("解密 CKC 数据 ...");
            using var aes = Aes.Create();
            aes.Mode = CipherMode.CBC;
            aes.Key = _key;
            aes.IV = iv;
            aes.Padding = PaddingMode.None;
            var decryptor = aes.CreateDecryptor();
            var decrypted = decryptor.TransformFinalBlock(data, 0, data.Length);
            aes.Mode = CipherMode.ECB;
            decryptor = aes.CreateDecryptor();
            var finalKey = decryptor.TransformFinalBlock(decrypted, decrypted.Length - 16, 16);
            var resultKey = ToHexLower(finalKey);
            _keyCache[kidStr] = resultKey;

            return resultKey;
        }

        private static byte[] GenerateRandomBytes(int length)
        {
            var bytes = new byte[length];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(bytes);
            return bytes;
        }

        public async Task<string> Decrypt(string Path,string kid)
        {
            Log($"检测到视频需要解密, 开始获取解密KEY...");
            kid = kid.Substring(kid.Length - 32);
            string key = await GetKeyAsync(kid);
            Log($"获取到解密KEY: {key}");
            var binPath = BBDownUtil.FindExecutable("mp4decrypt");
            if (string.IsNullOrEmpty(binPath))
                throw new Exception("找不到可执行的mp4decrypt文件");
            var parms = $"--key {kid}:{key} \"{Path}\" \"{Path}.decrypted.mp4\"";
            var code = RunExe(binPath, parms);
            if (code != 0)
                throw new Exception($"解密失败, 错误码: {code}");
            File.Delete(Path);
            File.Move(Path + ".decrypted.mp4", Path);
            return Path;
        }
    }
}
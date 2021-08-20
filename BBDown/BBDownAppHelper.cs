using ProtoBuf;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using static BBDown.BBDownLogger;

namespace BBDown
{
    class BBDownAppHelper
    {
        private static string API = "https://grpc.biliapi.net/bilibili.app.playurl.v1.PlayURL/PlayView";
        //private static string API2 = "https://app.bilibili.com/bilibili.pgc.gateway.player.v1.PlayURL/PlayView";
        private static string dalvikVer = "2.1.0";
        private static string osVer = "11";
        private static string brand = "M2012K11AC";
        private static string model = "Build/RKQ1.200826.002";
        private static string appVer = "6.32.0";
        private static int build = 6320200;
        private static string channel = "xiaomi_cn_tv.danmaku.bili_zm20200902";
        private static Network.Type networkType = Network.Type.Wifi;
        private static string networkOid = "46007";
        private static string cronet = "1.36.1";
        private static string buvid = "";
        private static string mobiApp = "android";
        private static string appKey = "android64";
        private static string sessionId = "dedf8669";
        private static string platform = "android";
        private static string env = "prod";
        private static int appId = 1;
        private static string region = "CN";
        private static string language = "zh";

        /// <summary>
        /// 发起请求并返回响应报文(protobuf -> json)
        /// </summary>
        /// <param name="epId"></param>
        /// <param name="cid"></param>
        /// <param name="qn"></param>
        /// <param name="appkey"></param>
        /// <returns></returns>
        public static string DoReq(string aid, string cid, string qn, bool bangumi, bool onlyAvc, string appkey = "")
        {
            var headers = GetHeader(appkey);
            LogDebug("App-Req-Headers: {0}", ConvertToString(headers));
            var body = GetPayload(Convert.ToInt64(aid), Convert.ToInt64(cid), Convert.ToInt64(qn), onlyAvc ? PlayViewReq.CodeType.Code264 : PlayViewReq.CodeType.Code265);
            //Console.WriteLine(ReadMessage<PlayViewReq>(body));
            var data = GetPostResponse(bangumi ? API : API, body, headers);
            var resp = ReadMessage<PlayViewReply>(data);
            LogDebug("PlayViewReplyPlain: {0}", ConvertToString(resp));
            return ConvertToDashJson(resp);
        }

        /// <summary>
        /// 将protobuf转换成网页那种json 这样就不用修改之前的解析逻辑了
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        private static string ConvertToDashJson(object data)
        {
            var resp = (PlayViewReply)data;
            var videos = new List<object>();
            var audios = new List<object>();

            if (resp.videoInfo.streamLists != null)
            {
                foreach (var item in resp.videoInfo.streamLists)
                {
                    if (item.dashVideo != null)
                    {
                        videos.Add(new AudioInfoWitCodecId(
                            item.streamInfo.Quality,
                            item.dashVideo.baseUrl,
                            (uint)(item.dashVideo.Size * 8 / 1024 / (resp.videoInfo.Timelength / 1000)),
                            item.dashVideo.Codecid
                        ));
                    }
                }
            }

            if (resp.videoInfo.dashAudioes != null)
            {
                foreach (var item in resp.videoInfo.dashAudioes)
                {
                    audios.Add(new AudioInfoWithCodecName(
                        item.Id,
                        item.baseUrl,
                        item.Bandwidth,
                        "M4A"
                    ));
                }
            }

            if (resp.videoInfo.Dolby != null && resp.videoInfo.Dolby.Audio != null)
            {
                audios.Add(new AudioInfoWithCodecName(
                    resp.videoInfo.Dolby.Audio.Id,
                    resp.videoInfo.Dolby.Audio.baseUrl,
                    resp.videoInfo.Dolby.Audio.Bandwidth,
                    "E-AC-3"
                ));
            }

            var json = new DashJson(
                0,
                "0",
                1,
                new DashData(
                    resp.videoInfo.Timelength,
                    new DashInfo(
                        videos,
                        audios
                    )
                )
            );

            return ConvertToString(json);
        }

        private static byte[] GetPayload(long aid, long cid, long qn, PlayViewReq.CodeType codec)
        {
            var obj = new PlayViewReq();
            obj.epId = aid;
            obj.Cid = cid;
            obj.Qn = qn;
            obj.Fnval = 976;
            obj.Spmid = "main.ugc-video-detail.0.0";
            obj.fromSpmid = "main.my-history.0.0";
            obj.preferCodecType = codec;
            LogDebug("PayLoadPlain: {0}", ConvertToString(obj));
            return PackMessage(ObjectToBytes(obj));
        }


        #region 生成Headers相关方法

        private static Dictionary<string, string> GetHeader(string appkey)
        {
            return new Dictionary<string, string>()
            {
                ["Host"] = "grpc.biliapi.net",
                ["user-agent"] = $"Dalvik/{dalvikVer} (Linux; U; Android {osVer}; {brand} {model}) {appVer} os/android model/{brand} mobi_app/android build/{build} channel/{channel} innerVer/{build} osVer/{osVer} network/2 grpc-java-cronet/{cronet}",
                ["te"] = "trailers",
                ["x-bili-fawkes-req-bin"] = GenerateFawkesReqBin(),
                ["x-bili-metadata-bin"] = GenerateMetadataBin(appkey),
                ["authorization"] = $"identify_v1 {appKey}",
                ["x-bili-device-bin"] = GenerateDeviceBin(),
                ["x-bili-network-bin"] = GenerateNetworkBin(),
                ["x-bili-restriction-bin"] = "",
                ["x-bili-locale-bin"] = GenerateLocaleBin(),
                ["x-bili-exps-bin"] = "",
                ["grpc-encoding"] = "gzip",
                ["grpc-accept-encoding"] = "identity,gzip",
                ["grpc-timeout"] = "17996161u",
            };
        }

        private static string GenerateLocaleBin()
        {
            var obj = new Locale();
            obj.cLocale = new Locale.LocaleIds();
            obj.cLocale.Language = language;
            obj.cLocale.Region = region;
            return SerializeToBase64(obj);
        }

        private static string GenerateNetworkBin()
        {
            var obj = new Network();
            obj.type = networkType;
            obj.Oid = networkOid;
            return SerializeToBase64(obj);
        }

        private static string GenerateDeviceBin()
        {
            var obj = new Device();
            obj.appId = appId;
            obj.Build = build;
            obj.Buvid = buvid;
            obj.mobiApp = mobiApp;
            obj.Platform = platform;
            obj.Channel = channel;
            obj.Brand = brand;
            obj.Model = model;
            obj.Osver = osVer;
            return SerializeToBase64(obj);
        }

        private static string GenerateMetadataBin(string appkey)
        {
            var obj = new Metadata();
            obj.accessKey = appkey;
            obj.mobiApp = mobiApp;
            obj.Build = build;
            obj.Channel = channel;
            obj.Buvid = buvid;
            obj.Platform = platform;
            return SerializeToBase64(obj);
        }

        private static string GenerateFawkesReqBin()
        {
            var obj = new FawkesReq();
            obj.Appkey = appKey;
            obj.Env = env;
            obj.sessionId = sessionId;
            return SerializeToBase64(obj);
        }

        /// <summary>
        /// 对象转字节数组
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        private static byte[] ObjectToBytes(Object obj)
        {
            using (var stream = new MemoryStream())
            {
                Serializer.Serialize(stream, obj);
                return stream.ToArray();
            }
        }

        /// <summary>
        /// 序列化为字符串
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        private static string SerializeToBase64(Object obj)
        {
            return Convert.ToBase64String(ObjectToBytes(obj)).TrimEnd('=');
        }

        #endregion

        /// <summary>
        /// 对象转字符串(json)
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        private static string ConvertToString(object obj)
        {
            return JsonSerializer.Serialize(obj);
        }

        /// <summary>
        /// 读取gRPC响应流 通过前5字节信息 解析/解压后面的报文体
        /// </summary>
        /// <param name="data"></param>
        /// <returns>字节流</returns>
        private static byte[] ReadMessage(byte[] data)
        {
            byte first;
            int size;
            (first, size) = ReadInfo(data);
            if (first == 1)
            {
                return GzipDecompress(data.Skip(5).ToArray());
            }
            return data.Skip(5).Take(size).ToArray();
        }

        /// <summary>
        /// 读取gRPC响应流 通过前5字节信息 解析/解压后面的报文体
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="data"></param>
        /// <returns>对应的protobuf</returns>
        private static T ReadMessage<T>(byte[] data)
        {
            var msg = ReadMessage(data);
            var obj = Serializer.Deserialize<T>(new MemoryStream(msg));
            return obj;
        }

        /// <summary>
        /// 读取报文长度
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        private static (byte first, int size) ReadInfo(byte[] data)
        {
            using (var stream = new MemoryStream(data.Take(5).ToArray()))
            {
                using (var reader = new BinaryReader(stream))
                {
                    var value1 = reader.ReadByte();
                    var value2 = reader.ReadBytes(4);

                    return (value1, BitConverter.ToInt32(value2.Reverse().ToArray()));
                }
            }
        }

        /// <summary>
        /// 给请求载荷添加头部信息
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        private static byte[] PackMessage(byte[] input)
        {
            using (var stream = new MemoryStream())
            {
                using (var writer = new BinaryWriter(stream))
                {
                    var comp = GzipCompress(input);
                    var reverse = BitConverter.GetBytes(comp.Length).Reverse().ToArray();
                    writer.Write((byte)1);
                    writer.Write(reverse);
                    writer.Write(comp);
                }
                return stream.ToArray();
            }
        }

        /// <summary>
        /// gzip压缩
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        private static byte[] GzipCompress(byte[] data)
        {
            using (var output = new MemoryStream())
            {
                using (var comp = new GZipStream(output, CompressionMode.Compress))
                {
                    comp.Write(data, 0, data.Length);
                }
                return output.ToArray();
            }
        }

        /// <summary>
        /// gzip解压
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        private static byte[] GzipDecompress(byte[] data)
        {
            using (var output = new MemoryStream())
            {
                using (var input = new MemoryStream(data))
                {
                    using (var decomp = new GZipStream(input, CompressionMode.Decompress))
                    {
                        decomp.CopyTo(output);
                    }
                }
                return output.ToArray();
            }
        }

        public static byte[] GetPostResponse(string Url, byte[] postData, Dictionary<string, string> headers)
        {
            LogDebug("Post to: {0}, data: {1}", Url, Convert.ToBase64String(postData));

            HttpClient client = new HttpClient(new HttpClientHandler
            {
                AllowAutoRedirect = true,
                //Proxy = null
            });

            ByteArrayContent content = new ByteArrayContent(postData);
            content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/grpc");

            HttpRequestMessage request = new HttpRequestMessage()
            {
                RequestUri = new Uri(Url),
                Method = HttpMethod.Post,
                Content = content,
                //Version = HttpVersion.Version20
            };

            if (headers != null)
                foreach (KeyValuePair<string, string> header in headers)
                    request.Headers.TryAddWithoutValidation(header.Key, header.Value);

            HttpResponseMessage response = client.SendAsync(request).Result;
            byte[] bytes = response.Content.ReadAsByteArrayAsync().Result;

            return bytes;
        }
    }

    internal class AudioInfoWithCodecName
    {
        [JsonPropertyName("id")]
        public uint Id { get; }
        [JsonPropertyName("base_url")]
        public string BaseUrl { get; }
        [JsonPropertyName("bandwidth")]
        public uint Bandwidth { get; }
        [JsonPropertyName("codecs")]
        public string Codecs { get; }

        public AudioInfoWithCodecName(uint id, string base_url, uint bandwidth, string codecs)
        {
            Id = id;
            BaseUrl = base_url;
            Bandwidth = bandwidth;
            Codecs = codecs;
        }

        public override bool Equals(object obj) => obj is AudioInfoWithCodecName other && Id == other.Id && BaseUrl == other.BaseUrl && Bandwidth == other.Bandwidth && Codecs == other.Codecs;
        public override int GetHashCode() => HashCode.Combine(Id, BaseUrl, Bandwidth, Codecs);
    }

    internal class AudioInfoWitCodecId
    {
        [JsonPropertyName("id")]
        public uint Id { get; }
        [JsonPropertyName("base_url")]
        public string BaseUrl { get; }
        [JsonPropertyName("bandwidth")]
        public uint Bandwidth { get; }
        [JsonPropertyName("codecid")]
        public uint Codecid { get; }

        public AudioInfoWitCodecId(uint id, string base_url, uint bandwidth, uint codecid)
        {
            Id = id;
            BaseUrl = base_url;
            Bandwidth = bandwidth;
            Codecid = codecid;
        }

        public override bool Equals(object obj) => obj is AudioInfoWitCodecId other && Id == other.Id && BaseUrl == other.BaseUrl && Bandwidth == other.Bandwidth && Codecid == other.Codecid;
        public override int GetHashCode() => HashCode.Combine(Id, BaseUrl, Bandwidth, Codecid);
    }

    internal class DashInfo
    {
        [JsonPropertyName("video")]
        public List<object> Video { get; }
        [JsonPropertyName("audio")]
        public List<object> Audio { get; }

        public DashInfo(List<object> video, List<object> audio)
        {
            Video = video;
            Audio = audio;
        }

        public override bool Equals(object obj) => obj is DashInfo other && EqualityComparer<List<object>>.Default.Equals(Video, other.Video) && EqualityComparer<List<object>>.Default.Equals(Audio, other.Audio);
        public override int GetHashCode() => HashCode.Combine(Video, Audio);
    }

    internal class DashData
    {
        [JsonPropertyName("timelength")]
        public ulong TimeLength { get; }
        [JsonPropertyName("dash")]
        public DashInfo Dash { get; }

        public DashData(ulong timelength, DashInfo dash)
        {
            TimeLength = timelength;
            Dash = dash;
        }

        public override bool Equals(object obj) => obj is DashData other && TimeLength == other.TimeLength && EqualityComparer<DashInfo>.Default.Equals(Dash, other.Dash);
        public override int GetHashCode() => HashCode.Combine(TimeLength, Dash);
    }

    internal class DashJson
    {
        [JsonPropertyName("code")]
        public int Code { get; }
        [JsonPropertyName("message")]
        public string Message { get; }
        [JsonPropertyName("ttl")]
        public int Ttl { get; }
        [JsonPropertyName("data")]
        public DashData Data { get; }

        public DashJson(int code, string message, int ttl, DashData data)
        {
            Code = code;
            Message = message;
            Ttl = ttl;
            Data = data;
        }

        public override bool Equals(object obj) => obj is DashJson other && Code == other.Code && Message == other.Message && Ttl == other.Ttl && EqualityComparer<DashData>.Default.Equals(Data, other.Data);
        public override int GetHashCode() => HashCode.Combine(Code, Message, Ttl, Data);
    }
}

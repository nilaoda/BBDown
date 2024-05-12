using BBDown.Core.Protobuf;
using Google.Protobuf;
using System.Buffers.Binary;
using System.IO.Compression;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using static BBDown.Core.Util.HTTPUtil;
using static BBDown.Core.Logger;

namespace BBDown.Core
{
    class AppHelper
    {
        private static readonly string API = "https://grpc.biliapi.net/bilibili.app.playurl.v1.PlayURL/PlayView";
        private static readonly string API2 = "https://app.bilibili.com/bilibili.pgc.gateway.player.v2.PlayURL/PlayView";
        private static readonly string dalvikVer = "2.1.0";
        private static readonly string osVer = "11";
        private static readonly string brand = "M2012K11AC";
        private static readonly string model = "Build/RKQ1.200826.002";
        private static readonly string appVer = "7.32.0";
        private static readonly int build = 7320200; // 新版才能抓到配音
        private static readonly string channel = "xiaomi_cn_tv.danmaku.bili_zm20200902";
        private static readonly Network.Types.TYPE networkType = Network.Types.TYPE.Wifi;
        private static readonly string networkOid = "46007";
        private static readonly string cronet = "1.36.1";
        private static readonly string buvid = "";
        private static readonly string mobiApp = "android";
        private static readonly string appKey = "android64";
        private static readonly string sessionId = "dedf8669";
        private static readonly string platform = "android";
        private static readonly string env = "prod";
        private static readonly int appId = 1;
        private static readonly string region = "CN";
        private static readonly string language = "zh";

        private static PlayViewReq.Types.CodeType GetVideoCodeType(string code)
        {
            return code switch
            {
                "AVC" => PlayViewReq.Types.CodeType.Code264,
                "HEVC" => PlayViewReq.Types.CodeType.Code265,
                "AV1" => PlayViewReq.Types.CodeType.Codeav1,
                _ => PlayViewReq.Types.CodeType.Code265
            };
        }

        /// <summary>
        /// 发起请求并返回响应报文(protobuf -> json)
        /// </summary>
        /// <param name="epId"></param>
        /// <param name="cid"></param>
        /// <param name="qn"></param>
        /// <param name="appkey"></param>
        /// <returns></returns>
        public static async Task<string> DoReqAsync(string aid, string cid, string epId, string qn, bool bangumi, string encoding, string appkey = "")
        {

            var headers = GetHeader(appkey);
            LogDebug("App-Req-Headers: {0}", JsonSerializer.Serialize(headers, JsonContext.Default.DictionaryStringString));
            byte[] data;
            // 只有pgc接口才有配音和片头尾信息
            if (bangumi)
            {
                if (!(string.IsNullOrEmpty(encoding) || encoding == "HEVC"))
                    LogWarn("APP的番剧不支持 HEVC 以外的编码");
                var body = GetPayload(Convert.ToInt64(epId), Convert.ToInt64(cid), Convert.ToInt64(qn), PlayViewReq.Types.CodeType.Code265);
                data = await GetPostResponseAsync(API2, body, headers);
            }
            else
            {
                var body = GetPayload(Convert.ToInt64(aid), Convert.ToInt64(cid), Convert.ToInt64(qn), GetVideoCodeType(encoding));
                data = await GetPostResponseAsync(API, body, headers);
            }
            var resp = new MessageParser<PlayViewReply>(() => new PlayViewReply()).ParseFrom(ReadMessage(data));

            LogDebug("PlayViewReplyPlain: {0}", JsonSerializer.Serialize(resp, JsonContext.Default.PlayViewReply));
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
            var clips = new List<object>();

            if (resp.VideoInfo.StreamList != null)
            {
                foreach (var item in resp.VideoInfo.StreamList)
                {
                    if (item.DashVideo != null)
                    {
                        videos.Add(new AudioInfoWitCodecId(
                            item.StreamInfo.Quality,
                            item.DashVideo.BaseUrl,
                            item.DashVideo.BackupUrl.ToList(),
                            (uint)(item.DashVideo.Size * 8 / (resp.VideoInfo.Timelength / 1000)),
                            item.DashVideo.Codecid
                        ));
                    }
                }
            }

            if (resp.VideoInfo.DashAudio != null)
            {
                audios.AddRange(resp.VideoInfo.DashAudio.Select(item => new AudioInfoWithCodecName(
                    item.Id,
                    item.BaseUrl,
                    item.BackupUrl.ToList(),
                    item.Bandwidth,
                    "M4A"
                )));
            }

            if (resp.VideoInfo.Flac != null && resp.VideoInfo.Flac.Audio != null)
            {
                audios.Add(new AudioInfoWithCodecName(
                    resp.VideoInfo.Flac.Audio.Id,
                    resp.VideoInfo.Flac.Audio.BaseUrl,
                    resp.VideoInfo.Flac.Audio.BackupUrl.ToList(),
                    resp.VideoInfo.Flac.Audio.Bandwidth,
                    "FLAC"
                ));
            }

            if (resp.VideoInfo.Dolby != null && resp.VideoInfo.Dolby.Audio != null)
            {
                audios.Add(new AudioInfoWithCodecName(
                    resp.VideoInfo.Dolby.Audio.Id,
                    resp.VideoInfo.Dolby.Audio.BaseUrl,
                    resp.VideoInfo.Dolby.Audio.BackupUrl.ToList(),
                    resp.VideoInfo.Dolby.Audio.Bandwidth,
                    "E-AC-3"
                ));
            }

            if (resp.Business != null && resp.Business.ClipInfo != null)
            {
                clips.AddRange(resp.Business.ClipInfo.Select(clip => new DashClip(
                    clip.Start,
                    clip.End,
                    clip.ToastText
                )));
            }

            var backgroundAudios = new List<object>();
            var roles = new List<object>();
            if (resp.PlayExtInfo != null && resp.PlayExtInfo.PlayDubbingInfo != null && resp.PlayExtInfo.PlayDubbingInfo.BackgroundAudio != null)
            {
                var dubInfo = resp.PlayExtInfo.PlayDubbingInfo;

                backgroundAudios.AddRange(dubInfo.BackgroundAudio.Audio.Select(item => new AudioInfoWithCodecName(
                    item.Id,
                    item.BaseUrl,
                    item.BackupUrl.ToList(),
                    item.Bandwidth,
                    "M4A"
                )));

                foreach (var item in dubInfo.RoleAudioList)
                {
                    foreach (var role in item.AudioMaterialList)
                    {
                        List<object> roleAudios = role.Audio.Select(item => new AudioInfoWithCodecName(
                            item.Id,
                            item.BaseUrl,
                            item.BackupUrl.ToList(),
                            item.Bandwidth,
                            "M4A"
                        )).Cast<object>().ToList();

                        roles.Add(new AudioMaterial(
                            role.AudioId,
                            role.Title ?? role.AudioId,
                            role.PersonName ?? role.Edition ?? "",
                            roleAudios
                        ));
                    }
                }
            }

            var json = new DashJson(
                0,
                "0",
                1,
                new DashData(
                    resp.VideoInfo.Timelength,
                    new DashInfo(
                        videos,
                        audios
                    ),
                    clips
                ),
                new DubbingInfo(
                    backgroundAudios,
                    roles
                )
            );

            return JsonSerializer.Serialize(json, JsonContext.Default.DashJson);
        }

        private static byte[] GetPayload(long aid, long cid, long qn, PlayViewReq.Types.CodeType codec)
        {
            var obj = new PlayViewReq
            {
                EpId = aid,
                Cid = cid,
                //obj.Qn = qn;
                Qn = 127,
                Fnval = 4048,
                Fourk = true,
                Spmid = "main.ugc-video-detail.0.0",
                FromSpmid = "main.my-history.0.0",
                PreferCodecType = codec,
                Download = 0, //0:播放 1:flv下载 2:dash下载
                ForceHost = 2 //0:允许使用ip 1:使用http 2:使用https
            };
            LogDebug("PayLoadPlain: {0}", JsonSerializer.Serialize(obj, JsonContext.Default.PlayViewReq));
            return PackMessage(obj.ToByteArray());
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
                ["authorization"] = $"identify_v1 {Config.TOKEN}",
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
            var obj = new Locale
            {
                CLocale = new Locale.Types.LocaleIds
                {
                    Language = language,
                    Region = region
                }
            };
            return Convert.ToBase64String(obj.ToByteArray());
        }

        private static string GenerateNetworkBin()
        {
            var obj = new Network
            {
                Type = networkType,
                Oid = networkOid
            };
            return Convert.ToBase64String(obj.ToByteArray());
        }

        private static string GenerateDeviceBin()
        {
            var obj = new Device
            {
                AppId = appId,
                Build = build,
                Buvid = buvid,
                MobiApp = mobiApp,
                Platform = platform,
                Channel = channel,
                Brand = brand,
                Model = model,
                Osver = osVer
            };
            return Convert.ToBase64String(obj.ToByteArray());
        }

        private static string GenerateMetadataBin(string appkey)
        {
            var obj = new Metadata
            {
                AccessKey = appkey,
                MobiApp = mobiApp,
                Build = build,
                Channel = channel,
                Buvid = buvid,
                Platform = platform
            };
            return Convert.ToBase64String(obj.ToByteArray());
        }

        private static string GenerateFawkesReqBin()
        {
            var obj = new FawkesReq
            {
                Appkey = appKey,
                Env = env,
                SessionId = sessionId
            };
            return Convert.ToBase64String(obj.ToByteArray());
        }

        #endregion

        /// <summary>
        /// 读取gRPC响应流 通过前5字节信息 解析/解压后面的报文体
        /// </summary>
        /// <param name="data"></param>
        /// <returns>字节流</returns>
        public static byte[] ReadMessage(byte[] data)
        {
            byte first;
            int size;
            (first, size) = ReadInfo(data);
            return first == 1 ? GzipDecompress(data[5..]) : data[5..(5 + size)];
        }

        /// <summary>
        /// 读取报文长度
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        private static (byte first, int size) ReadInfo(byte[] data)
        {
            var value1 = data[0];
            var value2 = data[1..5];

            return (value1, BinaryPrimitives.ReadInt32BigEndian(value2));
        }

        /// <summary>
        /// 给请求载荷添加头部信息
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static byte[] PackMessage(byte[] input)
        {
            using var stream = new MemoryStream();
            using (var writer = new BinaryWriter(stream))
            {
                var comp = GzipCompress(input);
                var reverse = (stackalloc byte[4]);
                writer.Write((byte)1);
                BinaryPrimitives.WriteInt32BigEndian(reverse, comp.Length);
                writer.Write(reverse);
                writer.Write(comp);
            }
            return stream.ToArray();
        }

        /// <summary>
        /// gzip压缩
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        private static byte[] GzipCompress(byte[] data)
        {
            using var output = new MemoryStream();
            using (var comp = new GZipStream(output, CompressionMode.Compress))
            {
                comp.Write(data, 0, data.Length);
            }
            return output.ToArray();
        }

        /// <summary>
        /// gzip解压
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        private static byte[] GzipDecompress(byte[] data)
        {
            using var output = new MemoryStream();
            using (var input = new MemoryStream(data))
            {
                using var decomp = new GZipStream(input, CompressionMode.Decompress);
                decomp.CopyTo(output);
            }
            return output.ToArray();
        }
    }


    [JsonSerializable(typeof(AudioMaterial))]
    [JsonSerializable(typeof(DubbingInfo))]
    [JsonSerializable(typeof(DashClip))]
    [JsonSerializable(typeof(AudioInfoWithCodecName))]
    [JsonSerializable(typeof(AudioInfoWitCodecId))]
    [JsonSerializable(typeof(DashJson))]
    [JsonSerializable(typeof(PlayViewReq))]
    [JsonSerializable(typeof(PlayViewReply))]
    [JsonSerializable(typeof(Dictionary<string, string>))]
    internal partial class JsonContext : JsonSerializerContext { }

    internal class AudioMaterial
    {
        [JsonPropertyName("audio_id")]
        public string AudioId { get; }
        [JsonPropertyName("title")]
        public string Title { get; }
        [JsonPropertyName("person_name")]
        public string PersonName { get; }
        [JsonPropertyName("audio")]
        public List<object> Audio { get; }

        public AudioMaterial(string audio_id, string title, string person_name, List<object> audio)
        {
            AudioId = audio_id;
            Title = title;
            PersonName = person_name;
            Audio = audio;
        }

        public override bool Equals(object? obj) => obj is AudioMaterial other && AudioId == other.AudioId && Title == other.Title && PersonName == other.PersonName && Audio == other.Audio;
        public override int GetHashCode() => HashCode.Combine(Title, Audio);
    }

    internal class DubbingInfo
    {
        [JsonPropertyName("background_audio")]
        public List<object> BackgroundAudio { get; }
        [JsonPropertyName("role_audio_list")]
        public List<object> RoleAudioList { get; }

        public DubbingInfo(List<object> background_audio, List<object> role_audio_list)
        {
            BackgroundAudio = background_audio;
            RoleAudioList = role_audio_list;
        }

        public override bool Equals(object? obj) => obj is DubbingInfo other && BackgroundAudio == other.BackgroundAudio && RoleAudioList == other.RoleAudioList;
        public override int GetHashCode() => HashCode.Combine(BackgroundAudio, RoleAudioList);
    }

    internal class DashClip
    {
        [JsonPropertyName("start")]
        public int Start { get; }
        [JsonPropertyName("end")]
        public int End { get; }
        [JsonPropertyName("toastText")]
        public string ToastText { get; }

        public DashClip(int start, int end, string toastText)
        {
            Start = start;
            End = end;
            ToastText = toastText;
        }

        public override bool Equals(object? obj) => obj is DashClip other && Start == other.Start && End == other.End && ToastText == other.ToastText;
        public override int GetHashCode() => HashCode.Combine(Start, End, ToastText);
    }

    internal class AudioInfoWithCodecName
    {
        [JsonPropertyName("id")]
        public uint Id { get; }
        [JsonPropertyName("base_url")]
        public string BaseUrl { get; }
        [JsonPropertyName("backup_url")]
        public List<string> BackupUrl { get; }
        [JsonPropertyName("bandwidth")]
        public uint Bandwidth { get; }
        [JsonPropertyName("codecs")]
        public string Codecs { get; }

        public AudioInfoWithCodecName(uint id, string base_url, List<string> backup_url, uint bandwidth, string codecs)
        {
            Id = id;
            BaseUrl = base_url;
            BackupUrl = backup_url;
            Bandwidth = bandwidth;
            Codecs = codecs;
        }

        public override bool Equals(object? obj) => obj is AudioInfoWithCodecName other && Id == other.Id && BaseUrl == other.BaseUrl && BackupUrl.SequenceEqual(other.BackupUrl) && Bandwidth == other.Bandwidth && Codecs == other.Codecs;
        public override int GetHashCode() => HashCode.Combine(Id, BaseUrl, BackupUrl, Bandwidth, Codecs);
    }

    internal class AudioInfoWitCodecId
    {
        [JsonPropertyName("id")]
        public uint Id { get; }
        [JsonPropertyName("base_url")]
        public string BaseUrl { get; }
        [JsonPropertyName("backup_url")]
        public List<string> BackupUrl { get; }
        [JsonPropertyName("bandwidth")]
        public uint Bandwidth { get; }
        [JsonPropertyName("codecid")]
        public uint Codecid { get; }

        public AudioInfoWitCodecId(uint id, string base_url, List<string> backup_url, uint bandwidth, uint codecid)
        {
            Id = id;
            BaseUrl = base_url;
            BackupUrl = backup_url;
            Bandwidth = bandwidth;
            Codecid = codecid;
        }

        public override bool Equals(object? obj) => obj is AudioInfoWitCodecId other && Id == other.Id && BaseUrl == other.BaseUrl && Bandwidth == other.Bandwidth && Codecid == other.Codecid;
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

        public override bool Equals(object? obj) => obj is DashInfo other && EqualityComparer<List<object>>.Default.Equals(Video, other.Video) && EqualityComparer<List<object>>.Default.Equals(Audio, other.Audio);
        public override int GetHashCode() => HashCode.Combine(Video, Audio);
    }

    internal class DashData
    {
        [JsonPropertyName("timelength")]
        public ulong TimeLength { get; }
        [JsonPropertyName("dash")]
        public DashInfo Dash { get; }
        [JsonPropertyName("clip_info_list")]
        public List<object> ClipList { get; }

        public DashData(ulong timelength, DashInfo dash, List<object> clipList)
        {
            TimeLength = timelength;
            Dash = dash;
            ClipList = clipList;
        }

        public override bool Equals(object? obj) => obj is DashData other && TimeLength == other.TimeLength && EqualityComparer<DashInfo>.Default.Equals(Dash, other.Dash) && EqualityComparer<List<object>>.Default.Equals(ClipList, other.ClipList);
        public override int GetHashCode() => HashCode.Combine(TimeLength, Dash, ClipList);
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
        [JsonPropertyName("dubbing_info")]
        public DubbingInfo DubbingInfo { get; }

        public DashJson(int code, string message, int ttl, DashData data, DubbingInfo dubbingInfo)
        {
            Code = code;
            Message = message;
            Ttl = ttl;
            Data = data;
            DubbingInfo = dubbingInfo;
        }

        public override bool Equals(object? obj) => obj is DashJson other && Code == other.Code && Message == other.Message && Ttl == other.Ttl && EqualityComparer<DashData>.Default.Equals(Data, other.Data);
        public override int GetHashCode() => HashCode.Combine(Code, Message, Ttl, Data, DubbingInfo);
    }
}

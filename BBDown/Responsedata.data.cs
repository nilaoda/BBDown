using System.Collections.Generic;
using Newtonsoft.Json;
using static BBDown.BBDownEntity;
using static BBDown.Program;

namespace BBDown
{
    public class Data
    {
        [JsonProperty("durl")]
        public Durl[] Durl { get; set; }
        [JsonProperty("dash")]
        public Dash Dash { get; set; }

        [JsonProperty("timelength")]
        public int Timelength { get; set; }

        [JsonProperty("seek_param")]
        public string SeekParam { get; set; }
        [JsonProperty("support_formats")]
        public SupportFormat[] SupportFormats { get; set; }

        [JsonProperty("seek_type")]
        public string SeekType { get; set; }
        [JsonProperty("video_codecid")]
        public string VideoCodecid { get; set; }
        [JsonProperty("accept_quality")]
        public List<int> AcceptQuality { get; set; }


        [JsonProperty("format")]
        public string Format { get; set; }


        [JsonProperty("accept_description")]
        public string[] AcceptDescription { get; set; }


        [JsonProperty("accept_format")]
        public string AcceptFormat { get; set; }

        [JsonProperty("quality")]
        public string Quality { get; set; }

        [JsonProperty("from")]
        public string From { get; set; }

        [JsonProperty("result")]
        public string Result { get; set; }

        [JsonProperty("message")]
        public string Message { get; set; }





    }

    public class AudioEntry
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("baseUrl")]
        public string BaseUrl { get; set; }

        [JsonProperty("base_url")]
        public string AudioBaseUrl { get; set; }

        [JsonProperty("backupUrl")]
        public string[] BackupUrl { get; set; }

        [JsonProperty("backup_url")]
        public string[] AudioBackupUrl { get; set; }

        [JsonProperty("bandwidth")]
        public long Bandwidth { get; set; }

        [JsonProperty("mimeType")]
        public string MimeType { get; set; }

        [JsonProperty("mime_type")]
        public string AudioMimeType { get; set; }

        [JsonProperty("codecs")]
        public string Codecs { get; set; }

        [JsonProperty("width")]
        public long Width { get; set; }

        [JsonProperty("height")]
        public long Height { get; set; }

        [JsonProperty("frameRate")]
        public string FrameRate { get; set; }

        [JsonProperty("frame_rate")]
        public string AudioFrameRate { get; set; }

        [JsonProperty("sar")]
        public string Sar { get; set; }

        [JsonProperty("startWithSap")]
        public long StartWithSap { get; set; }

        [JsonProperty("start_with_sap")]
        public long AudioStartWithSap { get; set; }

        [JsonProperty("SegmentBase")]
        public SegmentBaseEntry SegmentBase { get; set; }

        [JsonProperty("segment_base")]
        public SegmentBaseClass AudioSegmentBase { get; set; }

        [JsonProperty("codecid")]
        public string Codecid { get; set; }

        public class SegmentBaseClass
        {
            [JsonProperty("initialization")]
            public string Initialization { get; set; }

            [JsonProperty("index_range")]
            public string IndexRange { get; set; }
        }

        public class SegmentBaseEntry
        {
            [JsonProperty("Initialization")]
            public string Initialization { get; set; }

            [JsonProperty("indexRange")]
            public string IndexRange { get; set; }
        }
    }


}
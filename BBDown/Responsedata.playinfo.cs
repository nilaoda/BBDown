using System.Collections.Generic;
using Newtonsoft.Json;
using static BBDown.BBDownEntity;
using static BBDown.Program;

namespace BBDown
{
    public class PlayInfo : Data
    {
        [JsonProperty("code")]
        public int Code { get; set; }

        [JsonProperty("is_preview")]
        public int IsPreview { get; set; }

        [JsonProperty("no_rexcode")]
        public int NoRexcode { get; set; }

        [JsonProperty("fnval")]
        public int Fnval { get; set; }

        [JsonProperty("video_project")]
        public bool VideoProject { get; set; }

        [JsonProperty("fnver")]
        public int Fnver { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("bp")]
        public int Bp { get; set; }

        [JsonProperty("result")]
        public string ResultResult { get; set; }

        [JsonProperty("has_paid")]
        public bool HasPaid { get; set; }

        [JsonProperty("status")]
        public int Status { get; set; }


    }

}
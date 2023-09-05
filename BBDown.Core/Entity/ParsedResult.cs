using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static BBDown.Core.Entity.Entity;

namespace BBDown.Core.Entity
{
    public class ParsedResult
    {
        public string WebJsonString { get; set; }
        public List<Video> VideoTracks { get; set; } = new();
        public List<Audio> AudioTracks { get; set; } = new();
        public List<Audio> BackgroundAudioTracks { get; set; } = new();
        public List<AudioMaterialInfo> RoleAudioList { get; set; } = new();
        public List<ViewPoint> ExtraPoints { get; set; } = new();
        // ⬇⬇⬇⬇⬇ FOR FLV ⬇⬇⬇⬇⬇
        public List<string> Clips { get; set; } = new();
        public List<string> Dfns { get; set; } = new();
    }
}

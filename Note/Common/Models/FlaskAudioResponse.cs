using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Common.Models
{
    public class FlaskAudioResponse
    {
        [JsonPropertyName("transcribed_text")]
        public string TranscribedText { get; set; } = string.Empty;
        [JsonPropertyName("result")]
        public ParsedData Result { get; set; } = null!;
    }
}

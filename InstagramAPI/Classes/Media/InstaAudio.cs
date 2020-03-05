using System;
using Newtonsoft.Json;

namespace InstagramAPI.Classes.Media
{
    public class InstaAudio
    {
        [JsonProperty("audio_src")]
        public Uri AudioSrc { get; set; }

        [JsonProperty("duration")]
        public int Duration { get; set; }

        [JsonProperty("waveform_data")]
        public float[] WaveformData { get; set; }

        [JsonProperty("waveform_sampling_frequency_hz")]
        public int WaveformSamplingFrequencyHz { get; set; }
    }
}
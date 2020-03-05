using InstagramAPI.Classes.JsonConverters;
using InstagramAPI.Classes.Media;
using InstagramAPI.Classes.User;
using Newtonsoft.Json;

namespace InstagramAPI.Classes.Direct.ItemContent
{
    [JsonConverter(typeof(JsonPathConverter))]
    public partial class AnimatedMedia
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("images.fixed_height")]
        public InstaAnimatedImage Image { get; set; }

        [JsonProperty("is_random")]
        public bool IsRandom { get; set; }

        [JsonProperty("is_sticker")]
        public bool IsSticker { get; set; }

        [JsonProperty("user.username")]
        public string Username { get; set; }
    }
}
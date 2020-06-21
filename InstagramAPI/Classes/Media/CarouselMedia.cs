using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using InstagramAPI.Classes.Direct.ItemContent;
using InstagramAPI.Classes.JsonConverters;
using Newtonsoft.Json;

namespace InstagramAPI.Classes.Media
{
    [JsonConverter(typeof(JsonPathConverter))]
    public class CarouselMedia
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("media_type")]
        public long MediaType { get; set; }

        [JsonProperty("image_versions2.candidates")]
        public InstaImage[] ImageCandidates { get; set; }

        [JsonProperty("original_width")]
        public long OriginalWidth { get; set; }

        [JsonProperty("original_height")]
        public long OriginalHeight { get; set; }

        [JsonProperty("pk")]
        public long Pk { get; set; }

        [JsonProperty("carousel_parent_id")]
        public string CarouselParentId { get; set; }

        [JsonProperty("can_see_insights_as_brand")]
        public bool CanSeeInsightsAsBrand { get; set; }

        [JsonProperty("usertags", NullValueHandling = NullValueHandling.Ignore)]
        public Tags Usertags { get; set; }
    }
}

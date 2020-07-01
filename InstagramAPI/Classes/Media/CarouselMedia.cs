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
    public class CarouselMedia : InstaMedia
    {
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

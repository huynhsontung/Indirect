using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using InstagramAPI.Classes.Media;
using Newtonsoft.Json;

namespace InstagramAPI.Classes.User
{
    public class CurrentUser : BaseUser
    {
        [JsonProperty("show_conversion_edit_entry")]
        public bool ShowConversationEditEntry { get; set; }

        [JsonProperty("birthday")] public string Birthday { get; set; }

        [JsonProperty("biography")] public string Biography { get; set; }

        [JsonProperty("phone_number")] public string PhoneNumber { get; set; }

        [JsonProperty("country_code")] public int CountryCode { get; set; }

        [JsonProperty("national_number")] public long NationalNumber { get; set; }

        [JsonProperty("gender")] public GenderType Gender { get; set; }

        [JsonProperty("email")] public string Email { get; set; }

        [JsonProperty("hd_profile_pic_versions")]
        public List<InstaImage> HdProfilePictureVersions { get; set; }

        [JsonProperty("hd_profile_pic_url_info")]
        public InstaImage HdProfilePicture { get; set; }

        [JsonProperty("external_url")] public string ExternalUrl { get; set; }
    }
}

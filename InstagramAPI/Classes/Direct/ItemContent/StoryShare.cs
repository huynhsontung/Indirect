using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace InstagramAPI.Classes.Direct.ItemContent
{
    public class StoryShare : ReelShare
    {
        [JsonProperty("title", NullValueHandling = NullValueHandling.Ignore)]
        public string Title { get; set; }

        [JsonProperty("message", NullValueHandling = NullValueHandling.Ignore)]
        public string Message { get; set; }

        [JsonIgnore]
        public string OwnerUsername
        {
            get
            {
                if (Media != null) return Media.User.Username;
                if (string.IsNullOrEmpty(Title) || !Title.Contains('@')) return null;
                var start = Title.IndexOf('@');
                var end = Title.LastIndexOf('\'');
                return Title.Substring(start + 1, end - start - 1);
            }
        }
    }
}

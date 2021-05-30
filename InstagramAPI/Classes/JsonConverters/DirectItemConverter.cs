using System;
using InstagramAPI.Classes.Direct;
using InstagramAPI.Classes.Media;
using InstagramAPI.Utils;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace InstagramAPI.Classes.JsonConverters
{
    public class DirectItemConverter : JsonConverter<DirectItem>
    {
        public static Instagram InstagramInstance { get; set; }

        public override void WriteJson(JsonWriter writer, DirectItem value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }

        public override DirectItem ReadJson(JsonReader reader, Type objectType, DirectItem existingValue, bool hasExistingValue,
            JsonSerializer serializer)
        {
            for (var i = 0; i < serializer.Converters.Count; i++)
            {
                var converter = serializer.Converters[i];
                if (converter is DirectItemConverter)
                {
                    serializer.Converters.Remove(converter);
                    i--;
                }
            }

            serializer.NullValueHandling = NullValueHandling.Ignore;
            var itemJson = reader.TokenType == JsonToken.String
                ? JObject.Parse((string)reader.Value)
                : JObject.Load(reader);
            var rawJson = itemJson.ToString(Formatting.None);
            var itemSender = itemJson["user_id"]?.ToObject<long>(serializer) ?? 0;
            var viewerPk = InstagramInstance.Session.LoggedInUser.Pk;
            var item = itemJson.ToObject<DirectItem>(serializer);
            item.RawJson = rawJson;
            item.FromMe = itemSender == viewerPk;
            SetDescriptionText(item);

            return item;
        }

        private void SetDescriptionText(DirectItem item)
        {
            try
            {
                switch (item.ItemType)
                {
                    case DirectItemType.ActionLog:
                        item.Description = item.ActionLog.Description;
                        break;

                    case DirectItemType.AnimatedMedia:
                        item.Description = item.FromMe ? "You sent a GIF" : "Sent you a GIF";
                        break;

                    case DirectItemType.Hashtag:
                        item.Description = "#" + item.HashtagMedia.Name;
                        break;

                    case DirectItemType.Like:
                        item.Description = item.Like;
                        break;

                    case DirectItemType.Link:
                        item.Description = item.Link.Text;
                        break;

                    case DirectItemType.Media:
                        if (item.Media.MediaType == InstaMediaType.Image)
                            item.Description = item.FromMe ? "You sent a photo" : "Sent you a photo";
                        else
                            item.Description = item.FromMe ? "You sent a video" : "Sent you a video";
                        break;

                    case DirectItemType.MediaShare:
                        item.Description = item.FromMe ? "You shared a post" : "Shared a post";
                        break;

                    case DirectItemType.RavenMedia:
                        var mediaType = item.RavenMedia?.MediaType ??
                                        item.VisualMedia.Media.MediaType;
                        if (mediaType == InstaMediaType.Image)
                            item.Description = item.FromMe ? "You sent a photo" : "Sent you a photo";
                        else
                            item.Description = item.FromMe ? "You sent a video" : "Sent you a video";
                        break;

                    case DirectItemType.ReelShare:
                        switch (item.ReelShareMedia.Type)
                        {
                            case "reaction":
                                item.Description = item.FromMe
                                    ? $"You reacted to their story {item.ReelShareMedia.Text}"
                                    : $"Reacted to your story {item.ReelShareMedia.Text}";
                                break;
                            case "reply":
                                item.Description = item.FromMe ? "You replied to their story" : "Replied to your story";
                                break;
                            case "mention":
                                item.Description = item.FromMe
                                    ? "You mentioned them in your story"
                                    : "Mentioned you in their story";
                                break;
                        }

                        break;

                    case DirectItemType.StoryShare:
                        item.Description = item.FromMe ? "You sent a story" : "Sent you a story";
                        break;

                    case DirectItemType.Text:
                        item.Description = item.Text;
                        break;

                    case DirectItemType.VoiceMedia:
                        item.Description = item.FromMe ? "You sent a voice clip" : "Sent you a voice clip";
                        break;

                    case DirectItemType.VideoCallEvent:
                        if (item.VideoCallEvent?.Action == "video_call_started")
                        {
                            item.Description = item.FromMe ? "You started a video chat" : "Video chat started";
                        }
                        else
                        {
                            item.Description = "Video chat ended";
                        }
                        break;

                    case DirectItemType.Profile:
                        item.Description = item.FromMe ? "You sent a profile" : "Sent a profile";
                        break;

                    default:
                        item.Description = item.ItemType.ToString();
                        break;
                }
            }
            catch (Exception)
            {
                this.Log($"Failed to write item description. Json: {item.RawJson}");
                // pass
            }
        }
    }
}
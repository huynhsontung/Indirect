using System;
using System.Diagnostics;
using InstagramAPI.Classes.Direct;
using InstagramAPI.Classes.Media;
using InstagramAPI.Utils;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace InstagramAPI.Classes.JsonConverters
{
    public class DirectItemConverter : JsonConverter<DirectItem>
    {
        public override void WriteJson(JsonWriter writer, DirectItem value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }

        public override DirectItem ReadJson(JsonReader reader, Type objectType, DirectItem existingValue, bool hasExistingValue,
            JsonSerializer serializer)
        {
            var itemJson = reader.TokenType == JsonToken.String
                ? JObject.Parse((string) reader.Value)
                : JObject.Load(reader);
            var rawJson = itemJson.ToString(Formatting.None);
            var itemType = itemJson["item_type"]?.ToObject<DirectItemType>(serializer) ?? DirectItemType.Unknown;
            var itemSender = itemJson["user_id"]?.ToObject<long>(serializer) ?? 0;
            var viewerPk = Instagram.Instance.Session.LoggedInUser.Pk;
            var item = itemJson.ToObject<DirectItem>(serializer);
            item.RawJson = rawJson;
            item.FromMe = itemSender == viewerPk;
            try
            {
                switch (itemType)
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
                            item.Description = item.FromMe ? "You sent them a photo" : "Sent you a photo";
                        else
                            item.Description = item.FromMe ? "You sent them a video" : "Sent you a video";
                        break;

                    case DirectItemType.MediaShare:
                        item.Description = item.FromMe ? "You shared a post" : "Shared a post";
                        break;

                    case DirectItemType.RavenMedia:
                        var mediaType = item.RavenMedia?.MediaType ??
                                        item.VisualMedia.Media.MediaType;
                        if (mediaType == InstaMediaType.Image)
                            item.Description = item.FromMe ? "You sent them a photo" : "Sent you a photo";
                        else
                            item.Description = item.FromMe ? "You sent them a video" : "Sent you a video";
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

                    case DirectItemType.Text:
                        item.Description = item.Text;
                        break;

                    case DirectItemType.VoiceMedia:
                        item.Description = item.FromMe ? "You sent a voice clip" : "Sent you a voice clip";
                        break;

                    default:
                        item = itemJson.ToObject<DirectItem>(serializer);
                        item.Description = item.ItemType.ToString();
                        break;
                }
            }
            catch
            {
                Debug.WriteLine($"Failed to write item description. Json: {rawJson}");
                // Not important enough to throw. Pass
            }

            return item;
        }
    }
}
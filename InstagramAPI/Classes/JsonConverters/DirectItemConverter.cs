using System;
using InstagramAPI.Classes.Direct;
using InstagramAPI.Classes.Direct.Items;
using InstagramAPI.Classes.Media;
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
            var itemJson = JObject.Load(reader);
            var rawJson = itemJson.ToString(Formatting.None);
            var itemType = itemJson["item_type"]?.ToObject<DirectItemType>(serializer) ?? DirectItemType.Unknown;
            var itemSender = itemJson["user_id"]?.ToObject<long>(serializer) ?? 0;
            var viewerPk = Instagram.Instance.Session.LoggedInUser.Pk;
            var fromMe = itemSender == viewerPk;
            DirectItem item;
            switch (itemType)
            {
                case DirectItemType.ActionLog:
                    item = itemJson.ToObject<ActionLogItem>(serializer);
                    item.Description = ((ActionLogItem)item).ActionLog.Description;
                    break;

                case DirectItemType.AnimatedMedia:
                    item = itemJson.ToObject<AnimatedMediaItem>(serializer);
                    item.Description = fromMe ? "You sent a GIF" : "Sent you a GIF";
                    break;

                case DirectItemType.Hashtag:
                    item = itemJson.ToObject<HashtagItem>(serializer);
                    item.Description = "#" + ((HashtagItem) item).HashtagMedia.Name;
                    break;

                case DirectItemType.Like:
                    item = itemJson.ToObject<LikeItem>(serializer);
                    item.Description = ((LikeItem) item).Like;
                    break;

                case DirectItemType.Link:
                    item = itemJson.ToObject<LinkItem>(serializer);
                    item.Description = ((LinkItem) item).Link.Text;
                    break;

                case DirectItemType.Media:
                    item = itemJson.ToObject<DirectMediaItem>(serializer);
                    if (((DirectMediaItem) item).Media.MediaType == InstaMediaType.Image)
                        item.Description = fromMe ? "You sent them a photo" : "Sent you a photo";
                    else
                        item.Description = fromMe ? "You sent them a video" : "Sent you a video";
                    break;

                case DirectItemType.MediaShare:
                    item = itemJson.ToObject<MediaShareItem>(serializer);
                    item.Description = fromMe ? "You shared a post" : "Shared a post";
                    break;

                case DirectItemType.RavenMedia:
                    item = itemJson.ToObject<RavenMediaItem>(serializer);
                    var mediaType = ((RavenMediaItem) item).RavenMedia?.MediaType ??
                                    ((RavenMediaItem) item).VisualMedia.Media.MediaType;
                    if (mediaType == InstaMediaType.Image)
                        item.Description = fromMe ? "You sent them a photo" : "Sent you a photo";
                    else
                        item.Description = fromMe ? "You sent them a video" : "Sent you a video";
                    break;

                case DirectItemType.ReelShare:
                    item = itemJson.ToObject<ReelShareItem>(serializer);
                    var reelShareItem = ((ReelShareItem) item);
                    switch (reelShareItem.ReelShareMedia.Type)
                    {
                        case "reaction":
                            item.Description = fromMe
                                ? $"You reacted to their story {reelShareItem.ReelShareMedia.Text}"
                                : $"Reacted to your story {reelShareItem.ReelShareMedia.Text}";
                            break;
                        case "reply":
                            item.Description = fromMe ? "You replied to their story" : "Replied to your story";
                            break;
                        case "mention":
                            item.Description = fromMe ? "You mentioned them in your story" : "Mentioned you in their story";
                            break;
                    }
                    break;

                case DirectItemType.Text:
                    item = itemJson.ToObject<TextItem>(serializer);
                    item.Description = ((TextItem) item).Text;
                    break;

                case DirectItemType.VoiceMedia:
                    item = itemJson.ToObject<VoiceMediaItem>(serializer);
                    item.Description = fromMe ? "You sent a voice clip" : "Sent you a voice clip";
                    break;

                default:
                    item = itemJson.ToObject<DirectItem>(serializer);
                    item.Description = item.ItemType.ToString();
                    break;
            }

            item.RawJson = rawJson;
            item.FromMe = fromMe;
            return item;
        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using InstagramAPI.Classes.Direct;
using InstagramAPI.Sync;
using InstagramAPI.Utils;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace InstagramAPI.Classes.JsonConverters
{
    public class SyncItemConverter : JsonConverter<SyncItem>
    {
        public override void WriteJson(JsonWriter writer, SyncItem value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }

        public override SyncItem ReadJson(JsonReader reader, Type objectType, SyncItem existingValue, bool hasExistingValue,
            JsonSerializer serializer)
        {
            var syncItem = serializer.Deserialize<SyncItem>(reader);
            if (syncItem == null)
            {
                return null;
            }

            if (!string.IsNullOrEmpty(syncItem.Value))
            {
                try
                {
                    if (syncItem.Path.EndsWith("shh_mode_enabled"))
                    {
                        syncItem.ShhModeEnabled = JsonConvert.DeserializeObject<bool?>(syncItem.Value);
                    }
                    else
                    {
                        syncItem.Item = JsonConvert.DeserializeObject<DirectItem>(syncItem.Value);
                    }

                }
                catch (Exception e)
                {
                    var token = JToken.Parse(syncItem.Value);
                    string value;
                    if (token.Type == JTokenType.Object)
                    {
                        var jProps = JObject.Parse(syncItem.Value).Properties().Select(p => p.Name);
                        value = string.Join(",", jProps);
                    }
                    else
                    {
                        value = token.Type.ToString();
                    }

                    DebugLogger.LogException(e, properties: new Dictionary<string, string>
                    {
                        {"Op", syncItem.Op},
                        {"Path", syncItem.Path.StripSensitive()},
                        {"Value", value}
                    });
                }
            }

            return syncItem;
        }
    }
}

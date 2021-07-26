using System;
using InstagramAPI.Realtime;
using Newtonsoft.Json;

namespace InstagramAPI.Classes.JsonConverters
{
    public class ActivityIndicatorDataConverter : JsonConverter<ActivityIndicatorData>
    {
        public override void WriteJson(JsonWriter writer, ActivityIndicatorData value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }

        public override ActivityIndicatorData ReadJson(JsonReader reader, Type objectType, ActivityIndicatorData existingValue,
            bool hasExistingValue, JsonSerializer serializer)
        {
            var data = serializer.Deserialize<ActivityIndicatorData>(reader);
            if (data == null)
            {
                return null;
            }

            if (!string.IsNullOrEmpty(data.Value))
            {
                data.Indicator = JsonConvert.DeserializeObject<ActivityIndicator>(data.Value);
            }

            return data;
        }
    }
}

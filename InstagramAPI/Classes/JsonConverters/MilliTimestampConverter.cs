using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace InstagramAPI.Classes.JsonConverters
{
    class MilliTimestampConverter : JsonConverter<DateTimeOffset>
    {
        public override void WriteJson(JsonWriter writer, DateTimeOffset value, JsonSerializer serializer)
        {
            writer.WriteValue(value.ToUnixTimeMilliseconds());
        }

        public override DateTimeOffset ReadJson(JsonReader reader, Type objectType, DateTimeOffset existingValue, bool hasExistingValue,
            JsonSerializer serializer)
        {
            var unixTime = (string)reader.Value;
            if (string.IsNullOrEmpty(unixTime)) return default;
            return MicroTimestampConverter.ReadTimestampJson(unixTime);
        }
    }
}

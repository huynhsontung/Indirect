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
            if (!(reader.Value is string unixTime))
                unixTime = reader.Value.ToString();
            return string.IsNullOrEmpty(unixTime) ? default : TimestampConverter.ReadTimestampJson(unixTime);
        }
    }
}

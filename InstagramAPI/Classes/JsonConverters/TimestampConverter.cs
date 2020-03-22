using System;
using Newtonsoft.Json;

namespace InstagramAPI.Classes.JsonConverters
{
    class TimestampConverter : JsonConverter
    {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            if (value == null)
            {
                writer.WriteNull();
                return;
            }
            writer.WriteValue(((DateTimeOffset) value).ToUnixTimeSeconds());
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (!(reader.Value is string unixTime))
                unixTime = reader.Value.ToString();
            return string.IsNullOrEmpty(unixTime) ? default : ReadTimestampJson(unixTime);
        }

        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(DateTimeOffset) || objectType == typeof(DateTimeOffset?);
        }

        public static DateTimeOffset ReadTimestampJson(string unixTime)
        {
            var length = unixTime.Length;
            if (length >= 13)
            {
                return DateTimeOffset.FromUnixTimeMilliseconds(long.Parse(unixTime.Substring(0, 13)));
            }
            else if (length >= 10)
            {
                return DateTimeOffset.FromUnixTimeSeconds(long.Parse(unixTime.Substring(0, 10)));
            }
            else
            {
                return default;
            }
        }
    }
}
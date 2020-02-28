using System;
using Newtonsoft.Json;

namespace InstagramAPI.Classes.JsonConverters
{
    class TimestampConverter : JsonConverter<DateTimeOffset>
    {
        public override void WriteJson(JsonWriter writer, DateTimeOffset value, JsonSerializer serializer)
        {
            writer.WriteValue(value.ToUnixTimeSeconds());
        }

        public override DateTimeOffset ReadJson(JsonReader reader, Type objectType, DateTimeOffset existingValue, bool hasExistingValue,
            JsonSerializer serializer)
        {
            if (!(reader.Value is string unixTime))
                unixTime = reader.Value.ToString();
            return string.IsNullOrEmpty(unixTime) ? default : ReadTimestampJson(unixTime);
        }

        public static DateTimeOffset ReadTimestampJson(string unixTime)
        {
            var length = unixTime.Length;
            if (length >= 13)
            {
                var dateTimeOffset =
                    DateTimeOffset.FromUnixTimeMilliseconds(long.Parse(unixTime.Substring(0, 13)));
                return dateTimeOffset.DateTime;
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
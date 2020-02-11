using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace InstagramAPI.Classes.JsonConverters
{
    class MicroTimestampConverter : JsonConverter<DateTimeOffset>
    {
        public override void WriteJson(JsonWriter writer, DateTimeOffset value, JsonSerializer serializer)
        {
            writer.WriteValue(value.ToUnixTimeMilliseconds() + "000");
        }

        public override DateTimeOffset ReadJson(JsonReader reader, Type objectType, DateTimeOffset existingValue, bool hasExistingValue,
            JsonSerializer serializer)
        {
            var unixTime = (string) reader.Value;
            if (string.IsNullOrEmpty(unixTime)) return default;
            return ReadTimestampJson(unixTime);
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

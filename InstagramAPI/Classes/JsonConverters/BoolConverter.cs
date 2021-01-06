using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace InstagramAPI.Classes.JsonConverters
{
    public class BoolConverter : JsonConverter
    {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            writer.WriteValue((bool)value);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var value = reader.Value;
            if (value == null)
            {
                return false;
            }

            if (reader.ValueType == typeof(long) || reader.ValueType == typeof(double) && (double) value == 0)
            {
                return false;
            }

            if (reader.ValueType == typeof(bool) && !(bool) value)
            {
                return false;
            }

            return !string.IsNullOrEmpty(value.ToString());
        }

        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(bool) || objectType == typeof(bool?);
        }
    }
}

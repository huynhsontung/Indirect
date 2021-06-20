using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Net;

namespace InstagramAPI.Classes.JsonConverters
{
    // TODO: To be removed after migration
    public class CookieConverter : JsonConverter<Cookie>
    {
        public override Cookie ReadJson(JsonReader reader, Type objectType, Cookie existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            var jsonObject = JObject.Load(reader);
            var name = jsonObject["Name"]?.Value<string>();
            var value = jsonObject["Value"]?.Value<string>();
            var path = jsonObject["Path"]?.Value<string>();
            var httpCookie = new Cookie(name, value, path)
            {
                Domain = jsonObject["Domain"]?.Value<string>(),
                HttpOnly = jsonObject["HttpOnly"].Value<bool>(),
                Secure = jsonObject["Secure"].Value<bool>()
            };

            if (jsonObject["Expires"]?.Type == JTokenType.Date)
            {
                httpCookie.Expires = jsonObject["Expires"].ToObject<DateTime>(serializer);
            }

            return httpCookie;
        }

        public override void WriteJson(JsonWriter writer, Cookie value, JsonSerializer serializer)
        {
            serializer.Serialize(writer, value);
        }
    }
}

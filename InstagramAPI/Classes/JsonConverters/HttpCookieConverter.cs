using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using Windows.Web.Http;

namespace InstagramAPI.Classes.JsonConverters
{
    public class HttpCookieConverter : JsonConverter<HttpCookie>
    {
        public override HttpCookie ReadJson(JsonReader reader, Type objectType, HttpCookie existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            var jsonObject = JObject.Load(reader);
            var name = jsonObject["Name"]?.Value<string>();
            var domain = jsonObject["Domain"]?.Value<string>();
            var path = jsonObject["Path"]?.Value<string>();
            var httpCookie = new HttpCookie(name, domain, path)
            {
                Expires = jsonObject["Expires"]?.ToObject<DateTimeOffset?>(serializer),
                HttpOnly = jsonObject["HttpOnly"].Value<bool>(),
                Secure = jsonObject["Secure"].Value<bool>(),
                Value = jsonObject["Value"]?.Value<string>()
            };

            return httpCookie;
        }

        public override void WriteJson(JsonWriter writer, HttpCookie value, JsonSerializer serializer)
        {
            serializer.Serialize(writer, value);
        }
    }
}

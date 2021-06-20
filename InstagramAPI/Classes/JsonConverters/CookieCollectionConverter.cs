using System;
using System.Collections.Generic;
using System.Net;
using Newtonsoft.Json;

namespace InstagramAPI.Classes.JsonConverters
{
    public class CookieCollectionConverter : JsonConverter<CookieCollection>
    {
        public override void WriteJson(JsonWriter writer, CookieCollection value, JsonSerializer serializer)
        {
            serializer.Serialize(writer, value);
        }

        public override CookieCollection ReadJson(JsonReader reader, Type objectType, CookieCollection existingValue, bool hasExistingValue,
            JsonSerializer serializer)
        {
            serializer.Converters.Add(new CookieConverter());
            var cookies = serializer.Deserialize<List<Cookie>>(reader);
            var cookieCollection = new CookieCollection();
            if (cookies?.Count > 0)
            {
                foreach (var cookie in cookies)
                {
                    cookieCollection.Add(cookie);
                }
            }

            return cookieCollection;
        }
    }
}

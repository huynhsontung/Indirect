using System;
using Newtonsoft.Json;

namespace InstagramAPI.Fbns
{
    public abstract class BaseConnectionData
    {
        [JsonIgnore]
        protected long ClientMqttSessionId
        {
            get
            {
                var difference = DateTime.Today.DayOfWeek - DayOfWeek.Monday;
                var lastMonday =
                    new DateTimeOffset(DateTime.Today.Subtract(TimeSpan.FromDays(difference > 0 ? difference : 7)));
                return DateTimeOffset.Now.ToUnixTimeMilliseconds() - lastMonday.ToUnixTimeMilliseconds();
            }
        }

        [JsonProperty]
        public string ClientId { get; protected set; } = Guid.NewGuid().ToString().Substring(0, 20);

        [JsonProperty]
        public string UserAgent { get; protected set; }

        [JsonProperty]
        public long UserId { get; protected set; }

        [JsonProperty]
        public string Password { get; protected set; }

        [JsonProperty]
        public string DeviceId { get; protected set; }

        [JsonProperty]
        public string DeviceSecret { get; protected set; }

        public abstract ConnectPayload ToPayload();
    }
}

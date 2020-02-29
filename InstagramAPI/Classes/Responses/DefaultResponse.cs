using Newtonsoft.Json;

namespace InstagramAPI.Classes.Responses
{
    public class DefaultResponse : BaseStatusResponse
    {
        [JsonProperty("status_code")] public string StatusCode { get; set; }
        [JsonProperty("message")] public string Message { get; set; }
    }
}
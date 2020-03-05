using Newtonsoft.Json;

namespace InstagramAPI.Classes.Responses
{
    public class RuploadResponse : BaseStatusResponse
    {
        [JsonProperty("upload_id")] public string UploadId { get; set; }
    }
}
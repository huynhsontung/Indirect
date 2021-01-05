using InstagramAPI.Classes.Challenge;
using Newtonsoft.Json;

namespace InstagramAPI.Classes.Responses
{
    internal class ChallengeStepResponse : BaseStatusResponse
    {
        [JsonProperty("step_name")]
        public string StepName { get; set; }

        [JsonProperty("step_data")]
        public ChallengeSelectMethodStep StepData { get; set; }

        [JsonProperty("flow_render_type")]
        public long FlowRenderType { get; set; }

        [JsonProperty("bloks_action")]
        public string BloksAction { get; set; }

        [JsonProperty("nonce_code")]
        public string NonceCode { get; set; }

        [JsonProperty("user_id")]
        public long UserId { get; set; }

        [JsonProperty("challenge_context")]
        public string ChallengeContext { get; set; }

        [JsonProperty("challenge_type_enum_str")]
        public string ChallengeTypeEnumStr { get; set; }
    }
}

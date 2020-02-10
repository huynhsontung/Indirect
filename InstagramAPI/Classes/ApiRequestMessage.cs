using System;
using InstagramAPI.Utils;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace InstagramAPI.Classes
{
    internal class ApiRequestChallengeMessage : ApiRequestMessage
    {
        public ApiRequestChallengeMessage(Instagram instaApi) : base(instaApi) {}

        [JsonProperty("_csrftoken")]
        public string CsrtToken { get; set; }
    }

    internal class ApiRequestMessage
    {
        private readonly Instagram _instaApi;

        [JsonProperty("country_codes")] public JRaw CountryCodes { get; set; } = new JRaw("[{\"country_code\":\"1\",\"source\":[\"default\",\"sim\"]}]");
        [JsonProperty("phone_id")] public string PhoneId => _instaApi.Device.PhoneId.ToString();
        [JsonProperty("username")] public string Username => _instaApi.Session.Username;
        [JsonProperty("adid")] public string AdId => _instaApi.Device.AdId.ToString();
        [JsonProperty("guid")] public Guid Guid => _instaApi.Device.Uuid;
        [JsonProperty("_uuid")] public string Uuid => Guid.ToString();
        [JsonProperty("device_id")] public string DeviceId => _instaApi.Device.DeviceId;
        [JsonProperty("password")] public string Password => _instaApi.Session.Password;
        [JsonProperty("login_attempt_count")] public string LoginAttemptCount { get; set; } = "0";

        public ApiRequestMessage(Instagram instaApi)
        {
            _instaApi = instaApi;
        }

        internal string GetMessageString()
        {
            var json = JsonConvert.SerializeObject(this);
            return json;
        }

        internal string GetChallengeMessageString(string csrfToken)
        {
            var api = new ApiRequestChallengeMessage(_instaApi)
            {
                CsrtToken = csrfToken,
                LoginAttemptCount = "1"
            };
            var json = JsonConvert.SerializeObject(api);
            return json;
        }

        internal string GetMessageStringForChallengeVerificationCodeSend(int Choice = 1)
        {
            return JsonConvert.SerializeObject(new { choice = Choice.ToString(), _csrftoken = "ReplaceCSRF", Guid, DeviceId });
        }

        internal string GetChallengeVerificationCodeSend(string verify)
        {
            return JsonConvert.SerializeObject(new { security_code = verify, _csrftoken = "ReplaceCSRF", Guid, DeviceId });
        }

        internal string GenerateSignature(ApiVersion apiVersion, string signatureKey, out string deviceid)
        {
            if (string.IsNullOrEmpty(signatureKey))
                signatureKey = apiVersion.SignatureKey;
            var res = CryptoHelper.CalculateHash(signatureKey,
                JsonConvert.SerializeObject(this));
            deviceid = DeviceId;
            return res;
        }

        internal string GenerateChallengeSignature(ApiVersion apiVersion, string signatureKey, string csrfToken, out string deviceid)
        {
            if (string.IsNullOrEmpty(signatureKey))
                signatureKey = apiVersion.SignatureKey;
            var api = new ApiRequestChallengeMessage(_instaApi)
            {
                CsrtToken = csrfToken,
                LoginAttemptCount = "1"
            };
            var res = CryptoHelper.CalculateHash(signatureKey,
                JsonConvert.SerializeObject(api));
            deviceid = DeviceId;
            return res;
        }

        internal bool IsEmpty()
        {
            if (string.IsNullOrEmpty(PhoneId) || string.IsNullOrEmpty(DeviceId) || Guid.Empty == Guid) return true;
            return false;
        }

        internal static string GenerateDeviceId()
        {
            return GenerateDeviceIdFromGuid(Guid.NewGuid());
        }

        internal static string GenerateUploadId(bool longId = false)
        {
            return longId
                ? DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString()
                : DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
        }

        public static string GenerateDeviceIdFromGuid(Guid guid)
        {
            var hashedGuid = CryptoHelper.CalculateMd5(guid.ToString());
            return $"android-{hashedGuid.Substring(0, 16)}";
        }
    }
}

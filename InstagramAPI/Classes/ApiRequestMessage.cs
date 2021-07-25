using System;
using Windows.Security.Cryptography;
using Windows.Security.Cryptography.Core;
using Windows.Storage.Streams;
using InstagramAPI.Classes.Core;
using InstagramAPI.Utils;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace InstagramAPI.Classes
{
    internal class ApiRequestChallengeMessage : ApiRequestMessage
    {
        public ApiRequestChallengeMessage(UserSessionData session) : base(session) {}

        [JsonProperty("_csrftoken")]
        public string CsrtToken { get; set; }
    }

    internal class ApiRequestMessage
    {
        private readonly UserSessionData _session;

        [JsonProperty("country_codes")] public JRaw CountryCodes { get; set; } = new JRaw("[{\"country_code\":\"1\",\"source\":[\"default\",\"sim\"]}]");
        [JsonProperty("phone_id")] public string PhoneId => _session.Device.PhoneId.ToString();
        [JsonProperty("username")] public string Username => _session.Username;
        [JsonProperty("adid")] public string AdId => _session.Device.AdId.ToString();
        [JsonProperty("guid")] public Guid Guid => _session.Device.Uuid;
        [JsonProperty("_uuid")] public string Uuid => Guid.ToString();
        [JsonProperty("device_id")] public string DeviceId => _session.Device.DeviceId;
        [JsonProperty("enc_password")] public string Password => EncodePassword(_session.Password);
        [JsonProperty("login_attempt_count")] public string LoginAttemptCount { get; set; } = "0";
        [JsonProperty("google_tokens")] public string[] GoogleTokens { get; set; } = new string[0];

        public ApiRequestMessage(UserSessionData session)
        {
            _session = session;
        }

        internal string EncodePassword(string plainPassword)
        {
            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
            var timestampBuffer = CryptographicBuffer.ConvertStringToBinary(timestamp, BinaryStringEncoding.Utf8);
            var passwordBuffer = CryptographicBuffer.ConvertStringToBinary(plainPassword, BinaryStringEncoding.Utf8);
            var aesKey = CryptographicBuffer.GenerateRandom(32);
            var iv = CryptographicBuffer.GenerateRandom(12);
            var pubKey = CryptographicBuffer.ConvertBinaryToString(BinaryStringEncoding.Utf8, _session.PasswordEncryptionPubKey);
            var pubKeyBuffer = CryptoHelper.ConvertPemToDer(pubKey);

            var rsaProvider = AsymmetricKeyAlgorithmProvider.OpenAlgorithm(AsymmetricAlgorithmNames.RsaPkcs1);
            var rsaPubKey = rsaProvider.ImportPublicKey(pubKeyBuffer);
            var aesKeyEncrypted = CryptographicEngine.Encrypt(rsaPubKey, aesKey, null);

            var aesGcmProvider = SymmetricKeyAlgorithmProvider.OpenAlgorithm(SymmetricAlgorithmNames.AesGcm);
            var aesKeyMaterial = aesGcmProvider.CreateSymmetricKey(aesKey);
            var encodedPassword = CryptographicEngine.EncryptAndAuthenticate(aesKeyMaterial,
                passwordBuffer, iv, timestampBuffer);

            using (var writer = new DataWriter())
            {
                writer.ByteOrder = ByteOrder.LittleEndian;
                writer.WriteByte(1);
                writer.WriteByte(_session.PasswordEncryptionKeyId);
                writer.WriteBuffer(iv);
                writer.WriteInt16((short) aesKeyEncrypted.Length);
                writer.WriteBuffer(aesKeyEncrypted);
                writer.WriteBuffer(encodedPassword.AuthenticationTag);
                writer.WriteBuffer(encodedPassword.EncryptedData);
                var buffer = writer.DetachBuffer();
                var encoded = CryptographicBuffer.EncodeToBase64String(buffer);
                return $"#PWD_INSTAGRAM:4:{timestamp}:{encoded}";
            }
        }

        internal static string GetChallengeMessageString(UserSessionData session, string csrtToken)
        {
            var api = new ApiRequestChallengeMessage(session)
            {
                CsrtToken = csrtToken,
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
            var api = new ApiRequestChallengeMessage(_session)
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

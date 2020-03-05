using Newtonsoft.Json;

namespace InstagramAPI.Classes
{
    public class TwoFactorLoginInfo
    {
        [JsonProperty("obfuscated_phone_number")]
        public string ObfuscatedPhoneNumber { get; set; }

        [JsonProperty("show_messenger_code_option")]
        public bool? ShowMessengerCodeOption { get; set; }

        [JsonProperty("two_factor_identifier")]
        public string TwoFactorIdentifier { get; set; }

        [JsonProperty("username")] public string Username { get; set; }

        [JsonProperty("phone_verification_settings")]
        public PhoneVerificationSettings PhoneVerificationSettings { get; set; }

        public static TwoFactorLoginInfo Empty => new TwoFactorLoginInfo();
    }

    public class PhoneVerificationSettings
    {
        [JsonProperty("max_sms_count")] public string MaxSmsCount { get; set; }

        [JsonProperty("resend_sms_delay_sec")] public int? ResendSmsDelaySeconds { get; set; }

        [JsonProperty("robocall_after_max_sms")]
        public bool? RobocallAfterMaxSms { get; set; }

        [JsonProperty("robocall_count_down_time")]
        public int? RobocallCountDownTime { get; set; }
    }
}

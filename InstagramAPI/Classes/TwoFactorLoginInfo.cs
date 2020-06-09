using Newtonsoft.Json;

namespace InstagramAPI.Classes
{
    public class TwoFactorLoginInfo
    {
        [JsonProperty("username")]
        public string Username { get; set; }

        [JsonProperty("sms_two_factor_on")]
        public bool SmsTwoFactorOn { get; set; }

        [JsonProperty("totp_two_factor_on")]
        public bool TotpTwoFactorOn { get; set; }

        [JsonProperty("obfuscated_phone_number")]
        public string ObfuscatedPhoneNumber { get; set; }

        [JsonProperty("two_factor_identifier")]
        public string TwoFactorIdentifier { get; set; }

        [JsonProperty("show_messenger_code_option")]
        public bool ShowMessengerCodeOption { get; set; }

        [JsonProperty("show_new_login_screen")]
        public bool ShowNewLoginScreen { get; set; }

        [JsonProperty("show_trusted_device_option")]
        public bool ShowTrustedDeviceOption { get; set; }

        [JsonProperty("phone_verification_settings")]
        public PhoneVerificationSettings PhoneVerificationSettings { get; set; }
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

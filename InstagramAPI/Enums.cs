namespace InstagramAPI
{
    public enum LoginResult
    {
        Success,
        BadPassword,
        InvalidUser,
        TwoFactorRequired,
        Exception,
        ChallengeRequired,
        LimitError,
        InactiveUser,
        CheckpointLoggedOut,

        // Two-Factor Login
        InvalidCode, //sms_code_validation_code_invalid
        CodeExpired, //invalid_nonce
    }

    public enum GenderType
    {
        //Gender (1 = male, 2 = female, 3 = unknown)
        Male = 1,
        Female = 2,
        Unknown = 3
    }

    public enum AnimatedImageType
    {
        Sticker,
        Gif,
    }
}

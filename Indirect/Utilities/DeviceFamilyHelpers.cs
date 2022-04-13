using Windows.System.Profile;

namespace Indirect.Utilities
{
    internal static class DeviceFamilyHelpers
    {
        private static readonly string DeviceFamily = AnalyticsInfo.VersionInfo.DeviceFamily;

        public static bool IsXbox => DeviceFamily == "Windows.Xbox";
        public static bool IsDesktop => DeviceFamily == "Windows.Desktop";
        public static bool IsIoT => DeviceFamily == "Windows.IoT";
        public static bool IsTeam => DeviceFamily == "Windows.Team";
        public static bool IsHolographic => DeviceFamily == "Windows.Holographic";

        public static bool MultipleViewsSupport => IsDesktop || IsHolographic;
    }
}

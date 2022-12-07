using System;
using System.Linq;
using Windows.ApplicationModel;
using Microsoft.AppCenter;
using Microsoft.AppCenter.Analytics;
using Microsoft.AppCenter.Crashes;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace InstagramAPI
{
    public partial class Instagram
    {
        private void ValidateLoggedIn()
        {
            if (!IsUserAuthenticated)
            {
                throw new ArgumentException("user must be authenticated");
            }
        }

        private static string GenerateRandomString(int length)
        {
            var rand = new Random();
            const string pool = "abcdefghijklmnopqrstuvwxyz0123456789";
            var result = "";
            for (var i = 0; i < length; i++)
            {
                result += pool[rand.Next(0, pool.Length)];
            }

            return result;
        }

        private static string GetRetryContext()
        {
            return new JObject
            {
                {"num_step_auto_retry", 0},
                {"num_reupload", 0},
                {"num_step_manual_retry", 0}
            }.ToString(Formatting.None);
        }

        public static string GetCurrentLocale()
        {
            var runtimeLanguages = Windows.Globalization.ApplicationLanguages.Languages;
            return runtimeLanguages.FirstOrDefault()?.Replace('-', '_');
        }

        public static void StartAppCenter()
        {
#if !DEBUG
            AppCenter.Start(APPCENTER_SECRET, typeof(Analytics));
#endif
        }

        public static IDisposable StartSentry()
        {
            PackageId id = Package.Current.Id;
            string release =
                $"{id.Name}@{id.Version.Major}.{id.Version.Minor}.{id.Version.Build}.{id.Version.Revision}";
            return Sentry.SentrySdk.Init(o =>
            {
                // Tells which project in Sentry to send events to:
                o.Dsn = SENTRY_DSN;
                o.Release = release;

#if DEBUG
                o.Debug = true;
#endif

                // Set traces_sample_rate to 1.0 to capture 100% of transactions for performance monitoring.
                // We recommend adjusting this value in production.
                //o.TracesSampleRate = 1.0;

                // Enable Global Mode since this is a client app.
                o.IsGlobalModeEnabled = true;
            });
        }
    }
}

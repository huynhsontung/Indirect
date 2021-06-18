using System.Collections.Generic;
using Windows.Storage;

namespace Indirect.Services
{
    internal class SettingsService
    {
        private static readonly ApplicationDataContainer LocalSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
        private readonly MainViewModel _viewModel;

        public SettingsService(MainViewModel viewModel)
        {
            _viewModel = viewModel;
        }

        public static bool TryGetGlobal<T>(string key, out T value)
        {
            var result = LocalSettings.Values.TryGetValue(key, out var obj);
            value = (T) obj;
            return result;
        }

        public bool TryGetForUser<T>(string key, out T value)
        {
            var userContainer = LocalSettings.CreateContainer(_viewModel.ActiveSession.SessionName,
                ApplicationDataCreateDisposition.Always);
            var result = userContainer.Values.TryGetValue(key, out var obj);
            value = (T) obj;
            return result;
        }

        public static void SetGlobal(string key, object value)
        {
            LocalSettings.Values[key] = value;
        }

        public void SetForUser(string key, object value)
        {
            var userContainer = LocalSettings.CreateContainer(_viewModel.ActiveSession.SessionName,
                ApplicationDataCreateDisposition.Always);
            userContainer.Values[key] = value;
        }

        public Dictionary<string, object> GetGlobalSettings()
        {
            return new Dictionary<string, object>(LocalSettings.Values);
        }

        public Dictionary<string, object> GetUserSettings()
        {
            var userContainer = LocalSettings.CreateContainer(_viewModel.ActiveSession.SessionName,
                ApplicationDataCreateDisposition.Always);
            return new Dictionary<string, object>(userContainer.Values);
        }
    }
}

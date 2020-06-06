using System;
using System.Collections.Generic;
using InstagramAPI.Classes.User;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace InstagramAPI
{
    public partial class Instagram
    {
        private void ValidateUser()
        {
            if ((string.IsNullOrEmpty(Session.Username) || string.IsNullOrEmpty(Session.Password)) &&
                string.IsNullOrEmpty(Session.FacebookAccessToken))
                throw new ArgumentException("user name and password or access token must be specified");
        }

        private void ValidateLoggedIn()
        {
            try
            {
                ValidateUser();
                if (!IsUserAuthenticated)
                    throw new ArgumentException("user must be authenticated");
            }
            catch (ArgumentException)
            {
                // Saved data may be corrupted. Force logout.
                IsUserAuthenticated = false;
                SaveToAppSettings();
                throw;
            }
        }

        private void ValidateRequestMessage()
        {
            if (_apiRequestMessage == null || _apiRequestMessage.IsEmpty())
                throw new ArgumentException("API request message null or empty");
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

        private void AddToUserRegistry(IEnumerable<BaseUser> users)
        {
            foreach (var user in users)
            {
                CentralUserRegistry[user.Pk] = user;
            }
        }
    }
}

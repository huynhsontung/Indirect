using System;

namespace InstagramAPI
{
    public partial class Instagram
    {
        private void ValidateUser()
        {
            if (string.IsNullOrEmpty(Session.Username) || string.IsNullOrEmpty(Session.Password))
                throw new ArgumentException("user name and password must be specified");
        }

        private void ValidateLoggedIn()
        {
            ValidateUser();
            if (!IsUserAuthenticated)
                throw new ArgumentException("user must be authenticated");
        }

        private void ValidateRequestMessage()
        {
            if (_apiRequestMessage == null || _apiRequestMessage.IsEmpty())
                throw new ArgumentException("API request message null or empty");
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using InstagramAPI.Classes.Android;
using InstagramAPI.Push;

namespace InstagramAPI.Classes
{
    [Serializable]
    public class StateData
    {
        public AndroidDevice Device { get; internal set; }

        public UserSessionData Session { get; internal set; }

        public bool IsAuthenticated { get; internal set; }

        // public CookieContainer Cookies { get; set; }     // Cookies are managed by UWP

        public FbnsConnectionData FbnsConnectionData { get; internal set; }

    }
}

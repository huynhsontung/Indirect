using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InstagramAPI.Classes.Core
{
    public struct UserSessionMetadata
    {
        public string Username { get; set; }

        public Uri ProfilePicture { get; set; }
    }
}

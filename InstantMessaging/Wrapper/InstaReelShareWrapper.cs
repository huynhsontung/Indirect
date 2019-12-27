using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using InstaSharper.API;
using InstaSharper.Classes.Models.Story;

namespace InstantMessaging.Wrapper
{
    class InstaReelShareWrapper : InstaReelShare
    {
        public new InstaStoryItem Media { get; set; }

        public InstaReelShareWrapper(InstaReelShare source, IInstaApi api)
        {
            // todo: finish ReelShareWrapper
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using InstaSharper.API;
using InstaSharper.Classes.Models.Story;

namespace InstantMessaging.Wrapper
{
    class InstaStoryShareWrapper : InstaStoryShare
    {
        public new InstaMediaWrapper Media { get; set; }

        public InstaStoryShareWrapper(InstaStoryShare source, IInstaApi api)
        {
            Media = new InstaMediaWrapper(source.Media, api);
            ReelType = source.ReelType;
            IsReelPersisted = source.IsReelPersisted;
            Text = source.Text;
            IsLinked = source.IsLinked;
            Title = source.Title;
            Message = source.Message;
        }
    }
}

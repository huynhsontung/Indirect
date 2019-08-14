using InstaSharper.Classes.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Media.Imaging;
using InstaSharper.API;
using InstaSharper.Classes.Models.Direct;
using InstaSharper.Classes.Models.User;

namespace InstantMessaging.Wrapper
{
    /// Wrapper of <see cref="InstaDirectInboxThread"/> with Observable lists
    public class InstaDirectInboxThreadWrapper : InstaDirectInboxThread
    {
        private IInstaApi _instaApi;

        public ObservableCollection<InstaDirectInboxItem> ObservableItems { get; set; } = new ObservableCollection<InstaDirectInboxItem>();
        public new List<InstaUserShortFriendshipWrapper> Users { get; } = new List<InstaUserShortFriendshipWrapper>();

        public InstaDirectInboxThreadWrapper(InstaDirectInboxThread source, IInstaApi api)
        {
            _instaApi = api;
            Canonical = source.Canonical;
            HasNewer = source.HasNewer;
            HasOlder = source.HasOlder;
            IsSpam = source.IsSpam;
            Muted = source.Muted;
            Named = source.Named;
            Pending = source.Pending;
            ViewerId = source.ViewerId;
            LastActivity = source.LastActivity;
            ThreadId = source.ThreadId;
            OldestCursor = source.OldestCursor;
            NewestCursor = source.NewestCursor;
            ThreadType = source.ThreadType;
            Title = source.Title;
            Inviter = source.Inviter;
            LastPermanentItem = source.LastPermanentItem;
            HasNewer = source.HasNewer;
            HasOlder = source.HasOlder;
            HasUnreadMessage = source.HasUnreadMessage;
            foreach (var instaUserShortFriendship in source.Users)
            {
                var user = new InstaUserShortFriendshipWrapper(instaUserShortFriendship, api);
                Users.Add(user);
            }

            UpdateItemList(source.Items);
        }

        public void Update(InstaDirectInboxThread source)
        {
            Canonical = source.Canonical;
            HasNewer = source.HasNewer;
            HasOlder = source.HasOlder;
            IsSpam = source.IsSpam;
            Muted = source.Muted;
            Named = source.Named;
            Pending = source.Pending;
            ViewerId = source.ViewerId;
            LastActivity = source.LastActivity;
            ThreadId = source.ThreadId;
            OldestCursor = source.OldestCursor;
            NewestCursor = source.NewestCursor;
            ThreadType = source.ThreadType;
            Title = source.Title;
            Inviter = source.Inviter;
            LastPermanentItem = source.LastPermanentItem;
            HasNewer = source.HasNewer;
            HasOlder = source.HasOlder;
            HasUnreadMessage = source.HasUnreadMessage;
        }

        public void UpdateItemList(IEnumerable<InstaDirectInboxItem> source)
        {
            if (ObservableItems.Count == 0)
            {
                foreach (var item in source)
                    ObservableItems.Add(item);
            }
            else
            {
                var olderItems = new List<InstaDirectInboxItem>();

                foreach (var item in source)
                {
                    var existing = ObservableItems.IndexOf(item);
                    if (existing == -1)
                    {
                        if (DateTime.Compare(item.TimeStamp, ObservableItems[ObservableItems.Count - 1].TimeStamp) > 0)
                        {
                            ObservableItems.Add(item);
                        }
                        else
                        {
                            olderItems.Add(item);
                        }
                    }
                    else
                    {
                        ObservableItems[existing] = item;
                    }
                }

                olderItems.Reverse();
                foreach (var item in olderItems)
                {
                    ObservableItems.Insert(0, item);
                }
            }
        }
    }
}

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
using System.ComponentModel;
using System.Threading;
using Windows.System;
using InstaSharper.Classes;
using InstaSharper.Helpers;
using Microsoft.Toolkit.Collections;

namespace InstantMessaging.Wrapper
{
    /// Wrapper of <see cref="InstaDirectInboxThread"/> with Observable lists
    class InstaDirectInboxThreadWrapper : InstaDirectInboxThread, INotifyPropertyChanged, IIncrementalSource<InstaDirectInboxItem>
    {
        private IInstaApi _instaApi;

        public event PropertyChangedEventHandler PropertyChanged;
        public ReversedIncrementalLoadingCollection<InstaDirectInboxThreadWrapper, InstaDirectInboxItem> ObservableItems { get; set; }
        public new List<InstaUserShortFriendshipWrapper> Users { get; } = new List<InstaUserShortFriendshipWrapper>();

        public InstaDirectInboxThreadWrapper(InstaDirectInboxThread source, IInstaApi api)
        {
            ObservableItems = new ReversedIncrementalLoadingCollection<InstaDirectInboxThreadWrapper, InstaDirectInboxItem>(this);
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
            IsGroup = source.IsGroup;
            IsPin = source.IsPin;
            ValuedRequest = source.ValuedRequest;
            PendingScore = source.PendingScore;
            VCMuted = source.VCMuted;
            ReshareReceiveCount = source.ReshareReceiveCount;
            ReshareSendCount = source.ReshareSendCount;
            ExpiringMediaReceiveCount = source.ExpiringMediaReceiveCount;
            ExpiringMediaSendCount = source.ExpiringMediaSendCount;
            NewestCursor = source.NewestCursor;
            ThreadType = source.ThreadType;
            Title = source.Title;
            MentionsMuted = source.MentionsMuted;

            Inviter = source.Inviter;
            LastPermanentItem = source.LastPermanentItem;
            LeftUsers = source.LeftUsers;
            LastSeenAt = source.LastSeenAt;
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
            UpdateExcludeItemList(source);
            UpdateItemList(source.Items);
        }

        private void UpdateExcludeItemList(InstaDirectInboxThread source)
        {
            Canonical = source.Canonical;
            //HasNewer = source.HasNewer;
            //HasOlder = source.HasOlder;
            IsSpam = source.IsSpam;
            Muted = source.Muted;
            Named = source.Named;
            Pending = source.Pending;
            ViewerId = source.ViewerId;
            LastActivity = source.LastActivity;
            ThreadId = source.ThreadId;
            IsGroup = source.IsGroup;
            IsPin = source.IsPin;
            ValuedRequest = source.ValuedRequest;
            PendingScore = source.PendingScore;
            VCMuted = source.VCMuted;
            ReshareReceiveCount = source.ReshareReceiveCount;
            ReshareSendCount = source.ReshareSendCount;
            ExpiringMediaReceiveCount = source.ExpiringMediaReceiveCount;
            ExpiringMediaSendCount = source.ExpiringMediaSendCount;
            ThreadType = source.ThreadType;
            Title = source.Title;
            MentionsMuted = source.MentionsMuted;

            Inviter = source.Inviter;
            LastPermanentItem = source.LastPermanentItem.TimeStamp > LastPermanentItem.TimeStamp ?
                source.LastPermanentItem : LastPermanentItem;
            LeftUsers = source.LeftUsers;
            LastSeenAt = source.LastSeenAt;
            HasUnreadMessage = source.HasUnreadMessage;

            if (string.Compare(OldestCursor, source.OldestCursor, StringComparison.Ordinal) > 0)
            {
                OldestCursor = source.OldestCursor;
                HasOlder = source.HasOlder;
            }

            if (string.Compare(NewestCursor, source.NewestCursor, StringComparison.Ordinal) < 0)
            {
                NewestCursor = source.NewestCursor;
                HasNewer = HasNewer;
            }

            UpdateUserList(source.Users);
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(string.Empty));
        }

        private void UpdateItemList(ICollection<InstaDirectInboxItem> source)
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
                }

                olderItems.Reverse();
                foreach (var item in olderItems)
                {
                    ObservableItems.Insert(0, item);
                }
            }
        }

        private void UpdateUserList(List<InstaUserShortFriendship> users)
        {
            var toBeAdded = users.Where(p2 => Users.All(p1 => !p1.Equals(p2)));
            var toBeDeleted = Users.Where(p1 => users.All(p2 => !p1.Equals(p2)));
            Users.AddRange(toBeAdded.Select(x => new InstaUserShortFriendshipWrapper(x, _instaApi)));
            foreach (var user in toBeDeleted)
            {
                Users.Remove(user);
            }
        }

        public async Task LoadOlderItems()
        {
            var pagination = PaginationParameters.MaxPagesToLoad(1);
            pagination.StartFromMaxId(OldestCursor);
            var result = await _instaApi.MessagingProcessor.GetDirectInboxThreadAsync(ThreadId, pagination);
            if (result.Succeeded)
            {
                Update(result.Value);
            }
        }

        public async Task<IEnumerable<InstaDirectInboxItem>> GetPagedItemsAsync(int pageIndex, int pageSize, CancellationToken cancellationToken = new CancellationToken())
        {
            var pagesToLoad = pageSize / 20;
            if (pagesToLoad < 1) pagesToLoad = 1;
            var pagination = PaginationParameters.MaxPagesToLoad(pagesToLoad);
            pagination.StartFromMaxId(OldestCursor);
            var result = await _instaApi.MessagingProcessor.GetDirectInboxThreadAsync(ThreadId, pagination);
            if (!result.Succeeded) return new List<InstaDirectInboxItem>();
            UpdateExcludeItemList(result.Value);
            return result.Value.Items;
        }
    }
}

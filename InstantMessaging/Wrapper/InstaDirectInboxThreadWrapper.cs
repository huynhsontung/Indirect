using InstaSharper.Classes.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InstantMessaging.Wrapper
{
    /// Wrapper of <see cref="InstaDirectInboxThread"/> with Observable lists
    public class InstaDirectInboxThreadWrapper
    {
        public ObservableCollection<InstaDirectInboxItem> ObservableItems { get; } = new ObservableCollection<InstaDirectInboxItem>();
        public bool Muted { get; set; }
        public List<InstaUserShort> Users { get; set; }
        public string Title { get; set; }

        public string OldestCursor { get; set; }
        public string NewestCursor { get; set; }
        public string NextCursor { get; set; }
        public string PrevCursor { get; set; }

        public DateTime LastActivity { get; set; }

        public long ViewerId { get; set; }
        public string ThreadId { get; set; }
        public string ThreadV2Id { get; set; }
        public bool HasOlder { get; set; }

        public InstaUserShort Inviter { get; set; }
        public bool Named { get; set; }
        public bool Pending { get; set; }

        public bool Canonical { get; set; }

        public bool HasNewer { get; set; }


        public bool IsSpam { get; set; }


        public InstaDirectThreadType ThreadType { get; set; }
        public InstaDirectInboxItem LastPermanentItem { get; set; }

        public InstaDirectInboxThreadWrapper(InstaDirectInboxThread source)
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
            NextCursor = source.NextCursor;
            PrevCursor = source.PrevCursor;
            ThreadType = source.ThreadType;
            Title = source.Title;
            Inviter = source.Inviter;
            LastPermanentItem = source.LastPermanentItem;
            Users = source.Users;

            UpdateItemList(source.Items);
        }

        public void UpdateItemList(List<InstaDirectInboxItem> source)
        {
            if(ObservableItems.Count == 0)
            {
                source.Reverse();
                foreach (var item in source)
                    ObservableItems.Add(item);
            }
            else
            {
                foreach (var item in source)
                {
                    if (!ObservableItems.Contains(item))
                    {
                        if (DateTime.Compare(item.TimeStamp, ObservableItems[ObservableItems.Count - 1].TimeStamp) > 0)
                            ObservableItems.Add(item);
                        else
                            ObservableItems.Insert(0, item);
                    }
                }
                
            }
        }
    }
}

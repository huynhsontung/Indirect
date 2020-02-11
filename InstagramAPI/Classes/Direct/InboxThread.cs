using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using InstagramAPI.Classes.JsonConverters;
using InstagramAPI.Classes.User;
using Newtonsoft.Json;

namespace InstagramAPI.Classes.Direct
{
    public class InboxThread : IEquatable<InboxThread>
    {
        [JsonProperty("muted")] 
        public bool Muted { get; set; }

        [JsonProperty("users")] 
        public List<UserShortFriendship> Users { get; set; } = new List<UserShortFriendship>();

        [JsonProperty("thread_title")] 
        public string Title { get; set; }

        [JsonProperty("oldest_cursor")] 
        public string OldestCursor { get; set; }

        [JsonProperty("last_activity_at")]
        [JsonConverter(typeof(MicroTimestampConverter))]
        public DateTimeOffset LastActivity { get; set; }

        [JsonProperty("last_non_sender_item_at")]
        [JsonConverter(typeof(MicroTimestampConverter))]
        public DateTimeOffset LastNonSenderItemAt { get; set; }

        [JsonProperty("viewer_id")]
        public long ViewerId { get; set; }

        [JsonProperty("thread_id")]
        public string ThreadId { get; set; }

        [JsonProperty("has_older")]
        public bool? HasOlder { get; set; }

        [JsonProperty("inviter")]
        public UserShort Inviter { get; set; }

        [JsonProperty("named")]
        public bool Named { get; set; }

        [JsonProperty("pending")]
        public bool Pending { get; set; }

        [JsonProperty("canonical")]
        public bool Canonical { get; set; }

        [JsonProperty("has_newer")]
        public bool? HasNewer { get; set; }

        [JsonProperty("is_spam")]
        public bool IsSpam { get; set; }

        [JsonProperty("thread_type")]
        public ThreadType ThreadType { get; set; }

        [JsonProperty("items")]
        public List<InstaDirectInboxItem> Items { get; set; }

        [JsonProperty("last_permanent_item")]
        public InstaDirectInboxItem LastPermanentItem { get; set; }

        [JsonProperty("is_pin")]
        public bool IsPin { get; set; }

        [JsonProperty("valued_request")]
        public bool ValuedRequest { get; set; }

        /// <summary>
        /// Media upload id. Example: /rupload_igphoto/direct_{PendingScore}
        /// </summary>
        [JsonProperty("pending_score")]
        public long? PendingScore { get; set; }

        [JsonProperty("vc_muted")]
        public bool VCMuted { get; set; }

        [JsonProperty("is_group")]
        public bool IsGroup { get; set; }

        [JsonProperty("reshare_send_count")]
        public int ReshareSendCount { get; set; }

        [JsonProperty("reshare_receive_count")]
        public int ReshareReceiveCount { get; set; }

        [JsonProperty("expiring_media_send_count")]
        public int ExpiringMediaSendCount { get; set; }

        [JsonProperty("expiring_media_receive_count")]
        public int ExpiringMediaReceiveCount { get; set; }

        [JsonProperty("last_seen_at")]
        public Dictionary<long, LastSeen> LastSeenAt { get; set; }

        [JsonProperty("left_users")]
        public List<UserShortFriendship> LeftUsers { get; set; } = new List<UserShortFriendship>();

        [JsonProperty("newest_cursor")]
        public string NewestCursor { get; set; }

        [JsonProperty("mentions_muted")]
        public bool MentionsMuted { get; set; }

        [JsonIgnore]
        public bool HasUnreadMessage {
            get
            {
                if (LastSeenAt != null && LastSeenAt.TryGetValue(ViewerId, out var viewerLastSeen))
                {
                    return LastNonSenderItemAt > viewerLastSeen.Timestamp &&
                                              LastActivity == LastNonSenderItemAt;
                }

                return false;
            }
        }

        public bool Equals(InboxThread other)
        {
            if (other == null) return false;
            return other.ThreadId == ThreadId;
        }
    }

    public enum ThreadType
    {
        Private = 0
    }
}

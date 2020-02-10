using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using InstagramAPI.Classes.Responses;
using InstagramAPI.Classes.User;
using Newtonsoft.Json;

namespace InstagramAPI.Classes.Direct
{
    public class InboxContainer : BaseStatusResponse
    {
        [JsonProperty("pending_requests_total")] public int PendingRequestsCount { get; set; }

        [JsonProperty("seq_id")] public int SeqId { get; set; }

        // TODO: Investigate what this property does
        // public InstaDirectInboxSubscription Subscription { get; set; } = new InstaDirectInboxSubscription();

        [JsonProperty("inbox")] public Inbox Inbox { get; set; } = new Inbox();

        [JsonProperty("pending_requests_users")] public List<UserShort> PendingUsers { get; set; } = new List<UserShort>(0);

        [JsonProperty("snapshot_at_ms")] public long SnapshotAtMs { get; set; }

        public DateTimeOffset SnapshotAt => DateTimeOffset.FromUnixTimeMilliseconds(SnapshotAtMs);
    }
}

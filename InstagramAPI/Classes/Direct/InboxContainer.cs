using System;
using System.Collections.Generic;
using InstagramAPI.Classes.JsonConverters;
using InstagramAPI.Classes.Responses;
using InstagramAPI.Classes.User;
using Newtonsoft.Json;

namespace InstagramAPI.Classes.Direct
{
    public class InboxContainer : BaseStatusResponse
    {
        [JsonProperty("pending_requests_total")] public int PendingRequestsCount { get; set; }

        [JsonProperty("seq_id")] public long SeqId { get; set; }

        // TODO: Investigate what this property does
        // public InstaDirectInboxSubscription Subscription { get; set; } = new InstaDirectInboxSubscription();

        [JsonProperty("inbox")] public Inbox Inbox { get; set; } = new Inbox();

        [JsonProperty("pending_requests_users")] public List<BaseUser> PendingUsers { get; set; }

        [JsonProperty("snapshot_at_ms")] 
        [JsonConverter(typeof(MilliTimestampConverter))]
        public DateTimeOffset SnapshotAt { get; set; }
    }
}

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
namespace InstagramAPI.Sync.Subs
{
    static class GraphQLSub
    {
        public static readonly Dictionary<string, string> QueryIds = new Dictionary<string, string>
        {
            {"AppPresence", "17846944882223835"},
            {"AsyncAdSub", "17911191835112000"},
            {"ClientConfigUpdate", "17849856529644700"},
            {"DirectStatus", "17854499065530643"},
            {"DirectTyping", "17867973967082385"},
            {"LiveWave", "17882305414154951"},
            {"InteractivityActivateQuestion", "18005526940184517"},
            {"InteractivityRealtimeQuestionSubmissionsStatus", "18027779584026952"},
            {"InteractivitySub", "17907616480241689"},
            {"LiveRealtimeComments", "17855344750227125"},
            {"LiveTypingIndicator", "17926314067024917"},
            {"MediaFeedback", "17877917527113814"},
            {"ReactNativeOTA", "17861494672288167"},
            {"VideoCallCoWatchControl", "17878679623388956"},
            {"VideoCallInAlert", "17878679623388956"},
            {"VideoCallPrototypePublish", "18031704190010162"},
            {"ZeroProvision", "17913953740109069"}
        };

    }

    class GraphQLSubBaseOptions
    {
        public string SubscriptionId { get; set; } = Guid.NewGuid().ToString();
        public bool? ClientLogged { get; set; }
    }

    static class GraphQLSubscriptions
    {
        private static string FormatSubscriptionString(string queryId, Dictionary<string, string> parameters, bool? clientLogged = null)
        {
            var jObj = new JObject();
            foreach (var item in parameters)
                jObj.Add(item.Key, item.Value);
            var j = new JObject
            {
                {"input_data",  jObj}
            };

            if (clientLogged != null)
                j.Add("client_logged", clientLogged.Value);
            return $"1/graphqlsubscriptions/{queryId}/{JsonConvert.SerializeObject(j)}";
        }

        public static string GetAppPresenceSubscription(GraphQLSubBaseOptions options = null)
        {
            if (options == null) options = new GraphQLSubBaseOptions();
            return FormatSubscriptionString(GraphQLSub.QueryIds["AppPresence"],
                new Dictionary<string, string>
                {
                    { "client_subscription_id", options.SubscriptionId ?? Guid.NewGuid().ToString()},
                }, options.ClientLogged);
        }

        public static string GetAsyncAdSubscription(string userId, GraphQLSubBaseOptions options = null)
        {
            if (options == null) options = new GraphQLSubBaseOptions();
            return FormatSubscriptionString(GraphQLSub.QueryIds["AsyncAdSub"],
                new Dictionary<string, string>
                {
                    {"client_subscription_id", options.SubscriptionId ?? Guid.NewGuid().ToString()},
                    {"user_id", userId}
                }, options.ClientLogged);
        }

        public static string GetClientConfigUpdateSubscription(GraphQLSubBaseOptions options = null)
        {
            if (options == null) options = new GraphQLSubBaseOptions();
            return FormatSubscriptionString(GraphQLSub.QueryIds["ClientConfigUpdate"],
                new Dictionary<string, string>
                {
                    { "client_subscription_id", options.SubscriptionId ?? Guid.NewGuid().ToString()},
                }, options.ClientLogged);
        }

        public static string GetDirectStatusSubscription(GraphQLSubBaseOptions options = null)
        {
            if (options == null) options = new GraphQLSubBaseOptions();
            return FormatSubscriptionString(GraphQLSub.QueryIds["DirectStatus"],
                new Dictionary<string, string>
                {
                    { "client_subscription_id", options.SubscriptionId ?? Guid.NewGuid().ToString()},
                }, options.ClientLogged);
        }

        public static string GetDirectTypingSubscription(string userId, bool? clientLogged = null)
        {
            return FormatSubscriptionString(GraphQLSub.QueryIds["DirectTyping"],
                new Dictionary<string, string>
                {
                    {"user_id", userId}
                }, clientLogged);
        }

        public static string GetIgLiveWaveSubscription(string broadcastId, string receiverId, GraphQLSubBaseOptions options = null)
        {
            if (options == null) options = new GraphQLSubBaseOptions();
            return FormatSubscriptionString(GraphQLSub.QueryIds["LiveWave"],
                new Dictionary<string, string>
                {
                    {"client_subscription_id", options.SubscriptionId ?? Guid.NewGuid().ToString()},
                    {"broadcast_id", broadcastId},
                    {"receiver_id", receiverId},
                }, options.ClientLogged);
        }

        public static string GetInteractivityActivateQuestionSubscription(string broadcastId, GraphQLSubBaseOptions options = null)
        {
            if (options == null) options = new GraphQLSubBaseOptions();
            return FormatSubscriptionString(GraphQLSub.QueryIds["InteractivityActivateQuestion"],
                new Dictionary<string, string>
                {
                    {"client_subscription_id", options.SubscriptionId ?? Guid.NewGuid().ToString()},
                    {"broadcast_id", broadcastId},
                }, options.ClientLogged);
        }

        public static string GetInteractivityRealtimeQuestionSubmissionsStatusSubscription(string broadcastId, GraphQLSubBaseOptions options = null)
        {
            if (options == null) options = new GraphQLSubBaseOptions();
            return FormatSubscriptionString(GraphQLSub.QueryIds["InteractivityRealtimeQuestionSubmissionsStatus"],
                new Dictionary<string, string>
                {
                    {"client_subscription_id", options.SubscriptionId ?? Guid.NewGuid().ToString()},
                    {"broadcast_id", broadcastId},
                }, options.ClientLogged);
        }

        public static string GetInteractivitySubscription(string broadcastId, GraphQLSubBaseOptions options = null)
        {
            if (options == null) options = new GraphQLSubBaseOptions();
            return FormatSubscriptionString(GraphQLSub.QueryIds["InteractivitySub"],
                new Dictionary<string, string>
                {
                    {"client_subscription_id", options.SubscriptionId ?? Guid.NewGuid().ToString()},
                    {"broadcast_id", broadcastId},
                }, options.ClientLogged);
        }

        public static string GetLiveRealtimeCommentsSubscription(string broadcastId, GraphQLSubBaseOptions options = null)
        {
            if (options == null) options = new GraphQLSubBaseOptions();
            return FormatSubscriptionString(GraphQLSub.QueryIds["LiveRealtimeComments"],
                new Dictionary<string, string>
                {
                    {"client_subscription_id", options.SubscriptionId ?? Guid.NewGuid().ToString()},
                    {"broadcast_id", broadcastId},
                }, options.ClientLogged);
        }

        public static string GetLiveTypingIndicatorSubscription(string broadcastId, GraphQLSubBaseOptions options = null)
        {
            if (options == null) options = new GraphQLSubBaseOptions();
            return FormatSubscriptionString(GraphQLSub.QueryIds["LiveTypingIndicator"],
                new Dictionary<string, string>
                {
                    {"client_subscription_id", options.SubscriptionId ?? Guid.NewGuid().ToString()},
                    {"broadcast_id", broadcastId},
                }, options.ClientLogged);
        }

        public static string GetMediaFeedbackSubscription(string feedbackId, GraphQLSubBaseOptions options = null)
        {
            if (options == null) options = new GraphQLSubBaseOptions();
            return FormatSubscriptionString(GraphQLSub.QueryIds["MediaFeedback"],
                new Dictionary<string, string>
                {
                    {"client_subscription_id", options.SubscriptionId ?? Guid.NewGuid().ToString()},
                    {"feedback_id", feedbackId},
                }, options.ClientLogged);
        }

        public static string GetReactNativeOTAUpdateSubscription(string buildNumber, GraphQLSubBaseOptions options = null)
        {
            if (options == null) options = new GraphQLSubBaseOptions();
            return FormatSubscriptionString(GraphQLSub.QueryIds["ReactNativeOTA"],
                new Dictionary<string, string>
                {
                    {"client_subscription_id", options.SubscriptionId ?? Guid.NewGuid().ToString()},
                    {"build_number", buildNumber},
                }, options.ClientLogged);
        }

        public static string GetVideoCallCoWatchControlSubscription(string videoCallId, GraphQLSubBaseOptions options = null)
        {
            if (options == null) options = new GraphQLSubBaseOptions();
            return FormatSubscriptionString(GraphQLSub.QueryIds["VideoCallCoWatchControl"],
                new Dictionary<string, string>
                {
                    {"client_subscription_id", options.SubscriptionId ?? Guid.NewGuid().ToString()},
                    {"video_call_id", videoCallId},
                }, options.ClientLogged);
        }

        public static string GetVideoCallInCallAlertSubscription(string videoCallId, GraphQLSubBaseOptions options = null)
        {
            if (options == null) options = new GraphQLSubBaseOptions();
            return FormatSubscriptionString(GraphQLSub.QueryIds["VideoCallInAlert"],
                new Dictionary<string, string>
                {
                    {"client_subscription_id", options.SubscriptionId ?? Guid.NewGuid().ToString()},
                    {"video_call_id", videoCallId},
                }, options.ClientLogged);
        }

        public static string GetVideoCallPrototypePublishSubscription(string videoCallId, GraphQLSubBaseOptions options = null)
        {
            if (options == null) options = new GraphQLSubBaseOptions();
            return FormatSubscriptionString(GraphQLSub.QueryIds["VideoCallPrototypePublish"],
                new Dictionary<string, string>
                {
                    {"client_subscription_id", options.SubscriptionId ?? Guid.NewGuid().ToString()},
                    {"video_call_id", videoCallId},
                }, options.ClientLogged);
        }

        public static string GetZeroProvisionSubscription(string deviceId, GraphQLSubBaseOptions options = null)
        {
            if (options == null) options = new GraphQLSubBaseOptions();
            return FormatSubscriptionString(GraphQLSub.QueryIds["ZeroProvision"],
                new Dictionary<string, string>
                {
                    {"client_subscription_id", options.SubscriptionId ?? Guid.NewGuid().ToString()},
                    {"device_id", deviceId},
                }, options.ClientLogged);
        }
    }
}

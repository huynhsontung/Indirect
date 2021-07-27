using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace InstagramAPI.Realtime.Subscriptions
{
    internal struct GraphQLQueryId
    {
        public const string AppPresence = "17846944882223835";
        public const string AsyncAdSub = "17911191835112000";
        public const string ClientConfigUpdate = "17849856529644700";
        public const string DirectStatus = "17854499065530643";
        public const string DirectTyping = "17867973967082385";
        public const string LiveWave = "17882305414154951";
        public const string InteractivityActivateQuestion = "18005526940184517";
        public const string InteractivityRealtimeQuestionSubmissionsStatus = "18027779584026952";
        public const string InteractivitySub = "17907616480241689";
        public const string LiveRealtimeComments = "17855344750227125";
        public const string LiveTypingIndicator = "17926314067024917";
        public const string MediaFeedback = "17877917527113814";
        public const string ReactNativeOTA = "17861494672288167";
        public const string VideoCallCoWatchControl = "17878679623388956";
        public const string VideoCallInAlert = "17878679623388956";
        public const string VideoCallPrototypePublish = "18031704190010162";
        public const string ZeroProvision = "17913953740109069";
    }

    internal class GraphQLSubBaseOptions
    {
        public string SubscriptionId { get; set; } = Guid.NewGuid().ToString();
        public bool? ClientLogged { get; set; }
    }

    static class GraphQLSubscription
    {
        private static string FormatSubscriptionString(string queryId, JObject parameters, bool? clientLogged = null)
        {
            var jObject = new JObject
            {
                {"input_data",  parameters}
            };

            if (clientLogged != null)
                jObject.Add("client_logged", clientLogged.Value);
            return $"1/graphqlsubscriptions/{queryId}/{jObject.ToString(Formatting.None)}";
        }

        public static string GetAppPresenceSubscription(GraphQLSubBaseOptions options = null)
        {
            if (options == null) options = new GraphQLSubBaseOptions();
            return FormatSubscriptionString(GraphQLQueryId.AppPresence,
                new JObject
                {
                    { "client_subscription_id", options.SubscriptionId ?? Guid.NewGuid().ToString()},
                }, options.ClientLogged);
        }

        public static string GetAsyncAdSubscription(long userId, GraphQLSubBaseOptions options = null)
        {
            if (options == null) options = new GraphQLSubBaseOptions();
            return FormatSubscriptionString(GraphQLQueryId.AsyncAdSub,
                new JObject
                {
                    {"client_subscription_id", options.SubscriptionId ?? Guid.NewGuid().ToString()},
                    {"user_id", userId.ToString()}
                }, options.ClientLogged);
        }

        public static string GetClientConfigUpdateSubscription(GraphQLSubBaseOptions options = null)
        {
            if (options == null) options = new GraphQLSubBaseOptions();
            return FormatSubscriptionString(GraphQLQueryId.ClientConfigUpdate,
                new JObject
                {
                    { "client_subscription_id", options.SubscriptionId ?? Guid.NewGuid().ToString()},
                }, options.ClientLogged);
        }

        public static string GetDirectStatusSubscription(GraphQLSubBaseOptions options = null)
        {
            if (options == null) options = new GraphQLSubBaseOptions();
            return FormatSubscriptionString(GraphQLQueryId.DirectStatus,
                new JObject
                {
                    { "client_subscription_id", options.SubscriptionId ?? Guid.NewGuid().ToString()},
                }, options.ClientLogged);
        }

        public static string GetDirectTypingSubscription(long userId, bool? clientLogged = null)
        {
            return FormatSubscriptionString(GraphQLQueryId.DirectTyping,
                new JObject
                {
                    {"user_id", userId.ToString()}
                }, clientLogged);
        }

        public static string GetIgLiveWaveSubscription(string broadcastId, string receiverId, GraphQLSubBaseOptions options = null)
        {
            if (options == null) options = new GraphQLSubBaseOptions();
            return FormatSubscriptionString(GraphQLQueryId.LiveWave,
                new JObject
                {
                    {"client_subscription_id", options.SubscriptionId ?? Guid.NewGuid().ToString()},
                    {"broadcast_id", broadcastId},
                    {"receiver_id", receiverId},
                }, options.ClientLogged);
        }

        public static string GetInteractivityActivateQuestionSubscription(string broadcastId, GraphQLSubBaseOptions options = null)
        {
            if (options == null) options = new GraphQLSubBaseOptions();
            return FormatSubscriptionString(GraphQLQueryId.InteractivityActivateQuestion,
                new JObject
                {
                    {"client_subscription_id", options.SubscriptionId ?? Guid.NewGuid().ToString()},
                    {"broadcast_id", broadcastId},
                }, options.ClientLogged);
        }

        public static string GetInteractivityRealtimeQuestionSubmissionsStatusSubscription(string broadcastId, GraphQLSubBaseOptions options = null)
        {
            if (options == null) options = new GraphQLSubBaseOptions();
            return FormatSubscriptionString(GraphQLQueryId.InteractivityRealtimeQuestionSubmissionsStatus,
                new JObject
                {
                    {"client_subscription_id", options.SubscriptionId ?? Guid.NewGuid().ToString()},
                    {"broadcast_id", broadcastId},
                }, options.ClientLogged);
        }

        public static string GetInteractivitySubscription(string broadcastId, GraphQLSubBaseOptions options = null)
        {
            if (options == null) options = new GraphQLSubBaseOptions();
            return FormatSubscriptionString(GraphQLQueryId.InteractivitySub,
                new JObject
                {
                    {"client_subscription_id", options.SubscriptionId ?? Guid.NewGuid().ToString()},
                    {"broadcast_id", broadcastId},
                }, options.ClientLogged);
        }

        public static string GetLiveRealtimeCommentsSubscription(string broadcastId, GraphQLSubBaseOptions options = null)
        {
            if (options == null) options = new GraphQLSubBaseOptions();
            return FormatSubscriptionString(GraphQLQueryId.LiveRealtimeComments,
                new JObject
                {
                    {"client_subscription_id", options.SubscriptionId ?? Guid.NewGuid().ToString()},
                    {"broadcast_id", broadcastId},
                }, options.ClientLogged);
        }

        public static string GetLiveTypingIndicatorSubscription(string broadcastId, GraphQLSubBaseOptions options = null)
        {
            if (options == null) options = new GraphQLSubBaseOptions();
            return FormatSubscriptionString(GraphQLQueryId.LiveTypingIndicator,
                new JObject
                {
                    {"client_subscription_id", options.SubscriptionId ?? Guid.NewGuid().ToString()},
                    {"broadcast_id", broadcastId},
                }, options.ClientLogged);
        }

        public static string GetMediaFeedbackSubscription(string feedbackId, GraphQLSubBaseOptions options = null)
        {
            if (options == null) options = new GraphQLSubBaseOptions();
            return FormatSubscriptionString(GraphQLQueryId.MediaFeedback,
                new JObject
                {
                    {"client_subscription_id", options.SubscriptionId ?? Guid.NewGuid().ToString()},
                    {"feedback_id", feedbackId},
                }, options.ClientLogged);
        }

        public static string GetReactNativeOTAUpdateSubscription(string buildNumber, GraphQLSubBaseOptions options = null)
        {
            if (options == null) options = new GraphQLSubBaseOptions();
            return FormatSubscriptionString(GraphQLQueryId.ReactNativeOTA,
                new JObject
                {
                    {"client_subscription_id", options.SubscriptionId ?? Guid.NewGuid().ToString()},
                    {"build_number", buildNumber},
                }, options.ClientLogged);
        }

        public static string GetVideoCallCoWatchControlSubscription(string videoCallId, GraphQLSubBaseOptions options = null)
        {
            if (options == null) options = new GraphQLSubBaseOptions();
            return FormatSubscriptionString(GraphQLQueryId.VideoCallCoWatchControl,
                new JObject
                {
                    {"client_subscription_id", options.SubscriptionId ?? Guid.NewGuid().ToString()},
                    {"video_call_id", videoCallId},
                }, options.ClientLogged);
        }

        public static string GetVideoCallInCallAlertSubscription(string videoCallId, GraphQLSubBaseOptions options = null)
        {
            if (options == null) options = new GraphQLSubBaseOptions();
            return FormatSubscriptionString(GraphQLQueryId.VideoCallInAlert,
                new JObject
                {
                    {"client_subscription_id", options.SubscriptionId ?? Guid.NewGuid().ToString()},
                    {"video_call_id", videoCallId},
                }, options.ClientLogged);
        }

        public static string GetVideoCallPrototypePublishSubscription(string videoCallId, GraphQLSubBaseOptions options = null)
        {
            if (options == null) options = new GraphQLSubBaseOptions();
            return FormatSubscriptionString(GraphQLQueryId.VideoCallPrototypePublish,
                new JObject
                {
                    {"client_subscription_id", options.SubscriptionId ?? Guid.NewGuid().ToString()},
                    {"video_call_id", videoCallId},
                }, options.ClientLogged);
        }

        public static string GetZeroProvisionSubscription(string deviceId, GraphQLSubBaseOptions options = null)
        {
            if (options == null) options = new GraphQLSubBaseOptions();
            return FormatSubscriptionString(GraphQLQueryId.ZeroProvision,
                new JObject
                {
                    {"client_subscription_id", options.SubscriptionId ?? Guid.NewGuid().ToString()},
                    {"device_id", deviceId},
                }, options.ClientLogged);
        }
    }
}

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.UI.Core;
using Indirect.Entities.Wrappers;
using InstagramAPI.Classes.Direct;
using InstagramAPI.Sync;
using InstagramAPI.Utils;

namespace Indirect
{
    internal partial class MainViewModel
    {
        void SubscribeHandlers()
        {
            InstaApi.SyncClient.MessageReceived += OnMessageSyncReceived;
            InstaApi.SyncClient.ActivityIndicatorChanged += OnActivityIndicatorChanged;
            InstaApi.SyncClient.UserPresenceChanged += OnUserPresenceChanged;
            InstaApi.SyncClient.FailedToStart += OnSyncClientFailedToStart;
            Inbox.FirstUpdated += OnInboxFirstUpdated;
            PushClient.MessageReceived += (sender, args) =>
            {
                this.Log("Background notification: " + args.Json);
            };
        }

        private async void OnInboxFirstUpdated(int seqId, DateTimeOffset snapshotAt)
        {
            if (!string.IsNullOrEmpty(_threadToBeOpened) && Inbox.Threads.Count > 0)
            {
                await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    SelectedThread = Inbox.Threads.FirstOrDefault(x => x.ThreadId == _threadToBeOpened);
                });
            }
            await InstaApi.SyncClient.Start(seqId, snapshotAt).ConfigureAwait(false);
        }

        private async void OnSyncClientFailedToStart(object sender, Exception exception)
        {
            DebugLogger.LogException(exception);
            await HandleException();
        }

        private async void OnMessageSyncReceived(object sender, List<MessageSyncEventArgs> data)
        {
            try
            {
                var updateInbox = false;
                foreach (var syncEvent in data)
                {
                    if (syncEvent.Data.Count == 0) continue;
                    var itemData = syncEvent.Data[0];
                    if (syncEvent.SeqId > Inbox.SeqId)
                    {
                        Inbox.SeqId = syncEvent.SeqId;
                        if (itemData.Item != null)
                        {
                            Inbox.SnapshotAt = itemData.Item.Timestamp;
                        }
                    }
                    var segments = itemData.Path.Trim('/').Split('/');
                    var threadId = segments[2];
                    if (string.IsNullOrEmpty(threadId)) continue;
                    var mainThread = Inbox.Threads.FirstOrDefault(wrapper => wrapper.ThreadId == threadId);
                    if (mainThread == null)
                    {
                        if (!updateInbox) updateInbox = itemData.Op == "add";
                        continue;
                    }

                    // Update thread in main view as well as in secondary views
                    var threadsToUpdate = SecondaryThreadViews.Where(x => x.ThreadId == threadId).ToList();
                    threadsToUpdate.Add(mainThread);

                    foreach (var thread in threadsToUpdate)
                    {
                        switch (itemData.Op)
                        {
                            case "add":
                                {
                                    var item = itemData.Item;
                                    if (item.ItemType == DirectItemType.Placeholder)
                                    {
                                        if (syncEvent.Realtime) await Task.Delay(1000);
                                        var result =
                                            await InstaApi.GetItemsInDirectThreadAsync(threadId, itemData.Item.ItemId);
                                        if (result.IsSucceeded && result.Value.Items.Count > 0)
                                            item = result.Value.Items[0];
                                    }

                                    await thread.AddItem(item);
                                    break;
                                }
                            case "replace":
                                {
                                    if (itemData.Path.Contains("has_seen", StringComparison.Ordinal) &&
                                        long.TryParse(segments[4], out var userId))
                                    {
                                        await thread.UpdateLastSeenAt(userId, itemData.Item.Timestamp, itemData.Item.ItemId);
                                    }
                                    else
                                    {
                                        var item = thread.ObservableItems.LastOrDefault(x => x.ItemId == itemData.Item.ItemId);
                                        if (item != null)
                                        {
                                            await thread.Dispatcher.RunAsync(CoreDispatcherPriority.Low, () =>
                                            {
                                                if (itemData.Item.Reactions == null)
                                                {
                                                    item.Reactions.Clear();
                                                }
                                                else
                                                {
                                                    item.Reactions?.Update(new ReactionsWrapper(itemData.Item.Reactions),
                                                        thread.Users);
                                                }
                                            });
                                        }
                                    }
                                    break;
                                }
                            case "remove":
                                await thread.RemoveItem(itemData.Value);
                                break;
                            default:
                                DebugLogger.LogException(new Exception($"Sync operation '{itemData.Op}' not expected"));
                                break;
                        }
                    }
                }
                if (updateInbox)
                {
                    await Inbox.UpdateInbox();
                }
            }
            catch (Exception e)
            {
                DebugLogger.LogException(e);
                if (DateTimeOffset.Now - _lastUpdated > TimeSpan.FromSeconds(0.5))
                    await UpdateInboxAndSelectedThread();
            }
            this.Log("Sync(s) received.");
        }

        private void OnActivityIndicatorChanged(object sender, PubsubEventArgs data)
        {
            try
            {
                var indicatorData = data.Data[0];
                var segments = indicatorData.Path.Trim('/').Split('/');
                var threadId = segments[2];
                if (string.IsNullOrEmpty(threadId)) return;
                var thread = Inbox.Threads.FirstOrDefault(wrapper => wrapper.ThreadId == threadId);
                if (thread == null) return;
                if (indicatorData.Indicator.ActivityStatus == 1)
                    thread.PingTypingIndicator(indicatorData.Indicator.TimeToLive);
                else
                    thread.PingTypingIndicator(0);
            }
            catch (Exception e)
            {
                DebugLogger.LogException(e);
            }
        }

        private void OnUserPresenceChanged(object sender, UserPresenceEventArgs e)
        {
            UserPresenceDictionary[e.UserId] = e;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(UserPresenceDictionary)));
        }
    }
}

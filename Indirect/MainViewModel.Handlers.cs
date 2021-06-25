using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Indirect.Entities.Wrappers;
using Indirect.Utilities;
using InstagramAPI.Classes.Direct;
using InstagramAPI.Sync;
using InstagramAPI.Utils;

namespace Indirect
{
    internal partial class MainViewModel
    {
        private void RegisterSyncClientHandlers(SyncClient client)
        {
            client.MessageReceived -= OnMessageSyncReceived;
            client.ActivityIndicatorChanged -= OnActivityIndicatorChanged;
            client.UserPresenceChanged -= OnUserPresenceChanged;

            client.MessageReceived += OnMessageSyncReceived;
            client.ActivityIndicatorChanged += OnActivityIndicatorChanged;
            client.UserPresenceChanged += OnUserPresenceChanged;
        }

        private void InboxThreads_OnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action != NotifyCollectionChangedAction.Add || e.NewItems == null || ThreadInfoDictionary == null)
            {
                return;
            }

            foreach (var item in e.NewItems)
            {
                var thread = (DirectThreadWrapper) item;
                if (string.IsNullOrEmpty(thread.ThreadId))
                {
                    continue;
                }

                ThreadInfoDictionary[thread.ThreadId] = new DirectThreadInfo(thread.Source);
            }
        }

        private async void OnInboxFirstUpdated(long seqId, DateTimeOffset snapshotAt)
        {
            await SyncClient.Start(seqId, snapshotAt, true).ConfigureAwait(false);
            if (!string.IsNullOrEmpty(_threadToBeOpened) && Inbox.Threads.Count > 0)
            {
                await Inbox.Dispatcher.QuickRunAsync(() =>
                {
                    Inbox.SelectedThread = Inbox.Threads.FirstOrDefault(x => x.ThreadId == _threadToBeOpened);
                });
            }
        }

        private async void OnMessageSyncReceived(object sender, List<MessageSyncEventArgs> data)
        {
            try
            {
                var updateInbox = false;
                foreach (var syncEvent in data)
                {
                    if (syncEvent.Data.Count == 0)
                    {
                        continue;
                    }

                    var itemData = syncEvent.Data[0];
                    if (syncEvent.SeqId > Inbox.SeqId)
                    {
                        Inbox.Container.SeqId = syncEvent.SeqId;
                        Inbox.Container.SnapshotAt = DateTimeOffset.Now;
                    }

                    if (itemData.Item == null)
                    {
                        continue;
                    }

                    var segments = itemData.Path.Trim('/').Split('/');
                    var threadId = segments[2];
                    if (string.IsNullOrEmpty(threadId)) continue;
                    var mainThread = Inbox.Threads.FirstOrDefault(wrapper => wrapper.ThreadId == threadId);
                    if (mainThread == null && StartedFromMainView)
                    {
                        if (!updateInbox) updateInbox = itemData.Op == "add";
                        continue;
                    }

                    // Update thread in main view as well as in secondary views
                    var threadsToUpdate = SecondaryThreads.Where(x => x.ThreadId == threadId).ToList();
                    if (mainThread != null) threadsToUpdate.Add(mainThread);

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
                                        var item = thread.ObservableItems.LastOrDefault(x => x.Source.ItemId == itemData.Item.ItemId);
                                        if (item != null)
                                        {
                                            await thread.Dispatcher.QuickRunAsync(() =>
                                            {
                                                if (itemData.Item.Reactions == null)
                                                {
                                                    item.ObservableReactions.Clear();
                                                }
                                                else
                                                {
                                                    item.ObservableReactions.Update(itemData.Item.Reactions);
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

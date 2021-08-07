using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Windows.Networking.Connectivity;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Indirect.Entities.Wrappers;
using Indirect.Pages;
using Indirect.Utilities;
using InstagramAPI.Classes.Direct;
using InstagramAPI.Classes.Direct.ItemContent;
using InstagramAPI.Realtime;
using InstagramAPI.Utils;
using Microsoft.UI.Xaml.Controls;
using Newtonsoft.Json;

namespace Indirect
{
    internal partial class MainViewModel
    {
        private async Task StartRealtimeClient()
        {
            RealtimeClient.MessageReceived -= OnMessageSyncReceived;
            RealtimeClient.ActivityIndicatorChanged -= OnActivityIndicatorChanged;
            RealtimeClient.UserPresenceChanged -= OnUserPresenceChanged;
            RealtimeClient.ShuttingDown -= RealtimeClientOnUnexpectedShutdown;
            NetworkInformation.NetworkStatusChanged -= OnNetworkStatusChanged;

            RealtimeClient.MessageReceived += OnMessageSyncReceived;
            RealtimeClient.ActivityIndicatorChanged += OnActivityIndicatorChanged;
            RealtimeClient.UserPresenceChanged += OnUserPresenceChanged;
            RealtimeClient.ShuttingDown += RealtimeClientOnUnexpectedShutdown;

            try
            {
                await RealtimeClient.Start(Inbox.SeqId, Inbox.SnapshotAt);
            }
            catch (Exception e)
            {
                DebugLogger.LogException(e);
                ShowErrorMessage("Cannot connect to the server", "New messages will not be updated. Please try again later.");
                return;
            }

            // Hide error message
            ShowErrorMessage(null, null);
        }

        private void ShutdownRealtimeClient()
        {
            RealtimeClient.MessageReceived -= OnMessageSyncReceived;
            RealtimeClient.ActivityIndicatorChanged -= OnActivityIndicatorChanged;
            RealtimeClient.UserPresenceChanged -= OnUserPresenceChanged;
            RealtimeClient.ShuttingDown -= RealtimeClientOnUnexpectedShutdown;
            NetworkInformation.NetworkStatusChanged -= OnNetworkStatusChanged;

            RealtimeClient.Shutdown();
        }

        private async void RealtimeClientOnUnexpectedShutdown(object sender, EventArgs e)
        {
            ShowErrorMessage("Lost connection to the server", "Attempting to reconnect...");
            await Task.Delay(5000);

            var internetProfile = NetworkInformation.GetInternetConnectionProfile();
            if (internetProfile == null)
            {
                ShowErrorMessage("No Internet connection",
                    "New messages will not be updated. Please check your Internet connection.");
                NetworkInformation.NetworkStatusChanged += OnNetworkStatusChanged;
            }
            else
            {
                await StartRealtimeClient();
            }
        }

        private async void OnNetworkStatusChanged(object sender)
        {
            if (await Debouncer.Delay(nameof(OnNetworkStatusChanged), 5000) && !RealtimeClient.Running)
            {
                await StartRealtimeClient().ConfigureAwait(false);
            }
        }

        private async void OnInboxFirstUpdated(object sender, EventArgs eventArgs)
        {
            await StartRealtimeClient().ConfigureAwait(false);
            if (!string.IsNullOrEmpty(_threadToBeOpened) && Inbox.Threads.Count > 0)
            {
                await Inbox.Dispatcher.QuickRunAsync(() =>
                {
                    Inbox.SelectedThread = Inbox.Threads.FirstOrDefault(x => x.ThreadId == _threadToBeOpened);
                });
            }
        }

        private async Task<bool> SyncForEachThread(string threadId, SyncItem syncItem, string[] breadcrumbs)
        {
            var mainThread = Inbox.Threads.FirstOrDefault(wrapper => wrapper.ThreadId == threadId);

            // Update thread in main view as well as in secondary views
            var threadsToUpdate = SecondaryThreads.Where(x => x.ThreadId == threadId).ToList();
            if (mainThread != null)
            {
                threadsToUpdate.Add(mainThread);
            }

            if (threadsToUpdate.Count == 0)
            {
                return false;
            }

            foreach (var thread in threadsToUpdate)
            {
                switch (syncItem.Op)
                {
                    case "add":
                        await HandleSyncAdd(thread, syncItem, breadcrumbs);
                        break;

                    case "replace":
                        HandleSyncReplace(thread, syncItem, breadcrumbs);
                        break;

                    case "remove":
                        await HandleSyncRemove(thread, syncItem, breadcrumbs);
                        break;

                    default:
                        DebugLogger.LogException(new Exception($"Sync operation '{syncItem.Op}' not expected"));
                        break;
                }
            }

            return true;
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
                    this.Log($"SyncItem {itemData.Op} {itemData.Path}");
                    this.Log(itemData.Value);

                    if (syncEvent.SeqId > Inbox.SeqId)
                    {
                        Inbox.Container.SeqId = syncEvent.SeqId;
                        Inbox.Container.SnapshotAt = DateTimeOffset.Now;
                    }

                    var breadcrumbs = itemData.Path.Split('/', StringSplitOptions.RemoveEmptyEntries);
                    var threadId = breadcrumbs.Length > 3 && breadcrumbs[1] == "threads" ? breadcrumbs[2] : null;
                    if (string.IsNullOrEmpty(threadId)) continue;

                    var threadsUpdated = await SyncForEachThread(threadId, itemData, breadcrumbs.Skip(3).ToArray());
                    if (!threadsUpdated && StartedFromMainView)
                    {
                        if (!updateInbox) updateInbox = itemData.Op == "add";
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
        }

        private async Task HandleSyncAdd(DirectThreadWrapper thread, SyncItem syncItem, string[] breadcrumbs)
        {
            if (breadcrumbs.Length < 2)
            {
                return;
            }

            switch (breadcrumbs[0])
            {
                case "items" when breadcrumbs.Length == 2:
                {
                    var itemId = breadcrumbs[1];
                    var item = JsonConvert.DeserializeObject<DirectItem>(syncItem.Value);
                    item.ItemId = itemId;
                    thread.AddItem(item);
                    break;
                }

                case "items" when breadcrumbs.Length == 5 && breadcrumbs[2] == "reactions":
                {
                    var itemId = breadcrumbs[1];
                    var senderId = Convert.ToInt64(breadcrumbs[4]);
                    var reaction = JsonConvert.DeserializeObject<EmojiReaction>(syncItem.Value);
                    reaction.SenderId = senderId;
                    var existingItem = thread.ObservableItems.LastOrDefault(x => x.Source.ItemId == itemId);
                    if (existingItem != null)
                    {
                        // TODO: Refactor to use ObservableReactions' Dispatcher instead
                        await thread.Dispatcher.QuickRunAsync(() =>
                        {
                            existingItem.ObservableReactions.Add(reaction);
                        });
                    }

                    break;
                }
            }
        }

        private void HandleSyncReplace(DirectThreadWrapper thread, SyncItem syncItem, string[] breadcrumbs)
        {
            if (breadcrumbs.Length < 2)
            {
                return;
            }

            switch (breadcrumbs[0])
            {
                case "items" when breadcrumbs.Length == 2:
                {
                    var itemId = breadcrumbs[1];
                    var item = JsonConvert.DeserializeObject<DirectItem>(syncItem.Value);
                    item.ItemId = itemId;
                    thread.RemoveItem(itemId);
                    thread.AddItem(item);
                    break;
                }

                case "participants" when breadcrumbs.Length == 3 && breadcrumbs[2] == "has_seen":
                {
                    var userId = Convert.ToInt64(breadcrumbs[1]);
                    var item = JsonConvert.DeserializeObject<DirectItem>(syncItem.Value);
                    thread.UpdateLastSeenAt(userId, item.Timestamp, item.ItemId);
                    break;
                }
            }
        }

        private async Task HandleSyncRemove(DirectThreadWrapper thread, SyncItem syncItem, string[] breadcrumbs)
        {
            if (breadcrumbs.Length < 2)
            {
                return;
            }

            switch (breadcrumbs[0])
            {
                case "items" when breadcrumbs.Length == 2:
                {
                    var itemId = breadcrumbs[1];
                    thread.RemoveItem(itemId);
                    break;
                }

                case "items" when breadcrumbs.Length == 5 && breadcrumbs[2] == "reactions":
                {
                    var itemId = breadcrumbs[1];
                    var senderId = Convert.ToInt64(breadcrumbs[4]);
                    var existingItem = thread.ObservableItems.FirstOrDefault(x => x.Source.ItemId == itemId);
                    if (existingItem != null)
                    {
                        // TODO: Refactor to use ObservableReactions' Dispatcher instead
                        await thread.Dispatcher.QuickRunAsync(() =>
                        {
                            existingItem.ObservableReactions.Remove(senderId);
                        });
                    }

                    break;
                }
            }
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

        private void ShowErrorMessage(string title, string message)
        {
            _mainWindowDispatcherQueue.TryEnqueue(() =>
            {
                var frame = Window.Current.Content as Frame;
                var page = frame?.Content as MainPage;
                page?.ShowStatus(title, message, InfoBarSeverity.Error);
            });
        }
    }
}

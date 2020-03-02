using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.Storage;
using Windows.Storage.FileProperties;
using Windows.Storage.Streams;
using Windows.UI.Core;
using Indirect.Utilities;
using Indirect.Wrapper;
using InstagramAPI;
using InstagramAPI.Classes;
using InstagramAPI.Classes.Android;
using InstagramAPI.Classes.Direct;
using InstagramAPI.Classes.Media;
using InstagramAPI.Classes.Responses;
using InstagramAPI.Classes.User;
using InstagramAPI.Push;
using InstagramAPI.Sync;
using Microsoft.AppCenter.Crashes;
using Microsoft.Toolkit.Uwp;
using Microsoft.Toolkit.Uwp.UI;
using Buffer = Windows.Storage.Streams.Buffer;

namespace Indirect
{
    internal class ApiContainer : INotifyPropertyChanged
    {
        private static ApiContainer _instance;
        public static ApiContainer Instance
        {
            get
            {
                if (_instance != null) return _instance;
                _instance = new ApiContainer();
                return _instance;
            }
        }

        private ApiContainer() { }

        // Todo: handle exceptions thrown by _instaApi like no network connection
        public const string INSTA_API_PROP_NAME = "InstaApi";
        private const string STATE_FILE_NAME = "state.bin";

        private readonly Instagram _instaApi = Instagram.Instance;
        private DateTimeOffset _lastUpdated = DateTimeOffset.Now;
        private CancellationTokenSource _searchCancellationToken;
        private InstaDirectInboxThreadWrapper _selectedThread;

        public event PropertyChangedEventHandler PropertyChanged;

        public PushClient PushClient => _instaApi.PushClient;
        public SyncClient SyncClient => _instaApi.SyncClient;

        public InstaDirectInboxWrapper Inbox { get; private set; }

        public IncrementalLoadingCollection<InstaDirectInboxWrapper, InstaDirectInboxThreadWrapper> InboxThreads =>
            Inbox.Threads;

        public CurrentUser LoggedInUser { get; private set; }

        public InstaDirectInboxThreadWrapper SelectedThread
        {
            get => _selectedThread;
            set
            {
                _selectedThread = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectedThread)));
            }
        }

        public AndroidDevice Device => _instaApi?.Device;
        public bool IsUserAuthenticated => _instaApi.IsUserAuthenticated;

        public async void OnLoggedIn()
        {
            if (!_instaApi.IsUserAuthenticated) throw new Exception("User is not logged in.");
            _instaApi.SyncClient.MessageReceived += OnMessageSyncReceived;
            Inbox = new InstaDirectInboxWrapper(_instaApi);
            Inbox.FirstUpdated += (seqId, snapshotAt) => _instaApi.SyncClient.Start(seqId, snapshotAt);
            await UpdateLoggedInUser();
            PushClient.Start();
            PushClient.MessageReceived += (sender, args) =>
            {
                Debug.Write("Background notification: ");
                Debug.WriteLine(args.Json);
            };
        }

        public void SetSelectedThreadNull()
        {
            SelectedThread = null;
        }

        public async Task<Result<LoginResult>> Login(string username, string password)
        {
            var logInResult = await _instaApi.LoginAsync(username, password).ConfigureAwait(false);
            return logInResult;
        }

        public void Logout()
        {
            _instaApi.Logout();
            _ = ImageCache.Instance.ClearAsync();
            _ = VideoCache.Instance.ClearAsync();
            // _settings.Values.Clear();
        }

        private async Task UpdateLoggedInUser()
        {
            var loggedInUser = await _instaApi.GetCurrentUserAsync().ConfigureAwait(false);
            LoggedInUser = loggedInUser.Value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(LoggedInUser)));
        }

        private async void OnMessageSyncReceived(object sender, List<MessageSyncEventArgs> data)
        {
            if (data.Count > 1 || !data[0].Realtime) return; // Old data. No need to process.
            try
            {
                var itemData = data[0].Data[0];
                var segments = itemData.Path.Trim('/').Split('/');
                var threadId = segments[2];
                var thread = InboxThreads.SingleOrDefault(wrapper => wrapper.ThreadId == threadId);
                if (thread == null) return;
                if (itemData.Op == "add" && thread.ObservableItems.Count > 0)
                {
                    await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
                        () => thread.AddItem(itemData.Item));
                    _ = Inbox.UpdateInbox();
                }

                if (itemData.Op == "replace")
                {
                    // todo: Handle items seen
                    if (itemData.Path.Contains("has_seen", StringComparison.Ordinal)) return;
                    var incomingItem = itemData.Item;
                    var item = thread.ObservableItems.SingleOrDefault(x => x.ItemId == incomingItem.ItemId);
                    await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                    {
                        if (incomingItem.Reactions == null)
                        {
                            item?.Reactions.Clear();
                        }
                        else
                        {
                            item?.Reactions?.Update(new InstaDirectReactionsWrapper(incomingItem.Reactions, thread.ViewerId),
                                thread.Users);
                        }
                    });
                }
            }
            catch (Exception e)
            {
#if !DEBUG
                Crashes.TrackError(e);
#endif
                Debug.WriteLine(e);
                if (DateTimeOffset.Now - _lastUpdated > TimeSpan.FromSeconds(0.5))
                    UpdateInboxAndSelectedThread();
            }
            Debug.WriteLine("Sync(s) received.");
        }

        public async Task UpdateSelectedThread()
        {
            if (SelectedThread == null)
                return;
            var result = await _instaApi.GetThreadAsync(SelectedThread.ThreadId, PaginationParameters.MaxPagesToLoad(1))
                .ConfigureAwait(false);
            if (result.IsSucceeded)
                await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(
                    CoreDispatcherPriority.Normal,
                    () => { SelectedThread.Update(result.Value); });
        }

        public async void SendLike()
        {
            var selectedThread = SelectedThread;
            if (string.IsNullOrEmpty(selectedThread.ThreadId)) return;
            var result = await _instaApi.SendLikeAsync(selectedThread.ThreadId).ConfigureAwait(false);
            if (result.IsSucceeded) UpdateInboxAndSelectedThread();
        }

        // Send message to the current selected recipient
        public async void SendMessage(string content)
        {
            var selectedThread = SelectedThread;
            content = content.Trim(' ', '\n', '\r');
            if (string.IsNullOrEmpty(content)) return;
            content = content.Replace('\r', '\n');
            var tokens = content.Split("\t\n ".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
            var links = tokens.Where(x =>
                x.StartsWith("http://", StringComparison.InvariantCultureIgnoreCase) ||
                x.StartsWith("https://", StringComparison.InvariantCultureIgnoreCase) || 
                x.StartsWith("www.", StringComparison.InvariantCultureIgnoreCase)).ToList();
            Result<List<DirectThread>> result;
            Result<ItemAckPayloadResponse> ackResult;   // for links and hashtags
            if (!string.IsNullOrEmpty(selectedThread.ThreadId))
            {
                if (links.Any())
                {
                    ackResult = await _instaApi.SendLinkAsync(content, links, selectedThread.ThreadId).ConfigureAwait(false);
                    return;
                }

                result = await _instaApi.SendTextAsync(null, selectedThread.ThreadId, content)
                    .ConfigureAwait(false);
            }
            else
            {
                if (links.Any())
                {
                    ackResult = await _instaApi.SendLinkToRecipientsAsync(content, links,
                        selectedThread.Users.Select(x => x.Pk).ToArray()).ConfigureAwait(false);
                    return;
                }

                result = await _instaApi.SendTextAsync(selectedThread.Users.Select(x => x.Pk),
                    null, content).ConfigureAwait(false);
            }
            
            if (result.IsSucceeded && result.Value.Count > 0)
            {
                // SyncClient will take care of updating. Update here is just for precaution.
                selectedThread.Update(result.Value[0]);
                // await Inbox.UpdateInbox();
            }
        }

        public async void SendFile(StorageFile file, Action<UploaderProgress> progress)
        {
            if (file.ContentType.Contains("image", StringComparison.OrdinalIgnoreCase))
            {
                var properties = await file.Properties.GetImagePropertiesAsync();
                int imageHeight = (int) properties.Height;
                int imageWidth = (int) properties.Width;
                IBuffer buffer;
                if (properties.Width > 1080 || properties.Height > 1080)
                {
                    buffer = await Helpers.CompressImage(file, 1080, 1080).ConfigureAwait(false);
                    double widthRatio = (double)1080 / imageWidth;
                    double heightRatio = (double)1080 / imageHeight;
                    double scaleRatio = Math.Min(widthRatio, heightRatio);
                    imageHeight = (int) Math.Floor(imageHeight * scaleRatio);
                    imageWidth = (int) Math.Floor(imageWidth * scaleRatio);
                }
                else
                {
                    buffer = await FileIO.ReadBufferAsync(file);
                }
                var instaImage = new InstaImage
                {
                    UploadBuffer = buffer, 
                    Width = imageWidth, 
                    Height = imageHeight
                };
                if (string.IsNullOrEmpty(SelectedThread.ThreadId) || SelectedThread.PendingScore == null) return;
                await _instaApi.SendDirectPhotoAsync(instaImage, SelectedThread.ThreadId,
                    SelectedThread.PendingScore ?? 0, progress).ConfigureAwait(false);
                return;
            }

            if (file.ContentType.Contains("video", StringComparison.OrdinalIgnoreCase))
            {
                var properties = await file.Properties.GetVideoPropertiesAsync();
                if (properties.Duration > TimeSpan.FromMinutes(1)) return;
                var buffer = await FileIO.ReadBufferAsync(file);
                var instaVideo = new InstaVideo()
                {
                    UploadBuffer = buffer,
                    Width = (int) properties.Width,
                    Height = (int) properties.Height,
                };
                var thumbnail = await file.GetThumbnailAsync(ThumbnailMode.VideosView);
                var thumbnailBuffer = new Buffer((uint) thumbnail.Size);
                await thumbnail.ReadAsync(thumbnailBuffer, (uint) thumbnail.Size, InputStreamOptions.None);
                var thumbnailImage = new InstaImage()
                {
                    UploadBuffer = thumbnailBuffer,
                    Width = (int) thumbnail.OriginalWidth,
                    Height = (int) thumbnail.OriginalHeight
                };
                await _instaApi.SendDirectVideoAsync(progress,
                    new InstaVideoUpload(instaVideo, thumbnailImage), SelectedThread.ThreadId).ConfigureAwait(false);
            }
        }

        public async void UpdateInboxAndSelectedThread()
        {
            _lastUpdated = DateTime.Now;
            var selected = SelectedThread;
            await Inbox.UpdateInbox().ConfigureAwait(true);
            if (selected == null) return;
            if (InboxThreads.Contains(selected) && SelectedThread != selected) SelectedThread = selected;
            await UpdateSelectedThread().ConfigureAwait(true);
            MarkLatestItemSeen(selected);
        }

        public async void Search(string query, Action<List<InstaDirectInboxThreadWrapper>> updateAction)
        {
            if (query.Length > 50) return;
            _searchCancellationToken?.Cancel();
            _searchCancellationToken = new CancellationTokenSource();
            var cancellationToken = _searchCancellationToken.Token;
            try
            {
                await Task.Delay(500, cancellationToken).ConfigureAwait(false);   // Delay so we don't search something mid typing
            }
            catch (Exception)
            {
                return;
            }
            if (cancellationToken.IsCancellationRequested) return;

            var result = await _instaApi.GetRankedRecipientsByUsernameAsync(query).ConfigureAwait(false);
            if (!result.IsSucceeded) return;
            var recipients = result.Value;
            var threadsFromUser = recipients.Users.Select(x => new InstaDirectInboxThreadWrapper(x, _instaApi)).ToList();
            var threadsFromRankedThread = recipients.Threads.Select(x => new InstaDirectInboxThreadWrapper(x, _instaApi)).ToList();
            var list = new List<InstaDirectInboxThreadWrapper>(threadsFromRankedThread.Count + threadsFromUser.Count);
            list.AddRange(threadsFromRankedThread);
            list.AddRange(threadsFromUser);
            var decoratedList = list.Select(x =>
            {
                if (x.LastPermanentItem == null) x.LastPermanentItem = new DirectItem();
                x.LastPermanentItem.Text = x.Users.Count == 1 ? x.Users[0].FullName : $"{x.Users.Count} participants";
                return x;
            }).ToList();
            updateAction?.Invoke(decoratedList);
        }

        public async void MakeProperInboxThread(InstaDirectInboxThreadWrapper placeholderThread)
        {
            InstaDirectInboxThreadWrapper thread;
            if (string.IsNullOrEmpty(placeholderThread.ThreadId))
            {
                var userIds = placeholderThread.Users.Select(x => x.Pk);
                var result = await _instaApi.GetThreadByParticipantsAsync(userIds).ConfigureAwait(false);
                if (!result.IsSucceeded) return;
                thread = result.Value != null && result.Value.Users.Count > 0 ? 
                    new InstaDirectInboxThreadWrapper(result.Value, _instaApi) : new InstaDirectInboxThreadWrapper(placeholderThread.Users[0], _instaApi);
            }
            else
            {
                thread = placeholderThread;
            }

            foreach (var existingThread in InboxThreads)
            {
                if (!thread.Equals(existingThread)) continue;
                thread = existingThread;
                break;
            }

            if (thread.LastPermanentItem == null)
            {
                thread.LastPermanentItem = new DirectItem() {Description = thread.Users[0].FullName};
            }

            SelectedThread = thread;
        }

        public async void MarkLatestItemSeen(InstaDirectInboxThreadWrapper thread)
        {
            if (string.IsNullOrEmpty(thread?.ThreadId)) return;
            if (thread.LastSeenAt.TryGetValue(thread.ViewerId, out var lastSeen))
            {
                if (string.IsNullOrEmpty(thread.LastPermanentItem?.ItemId) || 
                    lastSeen.ItemId == thread.LastPermanentItem.ItemId ||
                    thread.LastPermanentItem.FromMe) return;
                await _instaApi.MarkItemSeenAsync(thread.ThreadId,
                    thread.LastPermanentItem.ItemId).ConfigureAwait(false);
            }
        }
    }
}
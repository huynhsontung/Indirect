using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.Foundation;
using Windows.Storage;
using Windows.Storage.FileProperties;
using Windows.Storage.Streams;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Imaging;
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


        private readonly Instagram _instaApi = Instagram.Instance;
        private DateTimeOffset _lastUpdated = DateTimeOffset.Now;
        private CancellationTokenSource _searchCancellationToken;
        private InstaDirectInboxThreadWrapper _selectedThread;

        public event PropertyChangedEventHandler PropertyChanged;

        public PushClient PushClient => _instaApi.PushClient;
        public SyncClient SyncClient => _instaApi.SyncClient;

        public InstaDirectInboxWrapper Inbox { get; }

        public IncrementalLoadingCollection<InstaDirectInboxWrapper, InstaDirectInboxThreadWrapper> InboxThreads => Inbox.Threads;

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

        private ApiContainer()
        {
            _instaApi.SyncClient.MessageReceived += OnMessageSyncReceived;
            _instaApi.SyncClient.FailedToStart += async (sender, exception) =>
            {
#if !DEBUG
                Crashes.TrackError(exception);
#endif
                await HandleException();
            };
            Inbox = new InstaDirectInboxWrapper(_instaApi);
            Inbox.FirstUpdated += async (seqId, snapshotAt) => await _instaApi.SyncClient.Start(seqId, snapshotAt).ConfigureAwait(false);
            PushClient.MessageReceived += (sender, args) =>
            {
                Debug.Write("Background notification: ");
                Debug.WriteLine(args.Json);
            };
        }

        public async void OnLoggedIn()
        {
            if (!_instaApi.IsUserAuthenticated) throw new Exception("User is not logged in.");
            await UpdateLoggedInUser();
            PushClient.Start();
        }

        public void SetSelectedThreadNull()
        {
            SelectedThread = null;
        }

        public Task<Result<LoginResult>> Login(string username, string password) => _instaApi.LoginAsync(username, password);

        public Task<Result<LoginResult>> LoginWithFacebook(string fbAccessToken) =>
            _instaApi.LoginWithFacebookAsync(fbAccessToken);

        public void Logout()
        {
            _instaApi.Logout();
            _ = ImageCache.Instance.ClearAsync();
            _ = VideoCache.Instance.ClearAsync();
            // _settings.Values.Clear();
        }

        private async Task UpdateLoggedInUser()
        {
            var loggedInUser = await _instaApi.GetCurrentUserAsync();
            LoggedInUser = loggedInUser.Value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(LoggedInUser)));
        }

        private async void OnMessageSyncReceived(object sender, List<MessageSyncEventArgs> data)
        {
            try
            {
                var updateInbox = false;
                foreach (var syncEvent in data)
                {
                    var itemData = syncEvent.Data[0];
                    if (syncEvent.SeqId > Inbox.SeqId)
                    {
                        Inbox.SeqId = syncEvent.SeqId;
                        Inbox.SnapshotAt = itemData.Item.Timestamp;
                    }
                    var segments = itemData.Path.Trim('/').Split('/');
                    var threadId = segments[2];
                    if (string.IsNullOrEmpty(threadId)) continue;
                    var thread = InboxThreads.SingleOrDefault(wrapper => wrapper.ThreadId == threadId);
                    if (thread == null)
                    {
                        if (!updateInbox) updateInbox = itemData.Op == "add";
                        continue;
                    }

                    switch (itemData.Op)
                    {
                        case "add":
                        {
                            await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
                                () => thread.AddItem(itemData.Item));
                            break;
                        }
                        case "replace":
                        {
                            if (itemData.Path.Contains("has_seen", StringComparison.Ordinal) && long.TryParse(segments[4], out var userId))
                            {
                                thread.UpdateLastSeenAt(userId, itemData.Item.Timestamp, itemData.Item.ItemId);
                                continue;
                            }
                            var item = thread.ObservableItems.SingleOrDefault(x => x.ItemId == itemData.Item.ItemId);
                            if (item == null) continue;

                            await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Low, () =>
                            {
                                if (itemData.Item.Reactions == null)
                                {
                                    item.Reactions.Clear();
                                }
                                else
                                {
                                    item.Reactions?.Update(new InstaDirectReactionsWrapper(itemData.Item.Reactions, thread.ViewerId),
                                        thread.Users);
                                }
                            });
                            break;
                        }
                    }
                }
                if (updateInbox) await Inbox.UpdateInbox();
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
            try
            {
                var result = await _instaApi.GetThreadAsync(SelectedThread.ThreadId, PaginationParameters.MaxPagesToLoad(1));
                if (result.IsSucceeded)
                    await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(
                        CoreDispatcherPriority.Normal,
                        () => { SelectedThread.Update(result.Value); });
            }
            catch (Exception e)
            {
#if !DEBUG
                Crashes.TrackError(e);
#endif
            }
        }

        public async void SendLike()
        {
            try
            {
                var selectedThread = SelectedThread;
                if (string.IsNullOrEmpty(selectedThread.ThreadId)) return;
                var result = await _instaApi.SendLikeAsync(selectedThread.ThreadId);
                if (result.IsSucceeded) UpdateInboxAndSelectedThread();
            }
            catch (Exception)
            {
                await HandleException("Failed to send like");
            }
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
            try
            {
                if (!string.IsNullOrEmpty(selectedThread.ThreadId))
                {
                    if (links.Any())
                    {
                        ackResult = await _instaApi.SendLinkAsync(content, links, selectedThread.ThreadId);
                        return;
                    }

                    result = await _instaApi.SendTextAsync(null, selectedThread.ThreadId, content);
                }
                else
                {
                    if (links.Any())
                    {
                        ackResult = await _instaApi.SendLinkToRecipientsAsync(content, links,
                            selectedThread.Users.Select(x => x.Pk).ToArray());
                        return;
                    }

                    result = await _instaApi.SendTextAsync(selectedThread.Users.Select(x => x.Pk),
                        null, content);
                }
            }
            catch (Exception e)
            {
#if !DEBUG
                Crashes.TrackError(e);
#endif
                await HandleException("Failed to send message");
                return;
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
            try
            {
                if (file.ContentType.Contains("image", StringComparison.OrdinalIgnoreCase))
                {
                    var properties = await file.Properties.GetImagePropertiesAsync();
                    int imageHeight = (int) properties.Height;
                    int imageWidth = (int) properties.Width;
                    IBuffer buffer;
                    if (properties.Width > 1080 || properties.Height > 1080)
                    {
                        buffer = await Helpers.CompressImage(file, 1080, 1080);
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

                    await SendBuffer(buffer, imageWidth, imageHeight, progress);
                }


                // Not yet tested
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
                        new InstaVideoUpload(instaVideo, thumbnailImage), SelectedThread.ThreadId);
                }
            }
            catch (Exception e)
            {
#if !DEBUG
                Crashes.TrackError(e);
#endif
                await HandleException("Failed to send message");
            }
        }


        /// <summary>
        /// For screenshot in clipboard
        /// </summary>
        /// <param name="stream"></param>
        public async void SendStream(IRandomAccessStream stream, Action<UploaderProgress> progress)
        {
            try
            {
                stream.Seek(0);
                var bitmap = new BitmapImage();
                await bitmap.SetSourceAsync(stream);
                int imageHeight = bitmap.PixelHeight;
                int imageWidth = bitmap.PixelWidth;

                IBuffer buffer;
                if (imageWidth > 1080 || imageHeight > 1080)
                {
                    buffer = await Helpers.CompressImage(stream, 1080, 1080);
                    double widthRatio = (double)1080 / imageWidth;
                    double heightRatio = (double)1080 / imageHeight;
                    double scaleRatio = Math.Min(widthRatio, heightRatio);
                    imageHeight = (int)Math.Floor(imageHeight * scaleRatio);
                    imageWidth = (int)Math.Floor(imageWidth * scaleRatio);
                }
                else
                {
                    buffer = new Buffer((uint) stream.Size);
                    await stream.WriteAsync(buffer);
                }

                await SendBuffer(buffer, imageWidth, imageHeight, progress);
            }
            catch (Exception e)
            {
#if !DEBUG
                Crashes.TrackError(e);
#endif
                await HandleException("Failed to send message");
            }
        }

        private async Task SendBuffer(IBuffer buffer, int imageWidth, int imageHeight, Action<UploaderProgress> progress)
        {
            var instaImage = new InstaImage
            {
                UploadBuffer = buffer,
                Width = imageWidth,
                Height = imageHeight
            };
            if (string.IsNullOrEmpty(SelectedThread.ThreadId)) return;
            var uploadId = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            await _instaApi.SendDirectPhotoAsync(instaImage, SelectedThread.ThreadId, uploadId, progress);
        }

        public async void UpdateInboxAndSelectedThread()
        {
            _lastUpdated = DateTime.Now;
            var selected = SelectedThread;
            await Inbox.UpdateInbox();
            if (selected == null) return;
            if (InboxThreads.Contains(selected) && SelectedThread != selected) SelectedThread = selected;
            await UpdateSelectedThread();
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
                await Task.Delay(500, cancellationToken); // Delay so we don't search something mid typing
            }
            catch (Exception)
            {
                return;
            }
            if (cancellationToken.IsCancellationRequested) return;

            var result = await _instaApi.GetRankedRecipientsByUsernameAsync(query);
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
                x.LastPermanentItem.Text = x.Users.Count == 1 ? x.Users?[0].FullName : $"{x.Users.Count} participants";
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
                var result = await _instaApi.GetThreadByParticipantsAsync(userIds);
                if (!result.IsSucceeded) return;
                thread = result.Value != null && result.Value.Users.Count > 0 ? 
                    new InstaDirectInboxThreadWrapper(result.Value, _instaApi) : new InstaDirectInboxThreadWrapper(placeholderThread.Users?[0], _instaApi);
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
                thread.LastPermanentItem = new DirectItem() {Description = thread.Users?[0].FullName};
            }

            SelectedThread = thread;
        }

        public async void MarkLatestItemSeen(InstaDirectInboxThreadWrapper thread)
        {
            try
            {
                if (thread == null || string.IsNullOrEmpty(thread.ThreadId)) return;
                if (thread.LastSeenAt.TryGetValue(thread.ViewerId, out var lastSeen))
                {
                    if (string.IsNullOrEmpty(thread.LastPermanentItem?.ItemId) || 
                        lastSeen.ItemId == thread.LastPermanentItem.ItemId ||
                        thread.LastPermanentItem.FromMe) return;
                    await _instaApi.MarkItemSeenAsync(thread.ThreadId, thread.LastPermanentItem.ItemId).ConfigureAwait(false);
                }
            }
            catch (Exception e)
            {
#if !DEBUG
                Crashes.TrackError(e);
#endif
            }
        }

        public static IAsyncAction HandleException(string message = null, Exception e = null)
        {
            if (string.IsNullOrEmpty(message))
            {
                message = "An unexpected error has occured. Indirect doesn't know how to proceed next and may crash. " +
                          "If this happens frequently, please submit an issue on Indirect's Github page.\n\n" +
                          "https://github.com/huynhsontung/Indirect";
            }

            return CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.High, async () =>
            {
                try
                {
                    var dialog = new ContentDialog()
                    {
                        Title = "An error occured",
                        Content = new ScrollViewer()
                        {
                            Content = new TextBlock()
                            {
                                Text = message,
                                TextWrapping = TextWrapping.Wrap,
                                IsTextSelectionEnabled = true
                            },
                            HorizontalScrollMode = ScrollMode.Disabled,
                            VerticalScrollMode = ScrollMode.Auto,
                            MaxWidth = 400
                        },
                        CloseButtonText = "Close",
                        DefaultButton = ContentDialogButton.Close
                    };
                    await dialog.ShowAsync();
                }
                catch (Exception innerException)
                {
                    Debug.WriteLine(innerException);
                }

                // Intentionally crash the app
                if (e != null) throw e;
            });
        }
    }
}
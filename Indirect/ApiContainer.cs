using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.Networking.Sockets;
using Windows.Security.Cryptography;
using Windows.Storage;
using Windows.Storage.FileProperties;
using Windows.Storage.Streams;
using Windows.UI.Core;
using Indirect.Notification;
using Indirect.Utilities;
using Indirect.Wrapper;
using InstaSharper.API;
using InstaSharper.API.Builder;
using InstaSharper.API.Push;
using InstaSharper.Classes;
using InstaSharper.Classes.Models.Direct;
using InstaSharper.Classes.Models.Media;
using InstaSharper.Classes.Models.User;
using InstaSharper.Classes.ResponseWrappers.Direct;
using InstaSharper.Enums;
using InstaSharper.Logger;
using Microsoft.AppCenter.Crashes;
using Microsoft.Toolkit.Uwp;
using Microsoft.Toolkit.Uwp.UI;

namespace Indirect
{
    internal class ApiContainer : INotifyPropertyChanged
    {
        // Todo: handle exceptions thrown by _instaApi like no network connection
        public const string INSTA_API_PROP_NAME = "InstaApi";
        private const string STATE_FILE_NAME = "state.bin";
        private const string SOCKET_ID = "mqtt_fbns";

        private static readonly InstaApiBuilder Builder = InstaApiBuilder.CreateBuilder();

        private InstaApi _instaApi;
        private readonly StorageFolder _localFolder = ApplicationData.Current.LocalFolder;
        private StorageFile _stateFile;
        private FbnsConnectionData _pushData;
        private DateTime _lastUpdated = DateTime.Now;
        private CancellationTokenSource _searchCancellationToken;
        private InstaDirectInboxThreadWrapper _selectedThread;

        public event PropertyChangedEventHandler PropertyChanged;

        public PushClient PushClient { get; private set; }
        public SyncClient SyncClient { get; private set; }
        public InstaDirectInboxWrapper Inbox { get; private set; }

        public IncrementalLoadingCollection<InstaDirectInboxWrapper, InstaDirectInboxThreadWrapper> InboxThreads =>
            Inbox.Threads;

        public InstaCurrentUser LoggedInUser { get; private set; }

        public InstaDirectInboxThreadWrapper SelectedThread
        {
            get => _selectedThread;
            set
            {
                _selectedThread = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectedThread)));
            }
        }

        public bool IsUserAuthenticated { get; private set; }

        public StateData StateData
        {
            get
            {
                var data = _instaApi?.GetStateData();
                if (data == null) return new StateData();
                data.FbnsConnectionData = PushClient?.ConnectionData;
                return data;
            }
        }


        private ApiContainer() {}

        public static async Task<ApiContainer> Factory()
        {
            var instance = new ApiContainer();
            await instance.CreateStateFile();
            await instance.LoadStateFromStorage();
            return instance;
        }

        public async void OnLoggedIn()
        {
            if (!_instaApi.IsUserAuthenticated) throw new Exception("User is not logged in.");
            // CoreApplication.Properties.Add(INSTA_API_PROP_NAME, _instaApi);
            SyncClient = new SyncClient(_instaApi);
            SyncClient.MessageReceived += OnMessageSyncReceived;
            PushClient = new PushClient(_instaApi, _pushData);
            Inbox = new InstaDirectInboxWrapper(_instaApi);
            Inbox.FirstUpdated += (seqId, snapshotAt) => SyncClient.Start(seqId, snapshotAt);
            await UpdateLoggedInUser();
            PushClient.Start();
        }

        public void SetSelectedThreadNull()
        {
            SelectedThread = null;
        }

        private async Task CreateStateFile()
        {
            _stateFile = (StorageFile) await _localFolder.TryGetItemAsync(STATE_FILE_NAME);
            if (_stateFile == null)
            {
                _stateFile =
                    await _localFolder.CreateFileAsync(STATE_FILE_NAME, CreationCollisionOption.ReplaceExisting);
                IsUserAuthenticated = false;
            }
        }

        private async Task LoadStateFromStorage()
        {
            using (var stateStream = await _stateFile.OpenStreamForReadAsync())
            {
                if (stateStream.Length > 0)
                {
                    var formatter = new BinaryFormatter();
                    stateStream.Seek(0, SeekOrigin.Begin);
                    var stateData = (StateData) formatter.Deserialize(stateStream);
                    if (stateData.Cookies != null)
                    {
                        _instaApi = Builder.LoadStateData(stateData)
                            .UseLogger(new DebugLogger(LogLevel.All))
                            .Build();
                        IsUserAuthenticated = _instaApi.IsUserAuthenticated;
                        _pushData = stateData.FbnsConnectionData;
                    }
                    else
                    {
                        _pushData = new FbnsConnectionData();
                    }
                }
                else
                {
                    _pushData = new FbnsConnectionData();
                }
            }
        }

        public async Task WriteStateToStorage()
        {
            using (var stateFileStream = await _stateFile.OpenStreamForWriteAsync())
            {
                var formatter = new BinaryFormatter();
                formatter.Serialize(stateFileStream, StateData);
            }
        }

        public async Task<IResult<InstaLoginResult>> Login(string username, string password)
        {
            var session = new UserSessionData {UserName = username, Password = password};
            _instaApi = Builder.SetUser(session)
                .UseLogger(new DebugLogger(LogLevel.All))
                .Build();

            var logInResult = await _instaApi.LoginAsync();
            await WriteStateToStorage();
            return logInResult;
        }

        public async Task Logout()
        {
            // Instagram doesnt support logout anymore
            // var result = await _instaApi.LogoutAsync();
            SyncClient.Shutdown();
            await PushClient.Shutdown();
            _pushData = new FbnsConnectionData();
            await ImageCache.Instance.ClearAsync();
            await VideoCache.Instance.ClearAsync();
            if (CoreApplication.Properties.ContainsKey(INSTA_API_PROP_NAME))
                CoreApplication.Properties.Remove(INSTA_API_PROP_NAME);
            using (var stream = await _stateFile.OpenStreamForWriteAsync())
            {
                stream.SetLength(0);
            }
        }

        private async Task UpdateLoggedInUser()
        {
            var loggedInUser = await _instaApi.GetCurrentUserAsync();
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
                if (itemData.Op == "add")
                {
                    await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
                        () => thread.AddItem(itemData.Item));
                    _ = Inbox.UpdateInbox();
                }

                if (itemData.Op == "replace")
                {
                    // todo: Handle items seen
                    if (itemData.Path.Contains("has_seen")) return;
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
                if (DateTime.Now - _lastUpdated > TimeSpan.FromSeconds(0.5))
                    UpdateInboxAndSelectedThread();
            }
            Debug.WriteLine("Sync(s) received.");
        }

        public async Task UpdateSelectedThread()
        {
            if (SelectedThread == null)
                return;
            var result = await _instaApi.MessagingProcessor.GetThreadAsync(SelectedThread.ThreadId,
                PaginationParameters.MaxPagesToLoad(1));
            if (result.Succeeded)
                await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(
                    CoreDispatcherPriority.Normal,
                    () => { SelectedThread.Update(result.Value); });
        }

        public async void SendLike()
        {
            var selectedThread = SelectedThread;
            if (string.IsNullOrEmpty(selectedThread.ThreadId)) return;
            var result = await _instaApi.MessagingProcessor.SendDirectLikeAsync(selectedThread.ThreadId);
            if (result.Succeeded) UpdateInboxAndSelectedThread();
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
                x.StartsWith("http://") || x.StartsWith("https://") || x.StartsWith("www.")).ToList();
            // var hashtags = tokens.Where(x => x.StartsWith('#')).Select(x => x.Substring(1)).ToList();
            IResult<List<InstaDirectInboxThread>> result;
            IResult<ItemAckPayloadResponse> ackResult = new Result<ItemAckPayloadResponse>(false, new ItemAckPayloadResponse());   // for links and hashtags
            if (!string.IsNullOrEmpty(selectedThread.ThreadId))
            {
                if (links.Any())
                {
                    ackResult = await _instaApi.MessagingProcessor.SendDirectLinkAsync(content, links, selectedThread.ThreadId);
                }

                // if (hashtags.Any())
                // {
                //     ackResult = await _instaApi.MessagingProcessor.SendDirectHashtagAsync(content, null,
                //         selectedThread.ThreadId);
                // }

                if (ackResult.Succeeded)
                {
                    UpdateInboxAndSelectedThread();
                    return;
                }

                result = await _instaApi.MessagingProcessor.SendDirectTextAsync(null, selectedThread.ThreadId, content);
            }
            else
            {
                if (links.Any())
                {
                    ackResult = await _instaApi.MessagingProcessor.SendDirectLinkToRecipientsAsync(content, links,
                        selectedThread.Users.Select(x => x.Pk.ToString()).ToArray());
                    if (ackResult.Succeeded)
                    {
                        UpdateInboxAndSelectedThread();
                        return;
                    }

                }
                result = await _instaApi.MessagingProcessor.SendDirectTextAsync(selectedThread.Users.Select(x => x.Pk),
                    null, content);
            }

            if (result.Succeeded && result.Value.Count > 0)
            {
                selectedThread.Update(result.Value[0]);
                await Inbox.UpdateInbox();
                if (SelectedThread == null) SelectedThread = selectedThread;
            }
        }

        public async void SendFile(StorageFile file, Action<InstaUploaderProgress> progress)
        {
            if (file.ContentType.Contains("image"))
            {
                var properties = await file.Properties.GetImagePropertiesAsync();
                int imageHeight = (int) properties.Height;
                int imageWidth = (int) properties.Width;
                byte[] bytes;
                if (properties.Width > 1080 || properties.Height > 1080)
                {
                    bytes = await Helpers.CompressImage(file, 1080, 1080);
                    double widthRatio = (double)1080 / imageWidth;
                    double heightRatio = (double)1080 / imageHeight;
                    double scaleRatio = Math.Min(widthRatio, heightRatio);
                    imageHeight = (int) Math.Floor(imageHeight * scaleRatio);
                    imageWidth = (int) Math.Floor(imageWidth * scaleRatio);
                }
                else
                {
                    var buffer = await FileIO.ReadBufferAsync(file);
                    bytes = buffer.ToArray();
                }
                var instaImage = new InstaImage
                {
                    ImageBytes = bytes, 
                    Width = imageWidth, 
                    Height = imageHeight, 
                    Url = file.Path
                };
                if (string.IsNullOrEmpty(SelectedThread.ThreadId) || SelectedThread.PendingScore == 0) return;
                await _instaApi.MessagingProcessor.SendDirectPhotoAsync(instaImage, SelectedThread.ThreadId,
                    SelectedThread.PendingScore, progress);
                return;
            }

            if (file.ContentType.Contains("video"))
            {
                var properties = await file.Properties.GetVideoPropertiesAsync();
                var buffer = await FileIO.ReadBufferAsync(file);
                var bytes = buffer.ToArray();
                var instaVideo = new InstaVideo()
                {
                    VideoBytes = bytes,
                    Width = (int) properties.Width,
                    Height = (int) properties.Height,
                    Url = file.Path
                };
                var thumbnail = await file.GetThumbnailAsync(ThumbnailMode.VideosView);
                var thumbnailBytes = new byte[thumbnail.Size];
                await thumbnail.ReadAsync(thumbnailBytes.AsBuffer(), (uint) thumbnail.Size, InputStreamOptions.None);
                var thumbnailImage = new InstaImage()
                {
                    ImageBytes = thumbnailBytes,
                    Width = (int) thumbnail.OriginalWidth,
                    Height = (int) thumbnail.OriginalHeight
                };
                await _instaApi.MessagingProcessor.SendDirectVideoAsync(progress,
                    new InstaVideoUpload(instaVideo, thumbnailImage), SelectedThread.ThreadId);
                return;
            }
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
                await Task.Delay(500, cancellationToken);   // Delay so we don't search something mid typing
            }
            catch (Exception)
            {
                return;
            }
            if (cancellationToken.IsCancellationRequested) return;

            var result = await _instaApi.MessagingProcessor.GetRankedRecipientsByUsernameAsync(query);
            if (!result.Succeeded) return;
            var recipients = result.Value;
            var threadsFromUser = recipients.Users.Select(x => new InstaDirectInboxThreadWrapper(x, _instaApi)).ToList();
            var threadsFromRankedThread = recipients.Threads.Select(x => new InstaDirectInboxThreadWrapper(x, _instaApi)).ToList();
            var list = new List<InstaDirectInboxThreadWrapper>(threadsFromRankedThread.Count + threadsFromUser.Count);
            list.AddRange(threadsFromRankedThread);
            list.AddRange(threadsFromUser);
            var decoratedList = list.Select(x =>
            {
                if (x.LastPermanentItem == null) x.LastPermanentItem = new InstaDirectInboxItem();
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
                var result = await _instaApi.MessagingProcessor.GetThreadByParticipantsAsync(userIds);
                if (!result.Succeeded) return;
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
                thread.LastPermanentItem = new InstaDirectInboxItem {Text = thread.Users[0].FullName};
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
                await _instaApi.MessagingProcessor.MarkItemSeenAsync(thread.ThreadId,
                    thread.LastPermanentItem.ItemId);
            }
        }
    }
}
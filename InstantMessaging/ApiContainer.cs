using InstantMessaging.Wrapper;
using InstaSharper.API;
using InstaSharper.API.Builder;
using InstaSharper.Classes;
using InstaSharper.Logger;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading.Tasks;
using Windows.Storage;
using ColorCode.Common;
using InstantMessaging.Notification;
using InstaSharper.API.Push;
using InstaSharper.Classes.Models.Direct;
using InstaSharper.Classes.Models.User;
using InstaSharper.Enums;

namespace InstantMessaging
{
    public class ApiContainer : INotifyPropertyChanged
    {
        // Todo: handle exceptions thrown by _instaApi like no network connection
        private const string STATE_FILE_NAME = "state.bin";
        private const string PUSH_STATE_FILE_NAME = "push.bin";

        private IInstaApi _instaApi;
        private readonly StorageFolder _localFolder = ApplicationData.Current.LocalFolder;
        private StorageFile _stateFile;
        private StorageFile _pushStateFile;
        private FbnsConnectionData _pushData;
        private UserSessionData _userSession;
        private PushClient _pushClient;

        public event EventHandler<PushNotification> NotificationReceived;

        public event PropertyChangedEventHandler PropertyChanged;

        public ObservableCollection<InstaDirectInboxThreadWrapper> InboxThreads { get; } = new ObservableCollection<InstaDirectInboxThreadWrapper>();
        public InstaCurrentUserWrapper LoggedInUser { get; private set; }
        public InstaDirectInboxThreadWrapper SelectedThread { get; set; }
        public bool IsUserAuthenticated { get; private set; }

        private ApiContainer() {}

        public static async Task<ApiContainer> Factory()
        {
            var instance = new ApiContainer();
            await instance.CreateStateFile();
            await instance.LoadStateFromStorage();
            return instance;
        }

        public void SetSelectedThreadNull()
        {
            SelectedThread = null;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectedThread)));
        }

        private async Task CreateStateFile()
        {
            _stateFile = (StorageFile) await _localFolder.TryGetItemAsync(STATE_FILE_NAME);
            if (_stateFile == null)
            {
                _stateFile = await _localFolder.CreateFileAsync(STATE_FILE_NAME, CreationCollisionOption.ReplaceExisting);
                IsUserAuthenticated = false;
            }

            _pushStateFile = (StorageFile)await _localFolder.TryGetItemAsync(PUSH_STATE_FILE_NAME) ?? await _localFolder.CreateFileAsync(PUSH_STATE_FILE_NAME);
        }

        private async Task LoadStateFromStorage()
        {
            using (var stateStream = await _stateFile.OpenStreamForReadAsync())
            {
                if (stateStream.Length > 0)
                {
                    stateStream.Seek(0, SeekOrigin.Begin);
                    _instaApi = InstaApiBuilder.CreateBuilder()
                        .LoadStateDataFromStream(stateStream)
                        .UseLogger(new DebugLogger(LogLevel.All))
                        .Build();
                    IsUserAuthenticated = _instaApi.IsUserAuthenticated;
                    if(IsUserAuthenticated) _userSession = _instaApi.GetLoggedUser();
                }
            }

            using (var stateStream = await _pushStateFile.OpenStreamForReadAsync())
            {
                if (stateStream.Length > 0)
                {
                    stateStream.Seek(0, SeekOrigin.Begin);
                    var formatter = new BinaryFormatter();
                    _pushData = (FbnsConnectionData) formatter.Deserialize(stateStream);
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
                var state = _instaApi.GetStateDataAsStream();
                state.Seek(0, SeekOrigin.Begin);
                state.CopyTo(stateFileStream);
            }

            using (var stateFileStream = await _pushStateFile.OpenStreamForWriteAsync())
            {
                var formatter = new BinaryFormatter();
                var stream = new MemoryStream();
                formatter.Serialize(stream, _pushData);
                stream.Seek(0, SeekOrigin.Begin);
                stream.CopyTo(stateFileStream);
            }
        }

        public async Task<IResult<InstaLoginResult>> Login(string username, string password)
        {

            var session = new UserSessionData {UserName = username, Password = password};
            _instaApi = InstaApiBuilder.CreateBuilder()
                .SetUser(session)
                .UseLogger(new DebugLogger(LogLevel.All))
                .Build();

            var logInResult = await _instaApi.LoginAsync();
            await WriteStateToStorage();
            _userSession = _instaApi.GetLoggedUser();
            return logInResult;
        }

        public async Task<IResult<bool>> Logout()
        {
            var result = await _instaApi.LogoutAsync();
            await _pushClient.Shutdown();
            _pushData = null;
            if (result.Value)
                await WriteStateToStorage();
            return result;
        }


        public async Task<IResult<InstaDirectInboxContainer>> GetInboxAsync()
        {
            if (_instaApi == null)
                throw new NullReferenceException("Api has not been initialized");

            var result = await _instaApi.MessagingProcessor.GetDirectInboxAsync(PaginationParameters.MaxPagesToLoad(1));
            InstaDirectInbox inbox;
            if (result.Succeeded)
                inbox = result.Value.Inbox;
            else throw new OperationCanceledException("Failed to fetch Inbox");

            foreach (var thread in inbox.Threads)
            {
                var existed = false;
                foreach (var existingThread in InboxThreads)
                {
                    if (thread.ThreadId != existingThread.ThreadId) continue;
                    existingThread.Update(thread);
                    existed = true;
                    break;
                }

                if (!existed) InboxThreads.Add(new InstaDirectInboxThreadWrapper(thread, _instaApi));
            }

            InboxThreads.SortStable((x,y)=> y.LastActivity.CompareTo(x.LastActivity));

            return result;
        }

        public async Task UpdateLoggedInUser()
        {
            var loggedInUser = await _instaApi.GetCurrentUserAsync();
            LoggedInUser = new InstaCurrentUserWrapper(loggedInUser.Value, _instaApi);
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(LoggedInUser)));
        }

        public async Task StartPushClient()
        {
            _pushClient = new PushClient(_instaApi, _pushData);
            _pushClient.MessageReceived += OnNotificationReceived;
            await _pushClient.Start();
        }

        private async void OnNotificationReceived(object sender, MessageReceivedEventArgs args)
        {
            NotificationReceived?.Invoke(this, args.NotificationContent);
            await GetInboxAsync();
            if (SelectedThread != null) await OnThreadChange(SelectedThread);
        }

        public async Task<IResult<InstaDirectInboxThread>> OnThreadChange(InstaDirectInboxThreadWrapper thread)
        {
            if (thread == null)
                throw new ArgumentNullException(nameof(thread));
            var result = await _instaApi.MessagingProcessor.GetDirectInboxThreadAsync(thread.ThreadId, PaginationParameters.MaxPagesToLoad(1));
            if (result.Succeeded)
            {
                thread.UpdateItemList(result.Value.Items);
            }
            return result;
        }

        // Send message to the current selected recipient
        public async Task<IResult<InstaDirectInboxThreadList>> SendMessage(string content)
        {
            var result = await _instaApi.MessagingProcessor.SendDirectTextAsync(SelectedThread.Users.FirstOrDefault()?.Pk.ToString(), SelectedThread.ThreadId, content);
            return result;
        }

    }
}

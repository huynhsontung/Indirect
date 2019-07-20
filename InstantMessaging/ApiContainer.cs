using InstantMessaging.Wrapper;
using InstaSharper.API;
using InstaSharper.API.Builder;
using InstaSharper.Classes;
using InstaSharper.Logger;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.Storage;
using InstaSharper.Classes.Models.Direct;
using InstaSharper.Enums;

namespace InstantMessaging
{
    public class ApiContainer : INotifyPropertyChanged
    {
        // Todo: handle exceptions thrown by _instaApi like no network connection
        private const string STATE_FILE_NAME = "state.bin";

        private IInstaApi _instaApi;
        private readonly StorageFolder _localFolder = ApplicationData.Current.LocalFolder;
        private StorageFile _stateFile;
        private UserSessionData _userSession;
        private InstaDirectInbox _inbox;

        public event PropertyChangedEventHandler PropertyChanged;
        public InstaDirectInboxThreadWrapper SelectedThread { get; set; }
        public ObservableCollection<InstaDirectInboxThreadWrapper> InboxThreads { get; } = new ObservableCollection<InstaDirectInboxThreadWrapper>();
        public long UnseenCount => _inbox?.UnseenCount ?? 0;
        public bool IsUserAuthenticated { get; private set; }

        private ApiContainer() {}

        public static async Task<ApiContainer> Factory()
        {
            var instance = new ApiContainer();
            await instance.CreateStateFile();
            await instance.LoadStateFromStorage();
            return instance;
        }

        private async Task CreateStateFile()
        {
            _stateFile = (StorageFile) await _localFolder.TryGetItemAsync(STATE_FILE_NAME);
            if (_stateFile == null)
            {
                _stateFile = await _localFolder.CreateFileAsync(STATE_FILE_NAME, CreationCollisionOption.ReplaceExisting);
                IsUserAuthenticated = false;
            }
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
        }

        public async Task<IResult<InstaLoginResult>> Login(string username, string password)
        {

            _userSession = new UserSessionData {UserName = username, Password = password};
            _instaApi = InstaApiBuilder.CreateBuilder()
                .SetUser(_userSession)
                .UseLogger(new DebugLogger(LogLevel.All))
                .Build();

            var logInResult = await _instaApi.LoginAsync();

            return logInResult;
        }

        public async Task<IResult<bool>> Logout()
        {
            var result = await _instaApi.LogoutAsync();
            if (result.Value)
                await WriteStateToStorage();
            return result;
        }

        public async Task StartPushClient()
        {
            await _instaApi.PushClient.Start();
        }

        public async Task<IResult<InstaDirectInboxContainer>> GetInboxAsync()
        {
            if (_instaApi == null)
                throw new NullReferenceException("Api has not been initialized");
            var result = await _instaApi.MessagingProcessor.GetDirectInboxAsync(PaginationParameters.MaxPagesToLoad(1));
            if (result.Succeeded)
                _inbox = result.Value.Inbox;

            foreach(var thread in _inbox.Threads)
            {
                InboxThreads.Add(new InstaDirectInboxThreadWrapper(thread, _instaApi));
            }

            return result;
        }

        public async Task<IResult<InstaDirectInboxThread>> GetInboxThread(InstaDirectInboxThreadWrapper thread)
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

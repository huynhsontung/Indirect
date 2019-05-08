using InstaSharper.API;
using InstaSharper.API.Builder;
using InstaSharper.Classes;
using InstaSharper.Logger;
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;
using System.Diagnostics;
using System.Collections.ObjectModel;
using InstaSharper.Classes.Models;
using System.ComponentModel;

namespace InstantMessaging
{   
    public class ApiContainer : INotifyPropertyChanged
    {
        private IInstaApi _instaApi;
        private StorageFolder _localFolder = ApplicationData.Current.LocalFolder;
        private StorageFile _stateFile;
        private UserSessionData _userSession;
        private const string StateFileName = "state.bin";
        private InstaDirectInbox _inbox;

        public event PropertyChangedEventHandler PropertyChanged;

        public ObservableCollection<InstaDirectInboxThread> InboxThreads { get; } = new ObservableCollection<InstaDirectInboxThread>();
        public long UnseenCount
        {
            get
            {
                if (_inbox != null)
                    return _inbox.UnseenCount;
                else return 0;
            }
        }
        public bool IsUserAuthenticated { get; private set; }

        private ApiContainer() {}

        public static async Task<ApiContainer> Factory()
        {
            var instance = new ApiContainer();
            await instance.LoadStateFromStorage();
            return instance;
        }

        public static ApiContainer Factory(string username, string password)
        {
            var instance = new ApiContainer();
            instance._userSession = new UserSessionData
            {
                UserName = username,
                Password = password
            };

            instance._instaApi = InstaApiBuilder.CreateBuilder()
                .SetUser(instance._userSession)
                .SetRequestDelay(RequestDelay.FromSeconds(1, 2))
                .UseLogger(new DebugLogger(LogLevel.All))
                .Build();
            return instance;
        }

        private async Task LoadStateFromStorage()
        {
            _stateFile = (StorageFile)await _localFolder.TryGetItemAsync(StateFileName);
            if (_stateFile != null)
            {
                using (Stream stateStream = await _stateFile.OpenStreamForReadAsync())
                {
                    if (stateStream.Length > 0)
                    {
                        stateStream.Seek(0, SeekOrigin.Begin);
                        _instaApi = InstaApiBuilder.CreateBuilder()
                            .LoadStateFromStream(stateStream)
                            .SetRequestDelay(RequestDelay.FromSeconds(1, 2))
                            .UseLogger(new DebugLogger(LogLevel.All))
                            .Build();
                        IsUserAuthenticated = _instaApi.IsUserAuthenticated;
                    }
                }
            }
            else
            {
                _stateFile = await _localFolder.CreateFileAsync(StateFileName, CreationCollisionOption.OpenIfExists);
                IsUserAuthenticated = false;
            }
        }

        public async Task<IResult<InstaLoginResult>> Login()
        {
            if (_instaApi == null)
                throw new ArgumentNullException("Api has not been initialized");
            if (!_instaApi.IsUserAuthenticated)
            {
                // login
                Debug.WriteLine($"Logging in as {_userSession.UserName}");
                //delay.Disable();
                var logInResult = await _instaApi.LoginAsync();
                //delay.Enable();

                if (!logInResult.Succeeded)
                {
                    return logInResult;
                }
                
            }

            // Write state back to storage
            using (var stateFileStream = await _stateFile.OpenStreamForWriteAsync())
            {
                var state = _instaApi.GetStateDataAsStream();
                state.Seek(0, SeekOrigin.Begin);
                state.CopyTo(stateFileStream);
            } 

            return Result.Success(InstaLoginResult.Success);
        }

        public async Task GetInboxAsync()
        {
            if (_instaApi == null)
                throw new ArgumentNullException("Api has not been initialized");
            var result = await _instaApi.GetDirectInboxAsync();
            if (result.Succeeded)
                _inbox = result.Value.Inbox;

            foreach(var thread in _inbox.Threads)
            {
                // ListView puts lower indicies to the bottom
                InboxThreads.Add(thread);
            }
        }

        private async Task GetMessaging()
        {
            //var recipientsResult = await _instaApi.GetRankedRecipientsAsync();
            //if (!recipientsResult.Succeeded)
            //{
            //    Debug.WriteLine("Unable to get ranked recipients");
            //    return;
            //}

            //Debug.WriteLine($"Got {recipientsResult.Value.Threads.Count} ranked threads");
            //foreach (var thread in recipientsResult.Value.Threads)
            //    Debug.WriteLine($"Threadname: {thread.ThreadTitle}, users: {thread.Users.Count}");

            //var inboxThreads = await _instaApi.GetDirectInboxAsync();
            //if (!inboxThreads.Succeeded)
            //{
            //    Debug.WriteLine("Unable to get inbox");
            //    return;
            //}
            //Debug.WriteLine($"Got {inboxThreads.Value.Inbox.Threads.Count} inbox threads");
            //foreach (var thread in inboxThreads.Value.Inbox.Threads)
            //    Debug.WriteLine($"Threadname: {thread.Title}, users: {thread.Users.Count}");

            var singleThreadId = "340282366841710300949128145896318328529";
            var singleThread = await _instaApi.GetDirectInboxThreadAsync(singleThreadId);
            if (singleThread.Succeeded)
            {
                foreach (var text in singleThread.Value.Items)
                    Debug.WriteLine(text.Text, "Message Content");
            }
        }

    }
}

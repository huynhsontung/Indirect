using InstantMessaging.Wrapper;
using InstaSharper.API;
using InstaSharper.API.Builder;
using InstaSharper.Classes;
using InstaSharper.Classes.Models;
using InstaSharper.Logger;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.ServiceModel;
using System.Threading.Tasks;
using Windows.Security.Cryptography.Certificates;
using Windows.Storage;
using DotNetty.Codecs.Mqtt;
using DotNetty.Codecs.Mqtt.Packets;
using DotNetty.Transport.Bootstrapping;
using DotNetty.Transport.Channels;
using DotNetty.Transport.Channels.Sockets;
using InstaSharper.API.Push;

namespace InstantMessaging
{
    public class ApiContainer : INotifyPropertyChanged
    {
        private IInstaApi _instaApi;
        private readonly StorageFolder _localFolder = ApplicationData.Current.LocalFolder;
        private StorageFile _stateFile;
        private UserSessionData _userSession;
        private const string STATE_FILE_NAME = "state.bin";
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

        public static async Task<ApiContainer> Factory(string username, string password)
        {
            var instance = new ApiContainer
            {
                _userSession = new UserSessionData {UserName = username, Password = password}
            };

            await instance.CreateStateFile();
            instance._instaApi = InstaApiBuilder.CreateBuilder()
                .SetUser(instance._userSession)
                .UseLogger(new DebugLogger(LogLevel.All))
                .Build();
            return instance;
        }

        private async Task CreateStateFile()
        {
            _stateFile = (StorageFile) await _localFolder.TryGetItemAsync(STATE_FILE_NAME);
            if (_stateFile == null)
            {
                _stateFile = await _localFolder.CreateFileAsync(STATE_FILE_NAME, CreationCollisionOption.OpenIfExists);
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
                        .LoadStateFromStream(stateStream)
                        .UseLogger(new DebugLogger(LogLevel.All))
                        .Build();
                    IsUserAuthenticated = _instaApi.IsUserAuthenticated;
                }
            }
        }

        private async Task WriteStateToStorage()
        {
            using (var stateFileStream = await _stateFile.OpenStreamForWriteAsync())
            {
                var state = _instaApi.GetStateDataAsStream();
                state.Seek(0, SeekOrigin.Begin);
                state.CopyTo(stateFileStream);
            }
        }

        public async Task<IResult<InstaLoginResult>> Login()
        {
            if (_instaApi == null)
                throw new NullReferenceException("Api has not been initialized");
            if (!_instaApi.IsUserAuthenticated)
            {
                var logInResult = await _instaApi.LoginAsync();

                if (!logInResult.Succeeded || logInResult.Value != InstaLoginResult.Success)
                {
                    return logInResult;
                }              
            }

            await WriteStateToStorage();

            return Result.Success(InstaLoginResult.Success);
        }

        public async Task<IResult<bool>> Logout()
        {
            var result = await _instaApi.LogoutAsync();
            if (result.Value)
                await WriteStateToStorage();
            return result;
        }

        public async Task<IResult<InstaDirectInboxContainer>> GetInboxAsync()
        {
            if (_instaApi == null)
                throw new NullReferenceException("Api has not been initialized");
            var result = await _instaApi.GetDirectInboxAsync();
            if (result.Succeeded)
                _inbox = result.Value.Inbox;

            foreach(var thread in _inbox.Threads)
            {
                InboxThreads.Add(new InstaDirectInboxThreadWrapper(thread));
            }

            return result;
        }

        public async Task<IResult<InstaDirectInboxThread>> GetInboxThread(InstaDirectInboxThreadWrapper thread)
        {
            if (thread == null)
                throw new ArgumentNullException(nameof(thread));
            var result = await _instaApi.GetDirectInboxThreadAsync(thread.ThreadId);
            if (result.Succeeded)
            {
                thread.UpdateItemList(result.Value.Items);
            }
            return result;
        }

        // Send message to the current selected recipient
        public async Task<IResult<InstaDirectInboxThreadList>> SendMessage(string content)
        {
            var result = await _instaApi.SendDirectMessage(SelectedThread.Users.FirstOrDefault()?.Pk.ToString(), SelectedThread.ThreadId, content);
            return result;
        }


        private const string DEFAULT_HOST = "mqtt-mini.facebook.com";
        private const int DEFAULT_PORT = 443;
        public async Task FbnsTest()
        {
            var bootstrap = new Bootstrap();
            bootstrap
                .Channel<TcpSocketChannel>()
                .Option(ChannelOption.TcpNodelay, true)
                .Option(ChannelOption.ConnectTimeout, TimeSpan.FromSeconds(5))
                .Handler(new ActionChannelInitializer<TcpSocketChannel>(channel =>
            {
                var pipeline = channel.Pipeline;
                pipeline.AddLast("decoder", new MqttDecoder(false, 10));
                pipeline.AddLast("encoder", new MqttEncoder());
                pipeline.AddLast("handler", new MqttHandler());
            }));

            var mqttChannel = await bootstrap.ConnectAsync(DEFAULT_HOST, DEFAULT_PORT);

            if (mqttChannel.Open)
            {
                var connectPacket = new ConnectPacket();
                connectPacket.ProtocolLevel = 3;
                connectPacket.ProtocolName = "MQTToT";
                connectPacket.KeepAliveInSeconds = 900;
            }

            await mqttChannel.CloseAsync();

        }

        class MqttHandler : SimpleChannelInboundHandler<Packet>
        {
            protected override void ChannelRead0(IChannelHandlerContext ctx, Packet msg)
            {
                PublishPacket ack = msg as PublishPacket;
                
                throw new NotImplementedException();
            }
        }
    }
}

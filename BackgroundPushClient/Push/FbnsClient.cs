using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Net.Security;
using System.Threading;
using System.Threading.Tasks;
using DotNetty.Handlers.Tls;
using DotNetty.Transport.Bootstrapping;
using DotNetty.Transport.Channels;
using DotNetty.Transport.Channels.Sockets;
using InstantMessaging.Notification.Push.PacketHelpers;
using InstaSharper.API.Push;
using InstaSharper.Classes;
using InstaSharper.Classes.DeviceInfo;
using InstaSharper.Helpers;

namespace BackgroundPushClient.Push
{
    public sealed class FbnsClient
    {
        private readonly UserSessionData _user;
        private readonly IHttpRequestProcessor _httpRequestProcessor;
        private readonly AndroidDevice _device;
        private SingleThreadEventLoop _loopGroup;
        private const string DEFAULT_HOST = "mqtt-mini.facebook.com";
        private int _secondsToNextRetry = 5;
        private CancellationTokenSource _connectRetryCancellationToken;

        internal bool IsShutdown => _loopGroup?.IsShutdown ?? false;

        internal FbnsConnectionData ConnectionData { get; }

        internal FbnsClient(AndroidDevice device, UserSessionData sessionData, IHttpRequestProcessor requestProcessor, FbnsConnectionData connectionData = null)
        {
            _user = sessionData;
            _httpRequestProcessor = requestProcessor;
            _device = device;

            ConnectionData = connectionData ?? new FbnsConnectionData();

            // If token is older than 24 hours then discard it
            if ((DateTime.Now - ConnectionData.FbnsTokenLastUpdated).TotalHours > 24) ConnectionData.FbnsToken = "";

            // Build user agent for first time setup
            if (string.IsNullOrEmpty(ConnectionData.UserAgent))
                ConnectionData.UserAgent = FbnsUserAgent.BuildFbUserAgent(device);
        }

        public async Task Start()
        {
            _connectRetryCancellationToken?.Cancel();
            _connectRetryCancellationToken = new CancellationTokenSource();
            if (_loopGroup != null) await _loopGroup.ShutdownGracefullyAsync();
            _loopGroup = new SingleThreadEventLoop();
            var cancellationToken = _connectRetryCancellationToken.Token;

            var connectPacket = new FbnsConnectPacket
            {
                Payload = await PayloadProcessor.BuildPayload(ConnectionData)
            };

            var bootstrap = new Bootstrap();
            bootstrap
                .Group(_loopGroup)
                .Channel<TcpSocketChannel>()
                .Option(ChannelOption.ConnectTimeout, TimeSpan.FromSeconds(5))
                .Option(ChannelOption.TcpNodelay, true)
                .Option(ChannelOption.SoKeepalive, true)
                .Handler(new ActionChannelInitializer<TcpSocketChannel>(channel =>
                {
                    var pipeline = channel.Pipeline;
                    pipeline.AddLast("tls",new TlsHandler(
                        stream => new SslStream(stream, true, (sender, certificate, chain, errors) => true),
                        new ClientTlsSettings(DEFAULT_HOST)));
                    pipeline.AddLast("encoder",new FbnsPacketEncoder());
                    pipeline.AddLast("decoder",new FbnsPacketDecoder());
                    pipeline.AddLast("handler",new PacketInboundHandler(this));
                }));

            try
            {
                if (cancellationToken.IsCancellationRequested) return;
                var fbnsChannel = await bootstrap.ConnectAsync(new DnsEndPoint(DEFAULT_HOST, 443));
                await fbnsChannel.WriteAndFlushAsync(connectPacket);
            }
            catch (Exception)
            {
                Debug.WriteLine($"Failed to connect to Push/MQTT server. No Internet connection? Retry in {_secondsToNextRetry} seconds.");
                await Task.Delay(TimeSpan.FromSeconds(_secondsToNextRetry), cancellationToken);
                if (cancellationToken.IsCancellationRequested) return;
                _secondsToNextRetry = _secondsToNextRetry < 300 ? _secondsToNextRetry * 2 : 300;    // Maximum wait time is 5 mins
                await Start();
            }
        }

        internal async Task RegisterClient(string token)
        {
            if (string.IsNullOrEmpty(token)) throw new ArgumentNullException(nameof(token));
            if (ConnectionData.FbnsToken == token)
            {
                ConnectionData.FbnsToken = token;
                return;
            }
            
            var uri = UriCreator.GetRegisterPushUri();
            var fields = new Dictionary<string, string>
            {
                {"device_type", "android_mqtt"},
                {"is_main_push_channel", "true"},
                {"phone_id", _device.PhoneId.ToString()},
                {"device_token", token},
                {"_csrftoken", _user.CsrfToken },
                {"guid", _device.Uuid.ToString() },
                {"_uuid", _device.Uuid.ToString() },
                {"users", _user.LoggedInUser.Pk.ToString() }
            };
            var request = HttpHelper.GetDefaultRequest(HttpMethod.Post, uri, _device);
            request.Content = new FormUrlEncodedContent(fields);
            await _httpRequestProcessor.SendAsync(request);

            ConnectionData.FbnsToken = token;
        }

        public async Task Shutdown()
        {
            _connectRetryCancellationToken?.Cancel();
            if (_loopGroup != null) await _loopGroup.ShutdownGracefullyAsync();
        }

        internal void OnMessageReceived(MessageReceivedEventArgs args)
        {
            MessageReceived?.Invoke(this, args);
        }
    }
}

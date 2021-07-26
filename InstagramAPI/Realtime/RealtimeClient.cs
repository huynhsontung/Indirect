using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Networking;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;
using InstagramAPI.Classes.Core;
using InstagramAPI.Classes.Mqtt.Packets;
using InstagramAPI.Fbns;
using InstagramAPI.Fbns.Packets;
using InstagramAPI.Push;
using InstagramAPI.Sync;
using InstagramAPI.Utils;

namespace InstagramAPI.Realtime
{
    public class RealtimeClient
    {
        public event EventHandler<List<MessageSyncEventArgs>> MessageReceived;
        public event EventHandler<PubsubEventArgs> ActivityIndicatorChanged;
        public event EventHandler<UserPresenceEventArgs> UserPresenceChanged;

        public bool Running => !(_runningTokenSource?.IsCancellationRequested ?? true);

        private const string HostName = "edge-mqtt.facebook.com";
        private const int KeepAlive = 60;   // seconds

        private readonly Instagram _instagram;
        private readonly RealtimeConnectionData _connectionData;
        private CancellationTokenSource _runningTokenSource;
        private DataWriter _outboundWriter;
        private DataReader _inboundReader;
        private StreamSocket _socket;

        public RealtimeClient(Instagram instagram)
        {
            _instagram = instagram;
            _connectionData = new RealtimeConnectionData(instagram.Device, ApiVersion.Current);
        }

        public async Task Start()
        {
            if (Running || !_instagram.IsUserAuthenticated)
            {
                return;
            }

            this.Log("Starting");

            _connectionData.SetCredential(_instagram.Session);
            var tokenSource = new CancellationTokenSource();
            var connectPacket = new FbnsConnectPacket
            {
                KeepAliveInSeconds = KeepAlive,
                Payload = await PayloadProcessor.BuildPayload(_connectionData, tokenSource.Token)
            };

            var socket = new StreamSocket();
            await socket.ConnectAsync(new HostName(HostName), "443", SocketProtectionLevel.Tls12);
            _inboundReader = new DataReader(socket.InputStream);
            _outboundWriter = new DataWriter(socket.OutputStream);
            _inboundReader.ByteOrder = ByteOrder.BigEndian;
            _inboundReader.InputStreamOptions = InputStreamOptions.Partial;
            _outboundWriter.ByteOrder = ByteOrder.BigEndian;
            _runningTokenSource = tokenSource;
            _socket = socket;
            await FbnsPacketEncoder.EncodePacket(connectPacket, _outboundWriter);
            StartPollingLoop();
        }

        private async void StartPollingLoop()
        {
            while (Running)
            {
                var reader = _inboundReader;
                Packet packet;
                try
                {
                    await reader.LoadAsync(FbnsPacketDecoder.PACKET_HEADER_LENGTH);
                }
                catch (Exception e)
                {
                    if (Running)
                    {
                        DebugLogger.LogException(e, false);
                    }

                    continue;
                }

                //try
                //{
                //    packet = await FbnsPacketDecoder.DecodePacket(reader);
                //    await OnPacketReceived(packet);
                //}
                //catch (Exception e)
                //{
                //    ExceptionsCaught?.Invoke(this, new UnhandledExceptionEventArgs(e, false));
                //}
            }
        }
    }
}

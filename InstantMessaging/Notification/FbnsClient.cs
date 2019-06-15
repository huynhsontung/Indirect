using System;
using System.Diagnostics;
using System.Threading.Tasks;
using DotNetty.Buffers;
using DotNetty.Codecs.Mqtt;
using DotNetty.Codecs.Mqtt.Packets;
using DotNetty.Transport.Bootstrapping;
using DotNetty.Transport.Channels;
using DotNetty.Transport.Channels.Embedded;
using DotNetty.Transport.Channels.Sockets;
using InstantMessaging.Notification.MqttHelpers;
using InstaSharper.Classes.Android.DeviceInfo;

namespace InstantMessaging.Notification
{
    class FbnsClient
    {
        private const string DEFAULT_HOST = "mqtt-mini.facebook.com";
        private const int DEFAULT_PORT = 443;
        private AndroidDevice _device;
        private FbnsConnectionData _connectionData;

        public FbnsClient(AndroidDevice device, FbnsConnectionData connectionData = null)
        {
            _device = device;
            _connectionData = connectionData ?? LoadConnectionData();
            if (string.IsNullOrEmpty(_connectionData.UserAgent))
                _connectionData.UserAgent = FbnsUserAgent.BuildFbUserAgent(device);
            LocalTest();
        }

        public async Task LocalTest()
        {
            var testChannel = new EmbeddedChannel(new MqttEncoder(), new CustomMqttEncoder());
            var connectPacket = new FbnsConnectPacket
            {
                Payload = await PayloadProcessor.BuildPayload(_connectionData)
            };
            testChannel.WriteOutbound(connectPacket);
            var body = testChannel.ReadOutbound<IByteBuffer>();
            var payload = testChannel.ReadOutbound<IByteBuffer>();
            body.Release();
            payload.Release();

            // this packet will not go through custom encoder. type check?
            var properConnectPacket = new ConnectPacket
            {
                ProtocolName = "MQTT",
                ClientId = "Tom",
                KeepAliveInSeconds = 900
            };
            testChannel.WriteOutbound(properConnectPacket);
            var connect = testChannel.ReadOutbound<IByteBuffer>();
            connect.Release();
        }

        public void SaveConnectionData()
        {
            // todo: implement save connection data to disk
        }

        public FbnsConnectionData LoadConnectionData()
        {
            // todo: implement load connection data from disk
            return new FbnsConnectionData();
        }

        public async Task FbnsTest()
        {
            var bootstrap = new Bootstrap();
            bootstrap
                .Channel<TcpSocketChannel>()
                .Option(ChannelOption.SoKeepalive, true)
                .Option(ChannelOption.TcpNodelay, true)
                .Option(ChannelOption.ConnectTimeout, TimeSpan.FromSeconds(5))
                .Handler(new ActionChannelInitializer<TcpSocketChannel>(channel =>
                {
                    var pipeline = channel.Pipeline;
                    pipeline.AddLast("decoder", new MqttDecoder(false, 10));
                    pipeline.AddLast("handler", new MqttHandler());
                    pipeline.AddLast("cus-encoder", new CustomMqttEncoder());
                    pipeline.AddLast("std-encoder", new MqttEncoder());
                }));

            var mqttChannel = await bootstrap.ConnectAsync(DEFAULT_HOST, DEFAULT_PORT);
            Debug.WriteLine($"TcpSocketChannel Open: {mqttChannel.Open}");
            Debug.WriteLine($"TcpSocketChannel Active: {mqttChannel.Active}");
            if (mqttChannel.Active)
            {
                var connectPacket = new FbnsConnectPacket
                {
                    Payload = await PayloadProcessor.BuildPayload(_connectionData)
                };
                await mqttChannel.WriteAndFlushAsync(connectPacket);
                connectPacket.Payload.Release();
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

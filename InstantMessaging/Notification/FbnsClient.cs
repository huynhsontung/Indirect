using System;
using System.Threading.Tasks;
using DotNetty.Codecs.Mqtt;
using DotNetty.Codecs.Mqtt.Packets;
using DotNetty.Transport.Bootstrapping;
using DotNetty.Transport.Channels;
using DotNetty.Transport.Channels.Sockets;
using InstantMessaging.Notification.MqttHelpers;

namespace InstantMessaging.Notification
{
    class FbnsClient
    {
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
                    pipeline.AddLast("std-decoder", new MqttDecoder(false, 10));
                    pipeline.AddLast("cus-encoder", new CustomMqttEncoder());
                    pipeline.AddLast("std-encoder", new MqttEncoder());
                    pipeline.AddLast("handler", new MqttHandler());
                }));

            var mqttChannel = await bootstrap.ConnectAsync(DEFAULT_HOST, DEFAULT_PORT);

            if (mqttChannel.Open)
            {
                var connectPacket = new FbnsConnectPacket();
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

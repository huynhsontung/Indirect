using System;
using DotNetty.Buffers;
using DotNetty.Codecs.Mqtt.Packets;

namespace InstantMessaging.Notification.MqttHelpers
{
    public class FbnsConnectPacket : Packet
    {
        public override PacketType PacketType { get; } = PacketType.CONNECT;

        public int Flags { get; } = 194;

        public string ProtocolName { get; } = "MQTToT";

        public int ProtocolLevel { get; } = 3;

        private int _keepAlive = 900;

        public int KeepAliveInSeconds
        {
            get => _keepAlive;
            set
            {
                if (value > 65535) throw new ArgumentOutOfRangeException();
                _keepAlive = value;
            }
        }

        public IByteBuffer Payload { get; set; } 
    }
}

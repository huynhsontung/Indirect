using Windows.Storage.Streams;
using InstagramAPI.Classes.Mqtt.Packets;

namespace InstagramAPI.Fbns.Packets
{
    public sealed class FbnsConnectPacket : Packet
    {
        public override PacketType PacketType => PacketType.CONNECT;

        public string ProtocolName => "MQTToT";

        public byte ProtocolLevel => 3;

        public ushort KeepAliveInSeconds { get; set; }

        public bool CleanSession => true;

        public bool HasWill { get; set; }

        public QualityOfService WillQualityOfService { get; set; }

        public bool WillRetain { get; set; }

        public bool HasPassword => true;

        public bool HasUsername => true;

        public IBuffer Payload { get; set; }
    }
}

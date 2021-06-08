using Windows.Storage.Streams;
using InstagramAPI.Classes.Mqtt.Packets;

namespace InstagramAPI.Push.Packets
{
    public sealed class FbnsConnectPacket : Packet
    {
        public override PacketType PacketType => PacketType.CONNECT;

        public string ProtocolName => "MQTToT";

        public byte ProtocolLevel => 3;

        public ushort KeepAliveInSeconds => 900;

        public bool CleanSession => true;

        public bool HasWill { get; set; }

        public IBuffer WillMessage { get; set; }

        public QualityOfService WillQualityOfService { get; set; }

        public bool WillRetain { get; set; }

        public bool HasPassword => true;

        public bool HasUsername => true;

        public string Username { get; set; }

        public string Password { get; set; }

        public string ClientId { get; set; }

        public string WillTopicName { get; set; }

        public IBuffer Payload { get; set; }
    }
}

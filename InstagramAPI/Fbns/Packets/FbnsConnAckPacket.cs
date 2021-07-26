using InstagramAPI.Classes.Mqtt.Packets;

namespace InstagramAPI.Fbns.Packets
{
    public sealed class FbnsConnAckPacket : Packet
    {
        public override PacketType PacketType { get; } = PacketType.CONNACK;

        public int ConnAckFlags { get; set; }

        public ConnectReturnCode ReturnCode { get; set; }

        public string Authentication { get; set; }
    }
}

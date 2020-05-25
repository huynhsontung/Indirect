using System;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage.Streams;
using InstagramAPI.Classes.Mqtt;
using InstagramAPI.Classes.Mqtt.Packets;
using InstagramAPI.Utils;

namespace InstagramAPI.Push.Packets
{
    public static class FbnsPacketEncoder
    {
        const uint PACKET_ID_LENGTH = 2;
        const uint STRING_SIZE_LENGTH = 2;
        // const uint MAX_VARIABLE_LENGTH = 4;

        public static async Task EncodePacket(Packet packet, DataWriter writer)
        {
            DebugLogger.Log(nameof(FbnsPacketEncoder), $"Encoding {packet.PacketType}");
            switch (packet.PacketType)
            {
                case PacketType.CONNECT:
                    if (packet is FbnsConnectPacket fbnsConnectPacket)
                    {
                        EncodeFbnsConnectPacket(fbnsConnectPacket, writer);
                    }
                    else
                    {
                        EncodeConnectMessage((ConnectPacket) packet, writer);
                    }
                    break;
                case PacketType.PUBLISH:
                    EncodePublishPacket((PublishPacket) packet, writer);
                    break;
                case PacketType.PUBACK:
                case PacketType.PUBREC:
                case PacketType.PUBREL:
                case PacketType.PUBCOMP:
                case PacketType.UNSUBACK:
                    EncodePacketWithIdOnly((PacketWithId)packet, writer);
                    break;
                case PacketType.PINGREQ:
                case PacketType.PINGRESP:
                case PacketType.DISCONNECT:
                    EncodePacketWithFixedHeaderOnly(packet, writer);
                    break;
                default:
                    throw new ArgumentException("Unsupported packet type: " + packet.PacketType, nameof(packet));
            }
            await writer.StoreAsync();
            await writer.FlushAsync();
        }

        static void EncodeConnectMessage(ConnectPacket packet, DataWriter writer)
        {
            uint payloadwriterferSize = 0;

            // Client id
            string clientId = packet.ClientId;
            if (string.IsNullOrEmpty(clientId)) throw new EncoderException("Client identifier is required.");
            byte[] clientIdBytes = EncodeStringInUtf8(clientId);
            payloadwriterferSize += STRING_SIZE_LENGTH + (uint) clientIdBytes.Length;

            byte[] willTopicBytes;
            IBuffer willMessage;
            if (packet.HasWill)
            {
                if (packet.WillMessage == null) throw new EncoderException("Packet has will but will message is null");
                if (string.IsNullOrEmpty(packet.WillTopicName)) throw new EncoderException("Packet has will but will topic is null or empty");
                // Will topic and message
                string willTopic = packet.WillTopicName;
                willTopicBytes = EncodeStringInUtf8(willTopic);
                willMessage = packet.WillMessage;
                payloadwriterferSize += STRING_SIZE_LENGTH + (uint) willTopicBytes.Length;
                payloadwriterferSize += 2 + willMessage.Length;
            }
            else
            {
                willTopicBytes = null;
                willMessage = null;
            }

            string userName = packet.Username;
            byte[] userNameBytes;
            if (packet.HasUsername)
            {
                userNameBytes = EncodeStringInUtf8(userName);
                payloadwriterferSize += STRING_SIZE_LENGTH + (uint) userNameBytes.Length;
            }
            else
            {
                userNameBytes = null;
            }

            byte[] passwordBytes;
            if (packet.HasPassword)
            {
                string password = packet.Password;
                passwordBytes = EncodeStringInUtf8(password);
                payloadwriterferSize += STRING_SIZE_LENGTH + (uint) passwordBytes.Length;
            }
            else
            {
                passwordBytes = null;
            }

            // Fixed header
            byte[] protocolNameBytes = EncodeStringInUtf8(packet.ProtocolName);
            uint variableHeaderwriterferSize = STRING_SIZE_LENGTH + (uint) protocolNameBytes.Length + 4;
            uint variablePartSize = variableHeaderwriterferSize + payloadwriterferSize;

            writer.WriteByte(CalculateFirstByteOfFixedHeader(packet));
            WriteVariableLengthInt(writer, variablePartSize);

            writer.WriteUInt16((ushort) protocolNameBytes.Length);
            writer.WriteBytes(protocolNameBytes);

            writer.WriteByte(packet.ProtocolLevel);
            writer.WriteByte(CalculateConnectFlagsByte(packet));
            writer.WriteUInt16(packet.KeepAliveInSeconds);

            // Payload
            writer.WriteUInt16((ushort) clientIdBytes.Length);
            writer.WriteBytes(clientIdBytes);
            if (packet.HasWill)
            {
                writer.WriteUInt16((ushort) willTopicBytes.Length);
                writer.WriteBytes(willTopicBytes);
                writer.WriteUInt16((ushort) willMessage.Length);
                writer.WriteBuffer(willMessage);
            }
            if (packet.HasUsername)
            {
                writer.WriteUInt16((ushort) userNameBytes.Length);
                writer.WriteBytes(userNameBytes);

                if (packet.HasPassword)
                {
                    writer.WriteUInt16((ushort) passwordBytes.Length);
                    writer.WriteBytes(passwordBytes);
                }
            }
        }

        private static void EncodeFbnsConnectPacket(FbnsConnectPacket packet, DataWriter writer)
        {
            var payload = packet.Payload;
            uint payloadSize = payload?.Length ?? 0;
            byte[] protocolNameBytes = EncodeStringInUtf8(packet.ProtocolName);
            // variableHeaderwriterferSize = 2 bytes length + ProtocolName bytes + 4 bytes
            // 4 bytes are reserved for: 1 byte ProtocolLevel, 1 byte ConnectFlags, 2 byte KeepAlive
            uint variableHeaderwriterferSize = (uint) (STRING_SIZE_LENGTH + protocolNameBytes.Length + 4);
            uint variablePartSize = variableHeaderwriterferSize + payloadSize;

            // MQTT message format from: http://public.dhe.ibm.com/software/dw/webservices/ws-mqtt/MQTT_V3.1_Protocol_Specific.pdf
            writer.WriteByte((byte) ((int) packet.PacketType << 4)); // Write packet type
            WriteVariableLengthInt(writer, variablePartSize); // Write remaining length

            // Variable part
            writer.WriteUInt16((ushort) protocolNameBytes.Length);
            writer.WriteBytes(protocolNameBytes);
            writer.WriteByte(packet.ProtocolLevel);
            writer.WriteByte(packet.ConnectFlags);
            writer.WriteUInt16(packet.KeepAliveInSeconds);

            if (payload != null)
            {
                writer.WriteBuffer(payload);
            }
        }

        private static void EncodePublishPacket(PublishPacket packet, DataWriter writer)
        {
            var payload = packet.Payload;

            string topicName = packet.TopicName;
            byte[] topicNameBytes = EncodeStringInUtf8(topicName);

            uint variableHeaderBufferSize = (uint)(STRING_SIZE_LENGTH + topicNameBytes.Length +
                                           (packet.QualityOfService > QualityOfService.AtMostOnce ? PACKET_ID_LENGTH : 0));
            uint payloadBufferSize = payload?.Length ?? 0;
            uint variablePartSize = variableHeaderBufferSize + payloadBufferSize;

            writer.WriteByte(CalculateFirstByteOfFixedHeader(packet));
            WriteVariableLengthInt(writer, variablePartSize);
            writer.WriteUInt16((ushort) topicNameBytes.Length);
            writer.WriteBytes(topicNameBytes);
            if (packet.QualityOfService > QualityOfService.AtMostOnce)
            {
                writer.WriteUInt16(packet.PacketId);
            }

            if (payload != null)
            {
                writer.WriteBuffer(payload);
            }
        }

        static void EncodePacketWithIdOnly(PacketWithId packet, DataWriter writer)
        {
            var msgId = packet.PacketId;

            const uint VariableHeaderBufferSize = PACKET_ID_LENGTH; // variable part only has a packet id

            writer.WriteByte(CalculateFirstByteOfFixedHeader(packet));
            WriteVariableLengthInt(writer, VariableHeaderBufferSize);
            writer.WriteUInt16(msgId);
        }

        static void EncodePacketWithFixedHeaderOnly(Packet packet, DataWriter writer)
        {
            writer.WriteByte(CalculateFirstByteOfFixedHeader(packet));
            writer.WriteByte(0);
        }

        static byte CalculateFirstByteOfFixedHeader(Packet packet)
        {
            int ret = 0;
            ret |= (int)packet.PacketType << 4;
            if (packet.Duplicate)
            {
                ret |= 0x08;
            }
            ret |= (int)packet.QualityOfService << 1;
            if (packet.RetainRequested)
            {
                ret |= 0x01;
            }
            return (byte) ret;
        }

        static void WriteVariableLengthInt(DataWriter writer, uint value)
        {
            do
            {
                var digit = value % 128;
                value /= 128;
                if (value > 0)
                {
                    digit |= 0x80;
                }
                writer.WriteByte((byte) digit);
            }
            while (value > 0);
        }

        static byte[] EncodeStringInUtf8(string s)
        {
            return Encoding.UTF8.GetBytes(s);
        }

        static byte CalculateConnectFlagsByte(ConnectPacket packet)
        {
            int flagByte = 0;
            if (packet.HasUsername)
            {
                flagByte |= 0x80;
            }
            if (packet.HasPassword)
            {
                flagByte |= 0x40;
            }
            if (packet.HasWill)
            {
                flagByte |= 0x04;
                flagByte |= ((int)packet.WillQualityOfService & 0x03) << 3;
                if (packet.WillRetain)
                {
                    flagByte |= 0x20;
                }
            }
            if (packet.CleanSession)
            {
                flagByte |= 0x02;
            }
            return (byte) flagByte;
        }
    }
}
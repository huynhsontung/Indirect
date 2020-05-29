using System;
using System.Collections.Generic;
using System.Text;
using Windows.Storage.Streams;
using InstagramAPI.Classes.Mqtt.Packets;
using ByteOrder = Windows.Storage.Streams.ByteOrder;

namespace InstagramAPI.Sync
{
    class StandalonePacketEncoder
    {
        private const int PACKET_ID_LENGTH = 2;
        private const int STRING_SIZE_LENGTH = 2;
        private const int MAX_VARIABLE_LENGTH = 4;

        public static IBuffer EncodePacket(Packet packet)
        {
            var writer = new DataWriter();
            writer.ByteOrder = ByteOrder.BigEndian;
            switch (packet.PacketType)
            {
                case PacketType.CONNECT:
                    EncodeConnectMessage(writer, (ConnectPacket) packet);
                    break;
                case PacketType.CONNACK:
                    EncodeConnAckMessage(writer, (ConnAckPacket) packet);
                    break;
                case PacketType.PUBLISH:
                    EncodePublishMessage(writer, (PublishPacket) packet);
                    break;
                case PacketType.PUBACK:
                case PacketType.PUBREC:
                case PacketType.PUBREL:
                case PacketType.PUBCOMP:
                case PacketType.UNSUBACK:
                    EncodePacketWithIdOnly(writer, (PacketWithId) packet);
                    break;
                case PacketType.SUBSCRIBE:
                    EncodeSubscribeMessage(writer, (SubscribePacket) packet);
                    break;
                case PacketType.SUBACK:
                    EncodeSubAckMessage(writer, (SubAckPacket) packet);
                    break;
                case PacketType.UNSUBSCRIBE:
                    EncodeUnsubscribeMessage(writer, (UnsubscribePacket) packet);
                    break;
                case PacketType.PINGREQ:
                case PacketType.PINGRESP:
                case PacketType.DISCONNECT:
                    EncodePacketWithFixedHeaderOnly(writer, packet);
                    break;
                default:
                    throw new ArgumentException("Unknown packet type: " + packet.PacketType, nameof(packet));
            }

            return writer.DetachBuffer();
        }

        static void EncodeConnectMessage(DataWriter writer, ConnectPacket packet)
        {
            int payloadBufferSize = 0;

            // Client id
            string clientId = packet.ClientId;
            if (string.IsNullOrEmpty(clientId)) throw new Exception("Client identifier is required.");
            byte[] clientIdBytes = EncodeStringInUtf8(clientId);
            payloadBufferSize += STRING_SIZE_LENGTH + clientIdBytes.Length;

            byte[] willTopicBytes;
            IBuffer willMessage;
            if (packet.HasWill)
            {
                // Will topic and message
                string willTopic = packet.WillTopicName;
                willTopicBytes = EncodeStringInUtf8(willTopic);
                willMessage = packet.WillMessage;
                payloadBufferSize += STRING_SIZE_LENGTH + willTopicBytes.Length;
                payloadBufferSize += 2 + (int) willMessage.Length;
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
                payloadBufferSize += STRING_SIZE_LENGTH + userNameBytes.Length;
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
                payloadBufferSize += STRING_SIZE_LENGTH + passwordBytes.Length;
            }
            else
            {
                passwordBytes = null;
            }

            // Fixed header
            byte[] protocolNameBytes = EncodeStringInUtf8(packet.ProtocolName);
            int variableHeaderBufferSize = STRING_SIZE_LENGTH + protocolNameBytes.Length + 4;
            int variablePartSize = variableHeaderBufferSize + payloadBufferSize;
            writer.WriteByte(CalculateFirstByteOfFixedHeader(packet));
            WriteVariableLengthInt(writer, variablePartSize);

            writer.WriteInt16((short) protocolNameBytes.Length);
            writer.WriteBytes(protocolNameBytes);

            writer.WriteByte((byte) packet.ProtocolLevel);
            writer.WriteByte(CalculateConnectFlagsByte(packet));
            writer.WriteInt16((short) packet.KeepAliveInSeconds);

            // Payload
            writer.WriteInt16((short) clientIdBytes.Length);
            writer.WriteBytes(clientIdBytes);
            if (packet.HasWill)
            {
                writer.WriteInt16((short) willTopicBytes.Length);
                writer.WriteBytes(willTopicBytes);
                writer.WriteInt16((short) willMessage.Length);
                writer.WriteBuffer(willMessage);
            }
            if (packet.HasUsername)
            {
                writer.WriteInt16((short) userNameBytes.Length);
                writer.WriteBytes(userNameBytes);

                if (packet.HasPassword)
                {
                    writer.WriteInt16((short) passwordBytes.Length);
                    writer.WriteBytes(passwordBytes);
                }
            }
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
                flagByte |= ((byte)packet.WillQualityOfService & 0x03) << 3;
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

        static void EncodeConnAckMessage(DataWriter writer, ConnAckPacket message)
        {
            writer.WriteByte(CalculateFirstByteOfFixedHeader(message));
            writer.WriteByte(2); // remaining length
            if (message.SessionPresent)
            {
                writer.WriteByte(1); // 7 reserved 0-bits and SP = 1
            }
            else
            {
                writer.WriteByte(0); // 7 reserved 0-bits and SP = 0
            }
            writer.WriteByte((byte)message.ReturnCode);
        }

        static void EncodePublishMessage(DataWriter writer, PublishPacket packet)
        {
            IBuffer payload = packet.Payload;

            string topicName = packet.TopicName;
            // Util.ValidateTopicName(topicName);
            byte[] topicNameBytes = EncodeStringInUtf8(topicName);

            int variableHeaderBufferSize = STRING_SIZE_LENGTH + topicNameBytes.Length +
                (packet.QualityOfService > QualityOfService.AtMostOnce ? PACKET_ID_LENGTH : 0);
            int payloadBufferSize = (int) payload.Length;
            int variablePartSize = variableHeaderBufferSize + payloadBufferSize;

            writer.WriteByte(CalculateFirstByteOfFixedHeader(packet));
            WriteVariableLengthInt(writer, variablePartSize);
            writer.WriteInt16((short) topicNameBytes.Length);
            writer.WriteBytes(topicNameBytes);
            if (packet.QualityOfService > QualityOfService.AtMostOnce)
            {
                writer.WriteInt16((short) packet.PacketId);
            }

            writer.WriteBuffer(payload);
        }

        static void EncodePacketWithIdOnly(DataWriter writer, PacketWithId packet)
        {
            int msgId = packet.PacketId;

            const int VariableHeaderBufferSize = PACKET_ID_LENGTH; // variable part only has a packet id

            writer.WriteByte(CalculateFirstByteOfFixedHeader(packet));
            WriteVariableLengthInt(writer, VariableHeaderBufferSize);
            writer.WriteInt16((short) msgId);
        }

        static void EncodeSubscribeMessage(DataWriter writer, SubscribePacket packet)
        {
            const int VariableHeaderSize = PACKET_ID_LENGTH;
            int payloadBufferSize = 0;

            var encodedTopicFilters = new List<byte[]>();

            foreach (var topic in packet.Requests)
            {
                byte[] topicFilterBytes = EncodeStringInUtf8(topic.TopicFilter);
                payloadBufferSize += STRING_SIZE_LENGTH + topicFilterBytes.Length + 1; // length, value, QoS
                encodedTopicFilters.Add(topicFilterBytes);
            }

            int variablePartSize = VariableHeaderSize + payloadBufferSize;

            writer.WriteByte(CalculateFirstByteOfFixedHeader(packet));
            WriteVariableLengthInt(writer, variablePartSize);

            // Variable Header
            writer.WriteInt16((short) packet.PacketId); // todo: review: validate?

            // Payload
            for (int i = 0; i < encodedTopicFilters.Count; i++)
            {
                var topicFilterBytes = encodedTopicFilters[i];
                writer.WriteInt16((short) topicFilterBytes.Length);
                writer.WriteBytes(topicFilterBytes);
                writer.WriteByte((byte)packet.Requests[i].QualityOfService);
            }
        }

        static void EncodeSubAckMessage(DataWriter writer, SubAckPacket message)
        {
            int payloadBufferSize = message.ReturnCodes.Count;
            int variablePartSize = PACKET_ID_LENGTH + payloadBufferSize;

            writer.WriteByte(CalculateFirstByteOfFixedHeader(message));
            WriteVariableLengthInt(writer, variablePartSize);
            writer.WriteInt16((short) message.PacketId);
            foreach (QualityOfService qos in message.ReturnCodes)
            {
                writer.WriteByte((byte)qos);
            }
        }

        static void EncodeUnsubscribeMessage(DataWriter writer, UnsubscribePacket packet)
        {
            const int VariableHeaderSize = 2;
            int payloadBufferSize = 0;

            var encodedTopicFilters = new List<byte[]>();

            foreach (string topic in packet.TopicFilters)
            {
                byte[] topicFilterBytes = EncodeStringInUtf8(topic);
                payloadBufferSize += STRING_SIZE_LENGTH + topicFilterBytes.Length; // length, value
                encodedTopicFilters.Add(topicFilterBytes);
            }

            int variablePartSize = VariableHeaderSize + payloadBufferSize;

            writer.WriteByte(CalculateFirstByteOfFixedHeader(packet));
            WriteVariableLengthInt(writer, variablePartSize);

            // Variable Header
            writer.WriteInt16((short) packet.PacketId); // todo: review: validate?

            // Payload
            foreach (var topic in encodedTopicFilters)
            {
                writer.WriteInt16((short) topic.Length);
                writer.WriteBytes(topic);
            }
        }

        static void EncodePacketWithFixedHeaderOnly(DataWriter writer, Packet packet)
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

        static void WriteVariableLengthInt(DataWriter writer, int value)
        {
            do
            {
                int digit = value % 128;
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
            // todo: validate against extra limitations per MQTT's UTF-8 string definition
            return Encoding.UTF8.GetBytes(s);
        }
    }
}

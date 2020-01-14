using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage.Streams;
using DotNetty.Buffers;
using DotNetty.Codecs.Mqtt.Packets;
using DotNetty.Common;
using DotNetty.Common.Utilities;
using Buffer = Windows.Storage.Streams.Buffer;

namespace Indirect.Notification
{
    class StandalonePacketEncoder
    {
        const int PacketIdLength = 2;
        const int StringSizeLength = 2;
        const int MaxVariableLength = 4;

        public static async Task<IBuffer> EncodePacket(Packet packet)
        {
            var writer = new DataWriter();
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

            await writer.StoreAsync();
            return writer.DetachBuffer();
        }

        static void EncodeConnectMessage(DataWriter writer, ConnectPacket packet)
        {
            int payloadBufferSize = 0;

            // Client id
            string clientId = packet.ClientId;
            if (string.IsNullOrEmpty(clientId)) throw new Exception("Client identifier is required.");
            byte[] clientIdBytes = EncodeStringInUtf8(clientId);
            payloadBufferSize += StringSizeLength + clientIdBytes.Length;

            byte[] willTopicBytes;
            IByteBuffer willMessage;
            if (packet.HasWill)
            {
                // Will topic and message
                string willTopic = packet.WillTopicName;
                willTopicBytes = EncodeStringInUtf8(willTopic);
                willMessage = packet.WillMessage;
                payloadBufferSize += StringSizeLength + willTopicBytes.Length;
                payloadBufferSize += 2 + willMessage.ReadableBytes;
            }
            else
            {
                willTopicBytes = null;
                willMessage = null;
            }

            // Fixed header
            var protocolNameByteSize = (int) writer.MeasureString(packet.ProtocolName);
            int variableHeaderBufferSize = StringSizeLength + protocolNameByteSize + 4;
            int variablePartSize = variableHeaderBufferSize + payloadBufferSize;
            int fixedHeaderBufferSize = 1 + MaxVariableLength;
            try
            {
                writer.WriteByte(CalculateFirstByteOfFixedHeader(packet));
                WriteVariableLengthInt(writer, variablePartSize);

                writer.WriteInt16((short) writer.MeasureString(packet.ProtocolName));
                writer.WriteString(packet.ProtocolName);

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
                    writer.WriteInt16((short) willMessage.ReadableBytes);
                    if (willMessage.IsReadable())
                    {
                        if (willMessage.ReadableBytes == willMessage.Array.Length)
                            writer.WriteBytes(willMessage.Array);
                        else
                        {
                            var willMessageBytes = new byte[willMessage.ReadableBytes];
                            willMessage.ReadBytes(willMessageBytes);
                            writer.WriteBytes(willMessageBytes);
                        }
                    }
                    willMessage.Release();
                }
                if (packet.HasUsername)
                {
                    writer.WriteInt16((short) writer.MeasureString(packet.Username));
                    writer.WriteString(packet.Username);

                    if (packet.HasPassword)
                    {
                        writer.WriteInt16((short)writer.MeasureString(packet.Password));
                        writer.WriteString(packet.Password);
                    }
                }
            }
            finally
            {
                willMessage?.SafeRelease();
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
            IByteBuffer payload = packet.Payload ?? Unpooled.Empty;

            string topicName = packet.TopicName;
            // Util.ValidateTopicName(topicName);
            byte[] topicNameBytes = EncodeStringInUtf8(topicName);

            int variableHeaderBufferSize = StringSizeLength + topicNameBytes.Length +
                (packet.QualityOfService > QualityOfService.AtMostOnce ? PacketIdLength : 0);
            int payloadBufferSize = payload.ReadableBytes;
            int variablePartSize = variableHeaderBufferSize + payloadBufferSize;
            int fixedHeaderBufferSize = 1 + MaxVariableLength;

            writer.WriteByte(CalculateFirstByteOfFixedHeader(packet));
            WriteVariableLengthInt(writer, variablePartSize);
            writer.WriteInt16((short) topicNameBytes.Length);
            writer.WriteBytes(topicNameBytes);
            if (packet.QualityOfService > QualityOfService.AtMostOnce)
            {
                writer.WriteInt16((short) packet.PacketId);
            }

            if (payload.IsReadable())
            {
                if (payload.ReadableBytes == payload.Array.Length)
                    writer.WriteBytes(payload.Array);
                else
                {
                    var payloadBytes = new byte[payload.ReadableBytes];
                    payload.ReadBytes(payloadBytes);
                    writer.WriteBytes(payloadBytes);
                }
            }
        }

        static void EncodePacketWithIdOnly(DataWriter writer, PacketWithId packet)
        {
            int msgId = packet.PacketId;

            const int VariableHeaderBufferSize = PacketIdLength; // variable part only has a packet id
            int fixedHeaderBufferSize = 1 + MaxVariableLength;
            
            writer.WriteByte(CalculateFirstByteOfFixedHeader(packet));
            WriteVariableLengthInt(writer, VariableHeaderBufferSize);
            writer.WriteInt16((short) msgId);
        }

        static void EncodeSubscribeMessage(DataWriter writer, SubscribePacket packet)
        {
            const int VariableHeaderSize = PacketIdLength;
            int payloadBufferSize = 0;

            int variablePartSize = VariableHeaderSize + payloadBufferSize;
            int fixedHeaderBufferSize = 1 + MaxVariableLength;

            
            writer.WriteByte(CalculateFirstByteOfFixedHeader(packet));
            WriteVariableLengthInt(writer, variablePartSize);

            // Variable Header
            writer.WriteInt16((short) packet.PacketId); // todo: review: validate?

            // Payload
            foreach (var subscriptionRequest in packet.Requests)
            {
                writer.WriteInt16((short)writer.MeasureString(subscriptionRequest.TopicFilter));
                writer.WriteString(subscriptionRequest.TopicFilter);
                writer.WriteByte((byte) subscriptionRequest.QualityOfService);
            }
        }

        static void EncodeSubAckMessage(DataWriter writer, SubAckPacket message)
        {
            int payloadBufferSize = message.ReturnCodes.Count;
            int variablePartSize = PacketIdLength + payloadBufferSize;
            int fixedHeaderBufferSize = 1 + MaxVariableLength;
            
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


            // foreach (string topic in packet.TopicFilters)
            // {
            //     byte[] topicFilterBytes = EncodeStringInUtf8(topic);
            //     payloadBufferSize += StringSizeLength + topicFilterBytes.Length; // length, value
            //     encodedTopicFilters.Add(topicFilterBytes);
            // }

            int variablePartSize = VariableHeaderSize + payloadBufferSize;
            int fixedHeaderBufferSize = 1 + MaxVariableLength;

            writer.WriteByte(CalculateFirstByteOfFixedHeader(packet));
            WriteVariableLengthInt(writer, variablePartSize);

            // Variable Header
            writer.WriteInt16((short) packet.PacketId); // todo: review: validate?

            // Payload
            foreach (var topic in packet.TopicFilters)
            {
                writer.WriteInt16((short) writer.MeasureString(topic));
                writer.WriteString(topic);
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

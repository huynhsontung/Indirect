using System;
using System.Collections.Generic;
using System.Text;
using DotNetty.Buffers;
using DotNetty.Codecs;
using DotNetty.Codecs.Mqtt.Packets;
using DotNetty.Common.Utilities;
using DotNetty.Transport.Channels;

namespace InstagramAPI.Push.Packets
{
    public sealed class FbnsPacketEncoder : MessageToMessageEncoder<Packet>
    {
        const int PACKET_ID_LENGTH = 2;
        const int STRING_SIZE_LENGTH = 2;
        const int MAX_VARIABLE_LENGTH = 4;

        public override bool IsSharable => true;

        protected override void Encode(IChannelHandlerContext context, Packet packet, List<object> output)
        {
            var bufferAllocator = context.Allocator;
            switch (packet.PacketType)
            {
                case PacketType.CONNECT:
                    if (packet is FbnsConnectPacket fbnsConnectPacket)
                    {
                        EncodeFbnsConnectPacket(bufferAllocator, fbnsConnectPacket, output);
                    }
                    else
                    {
                        EncodeConnectMessage(bufferAllocator, (ConnectPacket) packet, output);
                    }
                    break;
                case PacketType.PUBLISH:
                    EncodePublishPacket(bufferAllocator, (PublishPacket) packet, output);
                    break;
                case PacketType.PUBACK:
                case PacketType.PUBREC:
                case PacketType.PUBREL:
                case PacketType.PUBCOMP:
                case PacketType.UNSUBACK:
                    EncodePacketWithIdOnly(bufferAllocator, (PacketWithId)packet, output);
                    break;
                case PacketType.PINGREQ:
                case PacketType.PINGRESP:
                case PacketType.DISCONNECT:
                    EncodePacketWithFixedHeaderOnly(bufferAllocator, packet, output);
                    break;
                default:
                    throw new ArgumentException("Unsupported packet type: " + packet.PacketType, nameof(packet));
            }
        }

        static void EncodeConnectMessage(IByteBufferAllocator bufferAllocator, ConnectPacket packet, List<object> output)
        {
            int payloadBufferSize = 0;

            // Client id
            string clientId = packet.ClientId;
            if (string.IsNullOrEmpty(clientId)) throw new DecoderException("Client identifier is required.");
            byte[] clientIdBytes = EncodeStringInUtf8(clientId);
            payloadBufferSize += STRING_SIZE_LENGTH + clientIdBytes.Length;

            byte[] willTopicBytes;
            IByteBuffer willMessage;
            if (packet.HasWill)
            {
                // Will topic and message
                string willTopic = packet.WillTopicName;
                willTopicBytes = EncodeStringInUtf8(willTopic);
                willMessage = packet.WillMessage;
                payloadBufferSize += STRING_SIZE_LENGTH + willTopicBytes.Length;
                payloadBufferSize += 2 + willMessage.ReadableBytes;
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
            int fixedHeaderBufferSize = 1 + STRING_SIZE_LENGTH;
            IByteBuffer buf = null;
            try
            {
                buf = bufferAllocator.Buffer(fixedHeaderBufferSize + variablePartSize);
                buf.WriteByte(CalculateFirstByteOfFixedHeader(packet));
                WriteVariableLengthInt(buf, variablePartSize);

                buf.WriteShort(protocolNameBytes.Length);
                buf.WriteBytes(protocolNameBytes);

                buf.WriteByte(packet.ProtocolLevel);
                buf.WriteByte(CalculateConnectFlagsByte(packet));
                buf.WriteShort(packet.KeepAliveInSeconds);

                // Payload
                buf.WriteShort(clientIdBytes.Length);
                buf.WriteBytes(clientIdBytes, 0, clientIdBytes.Length);
                if (packet.HasWill)
                {
                    buf.WriteShort(willTopicBytes.Length);
                    buf.WriteBytes(willTopicBytes, 0, willTopicBytes.Length);
                    buf.WriteShort(willMessage.ReadableBytes);
                    if (willMessage.IsReadable())
                    {
                        buf.WriteBytes(willMessage);
                    }
                    willMessage.Release();
                    willMessage = null;
                }
                if (packet.HasUsername)
                {
                    buf.WriteShort(userNameBytes.Length);
                    buf.WriteBytes(userNameBytes, 0, userNameBytes.Length);

                    if (packet.HasPassword)
                    {
                        buf.WriteShort(passwordBytes.Length);
                        buf.WriteBytes(passwordBytes, 0, passwordBytes.Length);
                    }
                }

                output.Add(buf);
                buf = null;
            }
            finally
            {
                buf?.SafeRelease();
                willMessage?.SafeRelease();
            }
        }

        private static void EncodeFbnsConnectPacket(IByteBufferAllocator bufferAllocator, FbnsConnectPacket packet, List<object> output)
        {
            var payload = packet.Payload;
            int payloadSize = payload?.ReadableBytes ?? 0;
            byte[] protocolNameBytes = EncodeStringInUtf8(packet.ProtocolName);
            // variableHeaderBufferSize = 2 bytes length + ProtocolName bytes + 4 bytes
            // 4 bytes are reserved for: 1 byte ProtocolLevel, 1 byte ConnectFlags, 2 byte KeepAlive
            int variableHeaderBufferSize = STRING_SIZE_LENGTH + protocolNameBytes.Length + 4;
            int variablePartSize = variableHeaderBufferSize + payloadSize;
            int fixedHeaderBufferSize = 1 + MAX_VARIABLE_LENGTH;
            IByteBuffer buf = null;
            try
            {
                // MQTT message format from: http://public.dhe.ibm.com/software/dw/webservices/ws-mqtt/MQTT_V3.1_Protocol_Specific.pdf
                buf = bufferAllocator.Buffer(fixedHeaderBufferSize + variableHeaderBufferSize);
                buf.WriteByte((int)packet.PacketType << 4); // Write packet type
                WriteVariableLengthInt(buf, variablePartSize); // Write remaining length

                // Variable part
                buf.WriteShort(protocolNameBytes.Length);
                buf.WriteBytes(protocolNameBytes);
                buf.WriteByte(packet.ProtocolLevel);
                buf.WriteByte(packet.ConnectFlags);
                buf.WriteShort(packet.KeepAliveInSeconds);

                output.Add(buf);
                buf = null;
            }
            finally
            {
                buf?.SafeRelease();
            }

            if (payload?.IsReadable() ?? false)
            {
                output.Add(payload.Retain());
            }
        }

        private static void EncodePublishPacket(IByteBufferAllocator bufferAllocator, PublishPacket packet, List<object> output)
        {
            IByteBuffer payload = packet.Payload ?? Unpooled.Empty;

            string topicName = packet.TopicName;
            byte[] topicNameBytes = EncodeStringInUtf8(topicName);

            int variableHeaderBufferSize = STRING_SIZE_LENGTH + topicNameBytes.Length +
                                           (packet.QualityOfService > QualityOfService.AtMostOnce ? PACKET_ID_LENGTH : 0);
            int payloadBufferSize = payload.ReadableBytes;
            int variablePartSize = variableHeaderBufferSize + payloadBufferSize;
            int fixedHeaderBufferSize = 1 + MAX_VARIABLE_LENGTH;

            IByteBuffer buf = null;
            try
            {
                buf = bufferAllocator.Buffer(fixedHeaderBufferSize + variablePartSize);
                buf.WriteByte(CalculateFirstByteOfFixedHeader(packet));
                WriteVariableLengthInt(buf, variablePartSize);
                buf.WriteShort(topicNameBytes.Length);
                buf.WriteBytes(topicNameBytes);
                if (packet.QualityOfService > QualityOfService.AtMostOnce)
                {
                    buf.WriteShort(packet.PacketId);
                }

                output.Add(buf);
                buf = null;
            }
            finally
            {
                buf?.SafeRelease();
            }

            if (payload.IsReadable())
            {
                output.Add(payload.Retain());
            }
        }

        static void EncodePacketWithIdOnly(IByteBufferAllocator bufferAllocator, PacketWithId packet, List<object> output)
        {
            int msgId = packet.PacketId;

            const int VariableHeaderBufferSize = PACKET_ID_LENGTH; // variable part only has a packet id
            int fixedHeaderBufferSize = 1 + MAX_VARIABLE_LENGTH;
            IByteBuffer buffer = null;
            try
            {
                buffer = bufferAllocator.Buffer(fixedHeaderBufferSize + VariableHeaderBufferSize);
                buffer.WriteByte(CalculateFirstByteOfFixedHeader(packet));
                WriteVariableLengthInt(buffer, VariableHeaderBufferSize);
                buffer.WriteShort(msgId);

                output.Add(buffer);
                buffer = null;
            }
            finally
            {
                buffer?.SafeRelease();
            }
        }

        static void EncodePacketWithFixedHeaderOnly(IByteBufferAllocator bufferAllocator, Packet packet, List<object> output)
        {
            IByteBuffer buffer = null;
            try
            {
                buffer = bufferAllocator.Buffer(2);
                buffer.WriteByte(CalculateFirstByteOfFixedHeader(packet));
                buffer.WriteByte(0);

                output.Add(buffer);
                buffer = null;
            }
            finally
            {
                buffer?.SafeRelease();
            }
        }

        static int CalculateFirstByteOfFixedHeader(Packet packet)
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
            return ret;
        }

        static void WriteVariableLengthInt(IByteBuffer buffer, int value)
        {
            do
            {
                int digit = value % 128;
                value /= 128;
                if (value > 0)
                {
                    digit |= 0x80;
                }
                buffer.WriteByte(digit);
            }
            while (value > 0);
        }

        static byte[] EncodeStringInUtf8(string s)
        {
            return Encoding.UTF8.GetBytes(s);
        }

        static int CalculateConnectFlagsByte(ConnectPacket packet)
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
            return flagByte;
        }
    }
}
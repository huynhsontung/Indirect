using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage.Streams;
using InstagramAPI.Classes.Mqtt.Packets;
using InstagramAPI.Fbns.Packets;
using InstagramAPI.Utils;
using Ionic.Zlib;

namespace InstagramAPI.Fbns
{
    public static class FbnsPacketEncoder
    {
        const uint PacketIdLength = 2;
        const uint StringSizeLength = 2;
        // const uint MAX_VARIABLE_LENGTH = 4;

        public static async Task EncodePacket(Packet packet, DataWriter writer)
        {
            DebugLogger.Log(nameof(FbnsPacketEncoder), $"Encoding {packet.PacketType}");
            switch (packet.PacketType)
            {
                case PacketType.CONNECT:
                    EncodeFbnsConnectPacket((FbnsConnectPacket) packet, writer);
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
                case PacketType.SUBSCRIBE:
                    EncodeSubscribeMessage(writer, (SubscribePacket)packet);
                    break;
                case PacketType.UNSUBSCRIBE:
                    EncodeUnsubscribeMessage(writer, (UnsubscribePacket)packet);
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

        private static void EncodeFbnsConnectPacket(FbnsConnectPacket packet, DataWriter writer)
        {
            var payload = CompressPayload(packet.Payload);
            uint payloadSize = payload?.Length ?? 0;
            byte[] protocolNameBytes = EncodeStringInUtf8(packet.ProtocolName);
            // variableHeaderwriterferSize = 2 bytes length + ProtocolName bytes + 4 bytes
            // 4 bytes are reserved for: 1 byte ProtocolLevel, 1 byte ConnectFlags, 2 byte KeepAlive
            uint variableHeaderwriterferSize = (uint) (StringSizeLength + protocolNameBytes.Length + 4);
            uint variablePartSize = variableHeaderwriterferSize + payloadSize;

            // MQTT message format from: http://public.dhe.ibm.com/software/dw/webservices/ws-mqtt/MQTT_V3.1_Protocol_Specific.pdf
            writer.WriteByte((byte) ((int) packet.PacketType << 4)); // Write packet type
            WriteVariableLengthInt(writer, variablePartSize); // Write remaining length

            // Variable part
            writer.WriteUInt16((ushort) protocolNameBytes.Length);
            writer.WriteBytes(protocolNameBytes);
            writer.WriteByte(packet.ProtocolLevel);
            writer.WriteByte(CalculateConnectFlagsByte(packet));
            writer.WriteUInt16(packet.KeepAliveInSeconds);

            if (payload != null)
            {
                writer.WriteBuffer(payload);
            }
        }

        private static void EncodePublishPacket(PublishPacket packet, DataWriter writer)
        {
            var payload = CompressPayload(packet.Payload);

            string topicName = packet.TopicName;
            byte[] topicNameBytes = EncodeStringInUtf8(topicName);

            uint variableHeaderBufferSize = (uint)(StringSizeLength + topicNameBytes.Length +
                                           (packet.QualityOfService > QualityOfService.AtMostOnce ? PacketIdLength : 0));
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

            const uint variableHeaderBufferSize = PacketIdLength; // variable part only has a packet id

            writer.WriteByte(CalculateFirstByteOfFixedHeader(packet));
            WriteVariableLengthInt(writer, variableHeaderBufferSize);
            writer.WriteUInt16(msgId);
        }

        static void EncodePacketWithFixedHeaderOnly(Packet packet, DataWriter writer)
        {
            writer.WriteByte(CalculateFirstByteOfFixedHeader(packet));
            writer.WriteByte(0);
        }

        static void EncodeSubscribeMessage(DataWriter writer, SubscribePacket packet)
        {
            uint payloadBufferSize = 0;

            var encodedTopicFilters = new List<byte[]>();

            foreach (var topic in packet.Requests)
            {
                byte[] topicFilterBytes = EncodeStringInUtf8(topic.TopicFilter);
                payloadBufferSize += StringSizeLength + (uint) topicFilterBytes.Length + 1; // length, value, QoS
                encodedTopicFilters.Add(topicFilterBytes);
            }

            uint variablePartSize = PacketIdLength + payloadBufferSize;

            writer.WriteByte(CalculateFirstByteOfFixedHeader(packet));
            WriteVariableLengthInt(writer, variablePartSize);

            // Variable Header
            writer.WriteInt16((short)packet.PacketId); // todo: review: validate?

            // Payload
            for (int i = 0; i < encodedTopicFilters.Count; i++)
            {
                var topicFilterBytes = encodedTopicFilters[i];
                writer.WriteInt16((short)topicFilterBytes.Length);
                writer.WriteBytes(topicFilterBytes);
                writer.WriteByte((byte)packet.Requests[i].QualityOfService);
            }
        }

        static void EncodeUnsubscribeMessage(DataWriter writer, UnsubscribePacket packet)
        {
            uint payloadBufferSize = 0;

            var encodedTopicFilters = new List<byte[]>();

            foreach (string topic in packet.TopicFilters)
            {
                byte[] topicFilterBytes = EncodeStringInUtf8(topic);
                payloadBufferSize += StringSizeLength + (uint) topicFilterBytes.Length; // length, value
                encodedTopicFilters.Add(topicFilterBytes);
            }

            uint variablePartSize = PacketIdLength + payloadBufferSize;

            writer.WriteByte(CalculateFirstByteOfFixedHeader(packet));
            WriteVariableLengthInt(writer, variablePartSize);

            // Variable Header
            writer.WriteInt16((short)packet.PacketId); // todo: review: validate?

            // Payload
            foreach (var topic in encodedTopicFilters)
            {
                writer.WriteInt16((short)topic.Length);
                writer.WriteBytes(topic);
            }
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

        static byte CalculateConnectFlagsByte(FbnsConnectPacket packet)
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

        private static IBuffer CompressPayload(IBuffer payload)
        {
            var buffer = payload.ToArray();

            // zlib deflate
            var dataStream = new MemoryStream(buffer.Length);
            using (var zlibStream = new ZlibStream(dataStream, CompressionMode.Compress, CompressionLevel.Level9, true))
            {
                zlibStream.Write(buffer, 0, buffer.Length);
            }

            var compressed = dataStream.GetWindowsRuntimeBuffer(0, (int)dataStream.Length);
            return compressed;
        }
    }
}
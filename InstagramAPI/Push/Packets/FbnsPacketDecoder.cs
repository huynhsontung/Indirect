using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.Storage.Streams;
using InstagramAPI.Classes.Mqtt;
using InstagramAPI.Classes.Mqtt.Packets;
using InstagramAPI.Utils;

namespace InstagramAPI.Push.Packets
{
    /// <summary>
    ///     Customized MqttDecoder for Fbns that only handles Publish, PubAck, and ConnAck
    /// </summary>
    /// Reference: https://github.com/Azure/DotNetty/blob/dev/src/DotNetty.Codecs.Mqtt/MqttDecoder.cs
    public static class FbnsPacketDecoder
    {
        public const uint PACKET_HEADER_LENGTH = 2;

        private static class Signatures
        {
            public const byte PubAck = 64;
            public const byte ConnAck = 32;
//            public const byte PubRec = 80;
//            public const byte PubRel = 98;
//            public const byte PubComp = 112;
            public const byte Connect = 16;
            public const byte Subscribe = 130;
            public const byte SubAck = 144;
//            public const byte PingReq = 192;
            public const byte PingResp = 208;
//            public const byte Disconnect = 224;
            public const byte Unsubscribe = 162;
//            public const byte UnsubAck = 176;

            public static bool IsPublish(int signature)
            {
                return (signature & 240) == 48;
            }
        }

        public static async Task<Packet> DecodePacket(DataReader reader)
        {
            int signature = reader.ReadByte();

            var remainingLength = await DecodeRemainingLength(reader);
            
            // Load remaining length into buffer
            if (remainingLength > 0) await reader.LoadAsync(remainingLength);

            var packet = DecodePacketInternal(reader, signature, ref remainingLength);

            if (remainingLength > 0)
            {
                throw new DecoderException($"Declared remaining length is bigger than packet data size by {remainingLength}.");
            }

            return packet;
        }

        private static Packet DecodePacketInternal(DataReader reader, int packetSignature, ref uint remainingLength)
        {
            if (Signatures.IsPublish(packetSignature))
            {
                var qualityOfService =
                    (QualityOfService) ((packetSignature >> 1) &
                                        0x3); // take bits #1 and #2 ONLY and convert them into QoS value
                if (qualityOfService == QualityOfService.Reserved)
                {
                    throw new DecoderException(
                        $"Unexpected QoS value of {(int) qualityOfService} for {PacketType.PUBLISH} packet.");
                }

                bool duplicate = (packetSignature & 0x8) == 0x8; // test bit#3
                bool retain = (packetSignature & 0x1) != 0; // test bit#0
                var packet = new PublishPacket(qualityOfService, duplicate, retain);
                DecodePublishPacket(reader, packet, ref remainingLength);
                return packet;
            }

            switch (packetSignature & 240)  // We don't care about flags for these packets
            {
                case Signatures.Subscribe & 240:
                    var subscribePacket = new SubscribePacket();
                    DecodePacketIdVariableHeader(reader, subscribePacket, ref remainingLength);
                    DecodeSubscribePayload(reader, subscribePacket, ref remainingLength);
                    return subscribePacket;
                case Signatures.Connect:
                    throw new DecoderException("Fbns Connect packet is not expected");
                case Signatures.PubAck:
                    var pubAckPacket = new PubAckPacket();
                    DecodePacketIdVariableHeader(reader, pubAckPacket, ref remainingLength);
                    return pubAckPacket;
                case Signatures.ConnAck:
                    var connAckPacket = new FbnsConnAckPacket();
                    DecodeConnAckPacket(reader, connAckPacket, ref remainingLength);
                    return connAckPacket;
                case Signatures.SubAck:
                    var subAckPacket = new SubAckPacket();
                    DecodePacketIdVariableHeader(reader, subAckPacket, ref remainingLength);
                    DecodeSubAckPayload(reader, subAckPacket, ref remainingLength);
                    return subAckPacket;
                case Signatures.Unsubscribe & 240:
                    var unsubscribePacket = new UnsubscribePacket();
                    DecodePacketIdVariableHeader(reader, unsubscribePacket, ref remainingLength);
                    DecodeUnsubscribePayload(reader, unsubscribePacket, ref remainingLength);
                    return unsubscribePacket;
                case Signatures.PingResp:
                    return PingRespPacket.Instance;
                default:
                    throw new DecoderException($"Packet type {packetSignature} not supported");
            }
        }

        static void DecodeSubscribePayload(DataReader reader, SubscribePacket packet, ref uint remainingLength)
        {
            var subscribeTopics = new List<SubscriptionRequest>();
            while (remainingLength > 0)
            {
                string topicFilter = DecodeString(reader, ref remainingLength);
                ValidateTopicFilter(topicFilter);

                DecreaseRemainingLength(ref remainingLength, 1);
                var qos = reader.ReadByte();
                if (qos >= (int) QualityOfService.Reserved)
                {
                    throw new DecoderException($"[MQTT-3.8.3-4]. Invalid QoS value: {qos}.");
                }

                subscribeTopics.Add(new SubscriptionRequest(topicFilter, (QualityOfService)qos));
            }

            if (subscribeTopics.Count == 0)
            {
                throw new DecoderException("[MQTT-3.8.3-3]");
            }

            packet.Requests = subscribeTopics;
        }

        static void DecodeSubAckPayload(DataReader reader, SubAckPacket packet, ref uint remainingLength)
        {
            var returnCodes = new QualityOfService[remainingLength];
            for (int i = 0; i < remainingLength; i++)
            {
                var returnCode = (QualityOfService)reader.ReadByte();
                if (returnCode > QualityOfService.ExactlyOnce && returnCode != QualityOfService.Failure)
                {
                    throw new DecoderException($"[MQTT-3.9.3-2]. Invalid return code: {returnCode}");
                }
                returnCodes[i] = returnCode;
            }
            packet.ReturnCodes = returnCodes;

            remainingLength = 0;
        }

        static void DecodeUnsubscribePayload(DataReader reader, UnsubscribePacket packet, ref uint remainingLength)
        {
            var unsubscribeTopics = new List<string>();
            while (remainingLength > 0)
            {
                string topicFilter = DecodeString(reader, ref remainingLength);
                ValidateTopicFilter(topicFilter);
                unsubscribeTopics.Add(topicFilter);
            }

            if (unsubscribeTopics.Count == 0)
            {
                throw new DecoderException("[MQTT-3.10.3-2]");
            }

            packet.TopicFilters = unsubscribeTopics;

            remainingLength = 0;
        }

        static void DecodeConnAckPacket(DataReader reader, FbnsConnAckPacket packet, ref uint remainingLength)
        {
            packet.ConnAckFlags = reader.ReadByte();
            packet.ReturnCode = (ConnectReturnCode) reader.ReadByte();
            remainingLength -= 2;
            if (remainingLength > 0)
            {
                var authSize = reader.ReadUInt16();
                packet.Authentication = reader.ReadString(authSize);
                remainingLength -= authSize + 2u;
                if (remainingLength > 0)
                    DebugLogger.Log(nameof(FbnsPacketDecoder),
                        $"Unhandled data in the buffer. Length of remaining data = {remainingLength}");
            }
        }

        static void DecodePublishPacket(DataReader reader, PublishPacket packet, ref uint remainingLength)
        {
            string topicName = DecodeString(reader, ref remainingLength);

            packet.TopicName = topicName;
            if (packet.QualityOfService > QualityOfService.AtMostOnce)
            {
                DecodePacketIdVariableHeader(reader, packet, ref remainingLength);
            }

            IBuffer payload;
            if (remainingLength > 0)
            {
                payload = reader.ReadBuffer(remainingLength);
                remainingLength = 0;
            }
            else
            {
                payload = null;
            }
            packet.Payload = payload;
        }

        private static async Task<uint> DecodeRemainingLength(DataReader reader)
        {
            uint multiplier = 1 << 7;
            byte digit = reader.ReadByte();
            uint result = (uint)(digit & 0x7f);
            uint read = 1;
            while ((digit & 0x80) != 0 && read < 4)
            {
                await reader.LoadAsync(1);
                digit = reader.ReadByte();
                result += (uint) (digit & 0x7f) * multiplier;
                multiplier <<= 7;
                read++;
            }

            if (read == 4 && (digit & 0x80) != 0)
            {
                throw new DecoderException("Remaining length exceeds 4 bytes in length");
            }

            return result;
        }

        static void DecodePacketIdVariableHeader(DataReader reader, PacketWithId packet, ref uint remainingLength)
        {
            int packetId = packet.PacketId = DecodeUnsignedShort(reader, ref remainingLength);
            if (packetId == 0)
            {
                throw new DecoderException("[MQTT-2.3.1-1]");
            }
        }

        static ushort DecodeUnsignedShort(DataReader reader, ref uint remainingLength)
        {
            DecreaseRemainingLength(ref remainingLength, 2);
            return reader.ReadUInt16();
        }

        static string DecodeString(DataReader reader, ref uint remainingLength) => DecodeString(reader, ref remainingLength, 0, uint.MaxValue);

        static string DecodeString(DataReader reader, ref uint remainingLength, uint minBytes) => DecodeString(reader, ref remainingLength, minBytes, uint.MaxValue);

        static string DecodeString(DataReader reader, ref uint remainingLength, uint minBytes, uint maxBytes)
        {
            ushort size = DecodeUnsignedShort(reader, ref remainingLength);

            if (size < minBytes)
            {
                throw new DecoderException($"String value is shorter than minimum allowed {minBytes}. Advertised length: {size}");
            }
            if (size > maxBytes)
            {
                throw new DecoderException($"String value is longer than maximum allowed {maxBytes}. Advertised length: {size}");
            }

            if (size == 0)
            {
                return string.Empty;
            }

            DecreaseRemainingLength(ref remainingLength, size);
            string value = reader.ReadString(size);
            // todo: enforce string definition by MQTT spec
            return value;
        }

        static void DecreaseRemainingLength(ref uint remainingLength, uint minExpectedLength)
        {
            if (remainingLength < minExpectedLength)
            {
                throw new DecoderException($"Current Remaining Length of {remainingLength} is smaller than expected {minExpectedLength}.");
            }
            remainingLength -= minExpectedLength;
        }

        static void ValidateTopicFilter(string topicFilter)
        {
            int length = topicFilter.Length;
            if (length == 0)
            {
                throw new DecoderException("[MQTT-4.7.3-1]");
            }

            for (int i = 0; i < length; i++)
            {
                char c = topicFilter[i];
                switch (c)
                {
                    case '+':
                        if ((i > 0 && topicFilter[i - 1] != '/') || (i < length - 1 && topicFilter[i + 1] != '/'))
                        {
                            throw new DecoderException($"[MQTT-4.7.1-3]. Invalid topic filter: {topicFilter}");
                        }
                        break;
                    case '#':
                        if (i < length - 1 || (i > 0 && topicFilter[i - 1] != '/'))
                        {
                            throw new DecoderException($"[MQTT-4.7.1-2]. Invalid topic filter: {topicFilter}");
                        }
                        break;
                }
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Windows.Storage.Streams;
using DotNetty.Buffers;
using DotNetty.Codecs.Mqtt.Packets;
using InstagramAPI.Push.Packets;
using ByteOrder = Windows.Storage.Streams.ByteOrder;

namespace InstagramAPI.Push
{
    class StandalonePacketDecoder
    {
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
            //            public const byte Unsubscribe = 162;
            public const byte UnsubAck = 176;

            public static bool IsPublish(int signature)
            {
                return (signature & 240) == 48;
            }
        }

        public static Packet DecodePacket(DataReader reader)
        {
            reader.ByteOrder = ByteOrder.BigEndian;
            int signature = reader.ReadByte();
            int remainingLength = DecodeRemainingLength(reader);

            if (Signatures.IsPublish(signature))
            {
                var qualityOfService =
                    (QualityOfService)((signature >> 1) &
                                        0x3); // take bits #1 and #2 ONLY and convert them into QoS value
                if (qualityOfService == QualityOfService.Reserved)
                {
                    throw new Exception(
                        $"Unexpected QoS value of {(int)qualityOfService} for {PacketType.PUBLISH} packet.");
                }

                bool duplicate = (signature & 0x8) == 0x8; // test bit#3
                bool retain = (signature & 0x1) != 0; // test bit#0
                var packet = new PublishPacket(qualityOfService, duplicate, retain);
                DecodePublishPacket(reader, packet, ref remainingLength);
                return packet;
            }

            switch (signature & 240)  // We don't care about flags for these packets
            {
                // case Signatures.Subscribe & 240:
                //     var subscribePacket = new SubscribePacket();
                //     DecodePacketIdVariableHeader(reader, subscribePacket, ref remainingLength);
                //     DecodeSubscribePayload(buffer, subscribePacket, ref remainingLength);
                //     return subscribePacket;
                case Signatures.Connect:
                    var connectPacket = new ConnectPacket();
                    DecodeConnectPacket(reader, connectPacket, ref remainingLength);
                    return connectPacket;
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
                case Signatures.UnsubAck:
                    var unsubAckPacket = new UnsubAckPacket();
                    DecodePacketIdVariableHeader(reader, unsubAckPacket, ref remainingLength);
                    return unsubAckPacket;
                case Signatures.PingResp:
                    return PingRespPacket.Instance;
                default:
                    throw new Exception($"Packet type {signature} not supported");
            }
        }

        static void DecodeSubscribePayload(DataReader buffer, SubscribePacket packet, ref int remainingLength)
        {
            var subscribeTopics = new List<SubscriptionRequest>();
            while (remainingLength > 0)
            {
                string topicFilter = DecodeString(buffer, ref remainingLength);
                // ValidateTopicFilter(topicFilter);

                DecreaseRemainingLength(ref remainingLength, 1);
                int qos = buffer.ReadByte();
                if (qos >= (int)QualityOfService.Reserved)
                {
                    throw new Exception($"[MQTT-3.8.3-4]. Invalid QoS value: {qos}.");
                }

                subscribeTopics.Add(new SubscriptionRequest(topicFilter, (QualityOfService)qos));
            }

            if (subscribeTopics.Count == 0)
            {
                throw new Exception("[MQTT-3.8.3-3]");
            }

            packet.Requests = subscribeTopics;
        }

        static void DecodeSubAckPayload(DataReader buffer, SubAckPacket packet, ref int remainingLength)
        {
            var returnCodes = new QualityOfService[remainingLength];
            for (int i = 0; i < remainingLength; i++)
            {
                var returnCode = (QualityOfService)buffer.ReadByte();
                if (returnCode > QualityOfService.ExactlyOnce && returnCode != QualityOfService.Failure)
                {
                    throw new Exception($"[MQTT-3.9.3-2]. Invalid return code: {returnCode}");
                }
                returnCodes[i] = returnCode;
            }
            packet.ReturnCodes = returnCodes;

            remainingLength = 0;
        }

        static void DecodeConnectPacket(DataReader buffer, ConnectPacket packet, ref int remainingLength)
        {
            string protocolName = DecodeString(buffer, ref remainingLength);
            // if (!PROTOCOL_NAME.Equals(protocolName, StringComparison.Ordinal))
            // {
            //     throw new Exception($"Unexpected protocol name. Expected: {PROTOCOL_NAME}. Actual: {protocolName}");
            // }
            packet.ProtocolName = protocolName;

            DecreaseRemainingLength(ref remainingLength, 1);
            packet.ProtocolLevel = buffer.ReadByte();

            // if (packet.ProtocolLevel != Util.ProtocolLevel)
            // {
            //     var connAckPacket = new ConnAckPacket();
            //     connAckPacket.ReturnCode = ConnectReturnCode.RefusedUnacceptableProtocolVersion;
            //     context.WriteAndFlushAsync(connAckPacket);
            //     throw new Exception($"Unexpected protocol level. Expected: {Util.ProtocolLevel}. Actual: {packet.ProtocolLevel}");
            // }

            DecreaseRemainingLength(ref remainingLength, 1);
            int connectFlags = buffer.ReadByte();

            packet.CleanSession = (connectFlags & 0x02) == 0x02;

            bool hasWill = (connectFlags & 0x04) == 0x04;
            if (hasWill)
            {
                packet.HasWill = true;
                packet.WillRetain = (connectFlags & 0x20) == 0x20;
                packet.WillQualityOfService = (QualityOfService)((connectFlags & 0x18) >> 3);
                if (packet.WillQualityOfService == QualityOfService.Reserved)
                {
                    throw new Exception($"[MQTT-3.1.2-14] Unexpected Will QoS value of {(int)packet.WillQualityOfService}.");
                }
                packet.WillTopicName = string.Empty;
            }
            else if ((connectFlags & 0x38) != 0) // bits 3,4,5 [MQTT-3.1.2-11]
            {
                throw new Exception("[MQTT-3.1.2-11]");
            }

            packet.HasUsername = (connectFlags & 0x80) == 0x80;
            packet.HasPassword = (connectFlags & 0x40) == 0x40;
            if (packet.HasPassword && !packet.HasUsername)
            {
                throw new Exception("[MQTT-3.1.2-22]");
            }
            if ((connectFlags & 0x1) != 0) // [MQTT-3.1.2-3]
            {
                throw new Exception("[MQTT-3.1.2-3]");
            }

            packet.KeepAliveInSeconds = DecodeUnsignedShort(buffer, ref remainingLength);

            string clientId = DecodeString(buffer, ref remainingLength);
            if (string.IsNullOrEmpty(clientId)) throw new Exception("Client identifier is required.");
            packet.ClientId = clientId;

            if (hasWill)
            {
                packet.WillTopicName = DecodeString(buffer, ref remainingLength);
                int willMessageLength = DecodeUnsignedShort(buffer, ref remainingLength);
                DecreaseRemainingLength(ref remainingLength, willMessageLength);
                var payload = new byte[willMessageLength];
                buffer.ReadBytes(payload);
                packet.WillMessage = Unpooled.CopiedBuffer(payload);
            }

            if (packet.HasUsername)
            {
                packet.Username = DecodeString(buffer, ref remainingLength);
            }

            if (packet.HasPassword)
            {
                packet.Password = DecodeString(buffer, ref remainingLength);
            }
        }

        static void DecodeConnAckPacket(DataReader buffer, FbnsConnAckPacket packet, ref int remainingLength)
        {
            packet.ConnAckFlags = buffer.ReadByte();
            packet.ReturnCode = (ConnectReturnCode)buffer.ReadByte();
            remainingLength -= 2;
            if (remainingLength > 0)
            {
                var authSize = buffer.ReadUInt16();
                packet.Authentication = buffer.ReadString(authSize);
                remainingLength -= authSize + 2;
                if (remainingLength > 0)
                    Debug.WriteLine(
                        $"FbnsPacketDecoder: Unhandled data in the buffer. Length of remaining data = {remainingLength}",
                        "Warning");
            }
        }

        static void DecodePublishPacket(DataReader buffer, PublishPacket packet, ref int remainingLength)
        {
            string topicName = DecodeString(buffer, ref remainingLength);

            packet.TopicName = topicName;
            if (packet.QualityOfService > QualityOfService.AtMostOnce)
            {
                DecodePacketIdVariableHeader(buffer, packet, ref remainingLength);
            }

            if (remainingLength > 0)
            {
                var payload = new byte[remainingLength];
                buffer.ReadBytes(payload);
                remainingLength = 0;
                packet.Payload = Unpooled.CopiedBuffer(payload);
            }
            else
            {
                packet.Payload = Unpooled.Empty;
            }
        }

        private static int DecodeRemainingLength(DataReader buffer)
        {
            int result = 0;
            int multiplier = 1;
            byte digit;
            int read = 0;
            do
            {
                digit = buffer.ReadByte();
                result += (digit & 0x7f) * multiplier;
                multiplier <<= 7;
                read++;
            }
            while ((digit & 0x80) != 0 && read < 4);

            if (read == 4 && (digit & 0x80) != 0)
            {
                throw new Exception("Remaining length exceeds 4 bytes in length");
            }

            return result;
        }

        static void DecodePacketIdVariableHeader(DataReader buffer, PacketWithId packet, ref int remainingLength)
        {
            int packetId = packet.PacketId = DecodeUnsignedShort(buffer, ref remainingLength);
            if (packetId == 0)
            {
                throw new Exception("[MQTT-2.3.1-1]");
            }
        }

        static int DecodeUnsignedShort(DataReader buffer, ref int remainingLength)
        {
            DecreaseRemainingLength(ref remainingLength, 2);
            return buffer.ReadUInt16();
        }

        static string DecodeString(DataReader buffer, ref int remainingLength) => DecodeString(buffer, ref remainingLength, 0, int.MaxValue);

        static string DecodeString(DataReader buffer, ref int remainingLength, int minBytes) => DecodeString(buffer, ref remainingLength, minBytes, int.MaxValue);

        static string DecodeString(DataReader buffer, ref int remainingLength, int minBytes, int maxBytes)
        {
            int size = DecodeUnsignedShort(buffer, ref remainingLength);

            if (size < minBytes)
            {
                throw new Exception($"String value is shorter than minimum allowed {minBytes}. Advertised length: {size}");
            }
            if (size > maxBytes)
            {
                throw new Exception($"String value is longer than maximum allowed {maxBytes}. Advertised length: {size}");
            }

            if (size == 0)
            {
                return string.Empty;
            }

            DecreaseRemainingLength(ref remainingLength, size);

            string value = buffer.ReadString((uint) size);
            return value;
        }

        static void DecreaseRemainingLength(ref int remainingLength, int minExpectedLength)
        {
            if (remainingLength < minExpectedLength)
            {
                throw new Exception($"Current Remaining Length of {remainingLength} is smaller than expected {minExpectedLength}.");
            }
            remainingLength -= minExpectedLength;
        }

    }
}

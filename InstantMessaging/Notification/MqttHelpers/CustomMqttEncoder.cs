using System.Collections.Generic;
using System.Text;
using DotNetty.Buffers;
using DotNetty.Codecs;
using DotNetty.Common.Utilities;
using DotNetty.Transport.Channels;

namespace InstantMessaging.Notification.MqttHelpers
{
    public sealed class CustomMqttEncoder : MessageToMessageEncoder<FbnsConnectPacket>
    {
        public static readonly CustomMqttEncoder Instance = new CustomMqttEncoder();

        const int PacketIdLength = 2;
        const int StringSizeLength = 2;
        const int MaxVariableLength = 4;

        public override bool IsSharable => true;

        protected override void Encode(IChannelHandlerContext context, FbnsConnectPacket packet, List<object> output)
        {
            var bufferAllocator = context.Allocator;
            
            if (packet.Payload == null) throw new EncoderException("Payload required");
            // todo: payload is level 9 deflated thrift CompactProtocol
            int payloadSize = packet.Payload.ReadableBytes;
            byte[] protocolNameBytes = EncodeStringInUtf8(packet.ProtocolName);
            // variableHeaderBufferSize = 2 bytes length + ProtocolName bytes + 4 bytes
            // 4 bytes are reserved for: 1 byte ProtocolLevel, 1 byte Flags, 2 byte KeepAlive
            int variableHeaderBufferSize = StringSizeLength + protocolNameBytes.Length + 4; 
            int variablePartSize = variableHeaderBufferSize + payloadSize;
            int fixedHeaderBufferSize = 1 + MaxVariableLength;
            IByteBuffer buf = null;
            try
            {
                // MQTT message format from: http://public.dhe.ibm.com/software/dw/webservices/ws-mqtt/MQTT_V3.1_Protocol_Specific.pdf
                buf = bufferAllocator.Buffer(fixedHeaderBufferSize + variablePartSize);
                buf.WriteByte((int) packet.PacketType << 4); // Write packet type
                WriteVariableLengthInt(buf, variablePartSize); // Write remaining length
                buf.WriteByte(protocolNameBytes.Length);
                buf.WriteBytes(protocolNameBytes);

                buf.WriteByte(packet.ProtocolLevel);
                buf.WriteByte(packet.Flags);
                buf.WriteShort(packet.KeepAliveInSeconds);

//                buf.WriteBytes(payloadBytes);

                output.Add(buf);
                buf = null;
            }
            finally
            {
                buf?.SafeRelease();
            }


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
    }
}
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using DotNetty.Buffers;
using DotNetty.Codecs.Compression;
using DotNetty.Transport.Channels.Embedded;
using Thrift.Protocol;
using Thrift.Protocol.Entities;
using Thrift.Transport.Client;
using CompressionLevel = System.IO.Compression.CompressionLevel;
using DeflateStream = System.IO.Compression.DeflateStream;

namespace InstantMessaging.Notification
{
    /*
     * Reference from Valga/Fbns-react
     * https://github.com/valga/fbns-react
     */
    static class PayloadProcessor
    {
        private const short CLIENT_ID = 1;
        private const short CLIENT_INFO = 4;
        private const short PASSWORD = 5;

        private const short USER_ID = 1;
        private const short USER_AGENT = 2;
        private const short CLIENT_CAPABILITIES = 3;
        private const short ENDPOINT_CAPABILITIES = 4;
        private const short PUBLISH_FORMAT = 5;
        private const short NO_AUTOMATIC_FOREGROUND = 6;
        private const short MAKE_USER_AVAILABLE_IN_FOREGROUND = 7;
        private const short DEVICE_ID = 8;
        private const short IS_INITIALLY_FOREGROUND = 9;
        private const short NETWORK_TYPE = 10;
        private const short NETWORK_SUBTYPE = 11;
        private const short CLIENT_MQTT_SESSION_ID = 12;
        private const short SUBSCRIBE_TOPICS = 14;
        private const short CLIENT_TYPE = 15;
        private const short APP_ID = 16;
        private const short DEVICE_SECRET = 20;
        private const short CLIENT_STACK = 21;

        private static TMemoryBufferTransport _memoryBufferTransport; // doesn't need manual disposal
        private static TCompactProtocol _thrift;
        private static FbnsConnectionData _payloadData;

        public static async Task<IByteBuffer> BuildPayload(FbnsConnectionData data)
        {
            _memoryBufferTransport = new TMemoryBufferTransport();
            _thrift = new TCompactProtocol(_memoryBufferTransport);
            _payloadData = data;

            var rawPayload = await ToThrift();
            
            // zlib deflate using DotNetty Zlib codec
            var embeddedChannel = new EmbeddedChannel(ZlibCodecFactory.NewZlibEncoder(9));
            embeddedChannel.WriteOutbound(Unpooled.CopiedBuffer(rawPayload));
            var compressed = embeddedChannel.ReadOutbound<IByteBuffer>();
            return compressed;
        }

        /// <summary>
        /// Make a complete payload from <see cref="_payloadData"/> using Thrift.
        /// </summary>
        /// <returns>Payload</returns>
        private static async Task<byte[]> ToThrift()
        {
            await WriteString(CLIENT_ID, _payloadData.ClientId);

            #region Write struct ClientInfo
            await WriteStructBegin(CLIENT_INFO);
            await WriteInt64(USER_ID, _payloadData.UserId);
            await WriteString(USER_AGENT, _payloadData.UserAgent);
            await WriteInt64(CLIENT_CAPABILITIES, _payloadData.ClientCapabilities);
            await WriteInt64(ENDPOINT_CAPABILITIES, _payloadData.EndpointCapabilities);
            await WriteInt32(PUBLISH_FORMAT, _payloadData.PublishFormat);
            await WriteBool(NO_AUTOMATIC_FOREGROUND, _payloadData.NoAutomaticForeground);
            await WriteBool(MAKE_USER_AVAILABLE_IN_FOREGROUND, _payloadData.MakeUserAvailableInForeground);
            await WriteString(DEVICE_ID, _payloadData.DeviceId);
            await WriteBool(IS_INITIALLY_FOREGROUND, _payloadData.IsInitiallyForeground);
            await WriteInt32(NETWORK_TYPE, _payloadData.NetworkType);
            await WriteInt32(NETWORK_SUBTYPE, _payloadData.NetworkSubtype);
            if (_payloadData.ClientMqttSessionId == 0)
            {
                var weekAgo = new DateTimeOffset(DateTime.Today.Subtract(TimeSpan.FromDays(7)));
                _payloadData.ClientMqttSessionId = DateTimeOffset.Now.ToUnixTimeMilliseconds() - weekAgo.ToUnixTimeMilliseconds();
            }

            await WriteInt64(CLIENT_MQTT_SESSION_ID, _payloadData.ClientMqttSessionId);
            await WriteListInt32(SUBSCRIBE_TOPICS, _payloadData.SubscribeTopics);
            await WriteString(CLIENT_TYPE, _payloadData.ClientType);
            await WriteInt64(APP_ID, _payloadData.AppId);
            await WriteString(DEVICE_SECRET, _payloadData.DeviceSecret);
            await WriteByte(CLIENT_STACK, _payloadData.ClientStack);
            await WriteStructEnd();
            #endregion

            await WriteString(PASSWORD, _payloadData.Password);
            await WriteFieldStop();
            return _memoryBufferTransport.GetBuffer();
        }

        private static async Task WriteString(short id, string str)
        {
            if (str == null) str = "";
            await _thrift.WriteFieldBeginAsync(new TField(null, TType.String, id));
            await _thrift.WriteStringAsync(str);
        }

        private static async Task WriteStructBegin(short id)
        {
            await _thrift.WriteFieldBeginAsync(new TField(null, TType.Struct, id));
            /*
             * From Thrift source code:
             * Write a struct begin. This doesn't actually put anything on the wire. We
             * use it as an opportunity to put special placeholder markers on the field
             * stack so we can get the field id deltas correct.
             */
            await _thrift.WriteStructBeginAsync(new TStruct());
        }

        private static async Task WriteStructEnd()
        {
            await _thrift.WriteStructEndAsync();
        }

        private static async Task WriteInt64(short id, long value)
        {
            await _thrift.WriteFieldBeginAsync(new TField(null, TType.I64, id));
            await _thrift.WriteI64Async(value);
        }

        private static async Task WriteInt32(short id, int value)
        {
            await _thrift.WriteFieldBeginAsync(new TField(null, TType.I32, id));
            await _thrift.WriteI32Async(value);
        }

        private static async Task WriteByte(short id, sbyte value)
        {
            await _thrift.WriteFieldBeginAsync(new TField(null, TType.Byte, id));
            await _thrift.WriteByteAsync(value);
        }

        private static async Task WriteBool(short id, bool value)
        {
            await _thrift.WriteFieldBeginAsync(new TField(null, TType.Bool, id));
            await _thrift.WriteBoolAsync(value);
        }

        private static async Task WriteListInt32(short id, int[] values)
        {
            await _thrift.WriteFieldBeginAsync(new TField(null, TType.List, id));
            await _thrift.WriteListBeginAsync(new TList(TType.I32, values.Length));
            foreach (var value in values)
            {
                await _thrift.WriteI32Async(value);
            }
        }

        private static async Task WriteFieldStop()
        {
            await _thrift.WriteFieldStopAsync();
        }
    }
}

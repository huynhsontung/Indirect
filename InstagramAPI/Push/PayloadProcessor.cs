using System;
using System.IO;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Storage.Streams;
using Ionic.Zlib;
using Thrift.Protocol;
using Thrift.Protocol.Entities;
using Thrift.Transport.Client;
using CompressionLevel = Ionic.Zlib.CompressionLevel;
using CompressionMode = Ionic.Zlib.CompressionMode;
using System.Threading;

namespace InstagramAPI.Push
{
    /*
     * Reference from Valga/Fbns-react
     * https://github.com/valga/fbns-react
     */
    public static class PayloadProcessor
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

        /// <summary>
        /// Make a complete payload from <see cref="FbnsConnectionData"/> using Thrift.
        /// </summary>
        /// <returns>Payload</returns>
        public static async Task<IBuffer> BuildPayload(FbnsConnectionData data, CancellationToken cancellationToken)
        {
            _memoryBufferTransport = new TMemoryBufferTransport(new Thrift.TConfiguration());
            _thrift = new TCompactProtocol(_memoryBufferTransport);
            _payloadData = data;

            var rawPayload = await ToThrift(cancellationToken);

            // zlib deflate
            var dataStream = new MemoryStream(512);
            using (var zlibStream = new ZlibStream(dataStream, CompressionMode.Compress, CompressionLevel.Level9, true))
            {
                await zlibStream.WriteAsync(rawPayload, 0, rawPayload.Length);
            }

            var compressed = dataStream.GetWindowsRuntimeBuffer(0, (int)dataStream.Length);
            return compressed;
        }

        private static async Task<byte[]> ToThrift(CancellationToken cancellationToken)
        {
            await WriteString(CLIENT_ID, _payloadData.ClientId, cancellationToken);

            #region Write struct ClientInfo
            await WriteStructBegin(CLIENT_INFO, cancellationToken);
            await WriteInt64(USER_ID, _payloadData.UserId, cancellationToken);
            await WriteString(USER_AGENT, _payloadData.UserAgent, cancellationToken);
            await WriteInt64(CLIENT_CAPABILITIES, _payloadData.ClientCapabilities, cancellationToken);
            await WriteInt64(ENDPOINT_CAPABILITIES, _payloadData.EndpointCapabilities, cancellationToken);
            await WriteInt32(PUBLISH_FORMAT, _payloadData.PublishFormat, cancellationToken);
            await WriteBool(NO_AUTOMATIC_FOREGROUND, _payloadData.NoAutomaticForeground, cancellationToken);
            await WriteBool(MAKE_USER_AVAILABLE_IN_FOREGROUND, _payloadData.MakeUserAvailableInForeground, cancellationToken);
            await WriteString(DEVICE_ID, _payloadData.DeviceId, cancellationToken);
            await WriteBool(IS_INITIALLY_FOREGROUND, _payloadData.IsInitiallyForeground, cancellationToken);
            await WriteInt32(NETWORK_TYPE, _payloadData.NetworkType, cancellationToken);
            await WriteInt32(NETWORK_SUBTYPE, _payloadData.NetworkSubtype, cancellationToken);
            if (_payloadData.ClientMqttSessionId == 0)
            {
                var difference = DateTime.Today.DayOfWeek - DayOfWeek.Monday;
                var lastMonday = new DateTimeOffset(DateTime.Today.Subtract(TimeSpan.FromDays(difference > 0 ? difference : 7)));
                _payloadData.ClientMqttSessionId = DateTimeOffset.Now.ToUnixTimeMilliseconds() - lastMonday.ToUnixTimeMilliseconds();
            }

            await WriteInt64(CLIENT_MQTT_SESSION_ID, _payloadData.ClientMqttSessionId, cancellationToken);
            await WriteListInt32(SUBSCRIBE_TOPICS, _payloadData.SubscribeTopics, cancellationToken);
            await WriteString(CLIENT_TYPE, _payloadData.ClientType, cancellationToken);
            await WriteInt64(APP_ID, _payloadData.AppId, cancellationToken);
            await WriteString(DEVICE_SECRET, _payloadData.DeviceSecret, cancellationToken);
            await WriteByte(CLIENT_STACK, _payloadData.ClientStack, cancellationToken);
            await WriteFieldStop(cancellationToken);
            await WriteStructEnd(cancellationToken);
            #endregion

            await WriteString(PASSWORD, _payloadData.Password, cancellationToken);
            await WriteFieldStop(cancellationToken);
            return _memoryBufferTransport.GetBuffer();
        }

        private static async Task WriteString(short id, string str, CancellationToken cancellationToken)
        {
            if (str == null) str = "";
            await _thrift.WriteFieldBeginAsync(new TField(null, TType.String, id), cancellationToken);
            await _thrift.WriteStringAsync(str, cancellationToken);
        }

        private static async Task WriteStructBegin(short id, CancellationToken cancellationToken)
        {
            await _thrift.WriteFieldBeginAsync(new TField(null, TType.Struct, id),cancellationToken);
            /*
             * From Thrift source code:
             * Write a struct begin. This doesn't actually put anything on the wire. We
             * use it as an opportunity to put special placeholder markers on the field
             * stack so we can get the field id deltas correct.
             */
            await _thrift.WriteStructBeginAsync(new TStruct(), cancellationToken);
        }

        private static async Task WriteStructEnd(CancellationToken cancellationToken)
        {
            await _thrift.WriteStructEndAsync(cancellationToken);
        }

        private static async Task WriteInt64(short id, long value, CancellationToken cancellationToken)
        {
            await _thrift.WriteFieldBeginAsync(new TField(null, TType.I64, id), cancellationToken);
            await _thrift.WriteI64Async(value, cancellationToken);
        }

        private static async Task WriteInt32(short id, int value, CancellationToken cancellationToken)
        {
            await _thrift.WriteFieldBeginAsync(new TField(null, TType.I32, id), cancellationToken);
            await _thrift.WriteI32Async(value, cancellationToken);
        }

        private static async Task WriteByte(short id, sbyte value, CancellationToken cancellationToken)
        {
            await _thrift.WriteFieldBeginAsync(new TField(null, TType.Byte, id), cancellationToken);
            await _thrift.WriteByteAsync(value, cancellationToken);
        }

        private static async Task WriteBool(short id, bool value, CancellationToken cancellationToken)
        {
            await _thrift.WriteFieldBeginAsync(new TField(null, TType.Bool, id), cancellationToken);
            await _thrift.WriteBoolAsync(value, cancellationToken);
        }

        private static async Task WriteListInt32(short id, int[] values, CancellationToken cancellationToken)
        {
            await _thrift.WriteFieldBeginAsync(new TField(null, TType.List, id), cancellationToken);
            await _thrift.WriteListBeginAsync(new TList(TType.I32, values.Length), cancellationToken);
            foreach (var value in values)
            {
                await _thrift.WriteI32Async(value, cancellationToken);
            }
        }

        private static async Task WriteFieldStop(CancellationToken cancellationToken)
        {
            await _thrift.WriteFieldStopAsync(cancellationToken);
        }
    }
}

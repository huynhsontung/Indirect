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
    internal class PayloadProcessor : IDisposable
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

        private TCompactProtocol Protocol { get; }

        private TMemoryBufferTransport MemoryBufferTransport { get; }

        /// <summary>
        /// Make a complete payload from <see cref="FbnsConnectionData"/> using Thrift.
        /// </summary>
        /// <returns>Payload</returns>
        public static async Task<IBuffer> BuildPayload(FbnsConnectionData data, CancellationToken cancellationToken)
        {
            using (var instance = new PayloadProcessor())
            {
                var rawPayload = await instance.ToThrift(data, cancellationToken);

                // zlib deflate
                var dataStream = new MemoryStream(512);
                using (var zlibStream = new ZlibStream(dataStream, CompressionMode.Compress, CompressionLevel.Level9, true))
                {
                    await zlibStream.WriteAsync(rawPayload, 0, rawPayload.Length, cancellationToken);
                }

                var compressed = dataStream.GetWindowsRuntimeBuffer(0, (int)dataStream.Length);
                return compressed;
            }
        }

        private PayloadProcessor()
        {
            MemoryBufferTransport = new TMemoryBufferTransport(new Thrift.TConfiguration());
            Protocol = new TCompactProtocol(MemoryBufferTransport);
        }

        private async Task<byte[]> ToThrift(FbnsConnectionData payloadData, CancellationToken cancellationToken)
        {
            await WriteString(CLIENT_ID, payloadData.ClientId, cancellationToken);

            #region Write struct ClientInfo
            await WriteStructBegin(CLIENT_INFO, cancellationToken);
            await WriteInt64(USER_ID, payloadData.UserId, cancellationToken);
            await WriteString(USER_AGENT, payloadData.UserAgent, cancellationToken);
            await WriteInt64(CLIENT_CAPABILITIES, payloadData.ClientCapabilities, cancellationToken);
            await WriteInt64(ENDPOINT_CAPABILITIES, payloadData.EndpointCapabilities, cancellationToken);
            await WriteInt32(PUBLISH_FORMAT, payloadData.PublishFormat, cancellationToken);
            await WriteBool(NO_AUTOMATIC_FOREGROUND, payloadData.NoAutomaticForeground, cancellationToken);
            await WriteBool(MAKE_USER_AVAILABLE_IN_FOREGROUND, payloadData.MakeUserAvailableInForeground, cancellationToken);
            await WriteString(DEVICE_ID, payloadData.DeviceId, cancellationToken);
            await WriteBool(IS_INITIALLY_FOREGROUND, payloadData.IsInitiallyForeground, cancellationToken);
            await WriteInt32(NETWORK_TYPE, payloadData.NetworkType, cancellationToken);
            await WriteInt32(NETWORK_SUBTYPE, payloadData.NetworkSubtype, cancellationToken);
            if (payloadData.ClientMqttSessionId == 0)
            {
                var difference = DateTime.Today.DayOfWeek - DayOfWeek.Monday;
                var lastMonday = new DateTimeOffset(DateTime.Today.Subtract(TimeSpan.FromDays(difference > 0 ? difference : 7)));
                payloadData.ClientMqttSessionId = DateTimeOffset.Now.ToUnixTimeMilliseconds() - lastMonday.ToUnixTimeMilliseconds();
            }

            await WriteInt64(CLIENT_MQTT_SESSION_ID, payloadData.ClientMqttSessionId, cancellationToken);
            await WriteListInt32(SUBSCRIBE_TOPICS, payloadData.SubscribeTopics, cancellationToken);
            await WriteString(CLIENT_TYPE, payloadData.ClientType, cancellationToken);
            await WriteInt64(APP_ID, payloadData.AppId, cancellationToken);
            await WriteString(DEVICE_SECRET, payloadData.DeviceSecret, cancellationToken);
            await WriteByte(CLIENT_STACK, payloadData.ClientStack, cancellationToken);
            await WriteFieldStop(cancellationToken);
            await WriteStructEnd(cancellationToken);
            #endregion

            await WriteString(PASSWORD, payloadData.Password, cancellationToken);
            await WriteFieldStop(cancellationToken);
            return MemoryBufferTransport.GetBuffer();
        }

        private async Task WriteString(short id, string str, CancellationToken cancellationToken)
        {
            if (str == null) str = "";
            await Protocol.WriteFieldBeginAsync(new TField(null, TType.String, id), cancellationToken);
            await Protocol.WriteStringAsync(str, cancellationToken);
        }

        private async Task WriteStructBegin(short id, CancellationToken cancellationToken)
        {
            await Protocol.WriteFieldBeginAsync(new TField(null, TType.Struct, id),cancellationToken);
            /*
             * From Thrift source code:
             * Write a struct begin. This doesn't actually put anything on the wire. We
             * use it as an opportunity to put special placeholder markers on the field
             * stack so we can get the field id deltas correct.
             */
            await Protocol.WriteStructBeginAsync(new TStruct(), cancellationToken);
        }

        private async Task WriteStructEnd(CancellationToken cancellationToken)
        {
            await Protocol.WriteStructEndAsync(cancellationToken);
        }

        private async Task WriteInt64(short id, long value, CancellationToken cancellationToken)
        {
            await Protocol.WriteFieldBeginAsync(new TField(null, TType.I64, id), cancellationToken);
            await Protocol.WriteI64Async(value, cancellationToken);
        }

        private async Task WriteInt32(short id, int value, CancellationToken cancellationToken)
        {
            await Protocol.WriteFieldBeginAsync(new TField(null, TType.I32, id), cancellationToken);
            await Protocol.WriteI32Async(value, cancellationToken);
        }

        private async Task WriteByte(short id, sbyte value, CancellationToken cancellationToken)
        {
            await Protocol.WriteFieldBeginAsync(new TField(null, TType.Byte, id), cancellationToken);
            await Protocol.WriteByteAsync(value, cancellationToken);
        }

        private async Task WriteBool(short id, bool value, CancellationToken cancellationToken)
        {
            await Protocol.WriteFieldBeginAsync(new TField(null, TType.Bool, id), cancellationToken);
            await Protocol.WriteBoolAsync(value, cancellationToken);
        }

        private async Task WriteListInt32(short id, int[] values, CancellationToken cancellationToken)
        {
            await Protocol.WriteFieldBeginAsync(new TField(null, TType.List, id), cancellationToken);
            await Protocol.WriteListBeginAsync(new TList(TType.I32, values.Length), cancellationToken);
            foreach (var value in values)
            {
                await Protocol.WriteI32Async(value, cancellationToken);
            }
        }

        private async Task WriteFieldStop(CancellationToken cancellationToken)
        {
            await Protocol.WriteFieldStopAsync(cancellationToken);
        }

        public void Dispose()
        {
            Protocol?.Dispose();
            MemoryBufferTransport?.Dispose();
        }
    }
}

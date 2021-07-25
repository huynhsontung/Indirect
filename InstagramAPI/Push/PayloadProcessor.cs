using System;
using System.IO;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Storage.Streams;
using Ionic.Zlib;
using Thrift.Protocol;
using Thrift.Transport.Client;
using CompressionLevel = Ionic.Zlib.CompressionLevel;
using CompressionMode = Ionic.Zlib.CompressionMode;
using System.Threading;

namespace InstagramAPI.Push
{
    internal class PayloadProcessor : IDisposable
    {
        private TCompactProtocol Protocol { get; }

        private TMemoryBufferTransport MemoryBufferTransport { get; }

        /// <summary>
        /// Make a complete payload from <see cref="PushConnectionData"/> using Thrift.
        /// </summary>
        /// <returns>Payload</returns>
        public static async Task<IBuffer> BuildPayload(PushConnectionData data, CancellationToken cancellationToken)
        {
            using (var instance = new PayloadProcessor())
            {
                var payload = data.ToPayload();
                try
                {
                    await payload.WriteAsync(instance.Protocol, cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    return null;
                }

                var rawPayload = instance.MemoryBufferTransport.GetBuffer();

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

        public void Dispose()
        {
            Protocol?.Dispose();
            MemoryBufferTransport?.Dispose();
        }
    }
}

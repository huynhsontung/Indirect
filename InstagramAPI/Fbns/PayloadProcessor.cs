using System;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage.Streams;
using Thrift.Protocol;
using Thrift.Transport.Client;

namespace InstagramAPI.Fbns
{
    internal class PayloadProcessor : IDisposable
    {
        private TCompactProtocol Protocol { get; }

        private TMemoryBufferTransport MemoryBufferTransport { get; }

        /// <summary>
        /// Make a complete payload from <see cref="BaseConnectionData"/> using Thrift.
        /// </summary>
        /// <returns>Payload</returns>
        public static async Task<IBuffer> BuildPayload(BaseConnectionData data, CancellationToken cancellationToken = default)
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

                return instance.MemoryBufferTransport.GetBuffer().AsBuffer();
            }
        }

        public static async Task<T> DeserializeObject<T>(IBuffer source, T target, CancellationToken cancellationToken = default)
            where T : TBase
        {
            using (var instance = new PayloadProcessor(source.ToArray()))
            {
                try
                {
                    await target.ReadAsync(instance.Protocol, cancellationToken);
                }
                catch (OperationCanceledException )
                {
                    // pass
                }

                return target;
            }
        }

        private PayloadProcessor()
        {
            MemoryBufferTransport = new TMemoryBufferTransport(new Thrift.TConfiguration());
            Protocol = new TCompactProtocol(MemoryBufferTransport);
        }

        private PayloadProcessor(byte[] data)
        {
            MemoryBufferTransport = new TMemoryBufferTransport(data, new Thrift.TConfiguration());
            Protocol = new TCompactProtocol(MemoryBufferTransport);
        }

        public void Dispose()
        {
            Protocol?.Dispose();
            MemoryBufferTransport?.Dispose();
        }
    }
}

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage;

namespace Indirect.Utilities
{
    internal static class SyncLock
    {
        public static bool Acquired => _lockFile != null;

        private static FileStream _lockFile;
        private static CancellationTokenSource _tokenSource;

        internal static async void Acquire()
        {
            if (Acquired) return;
            _tokenSource = new CancellationTokenSource();
            var storageFolder = ApplicationData.Current.LocalFolder;
            var storageItem = await storageFolder.CreateFileAsync("SyncLock.mutex", CreationCollisionOption.OpenIfExists);
            for (int i = 0; i < 5; i++)
            {
                try
                {
                    _lockFile = new FileStream(storageItem.Path, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
                    return;
                }
                catch (Exception)
                {
                    try
                    {
                        await Task.Delay(200, _tokenSource.Token);
                    }
                    catch (TaskCanceledException)
                    {
                        return;
                    }
                }
            }
        }

        internal static void Release()
        {
            try
            {
                _tokenSource?.Cancel();
                _lockFile?.Dispose();
                _lockFile = null;
            }
            catch (Exception)
            {
                // pass
            }
        }
    }
}

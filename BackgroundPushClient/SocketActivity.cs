using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Windows.ApplicationModel.Background;
using Windows.Networking.Sockets;
using Windows.Storage;
using InstagramAPI;
using InstagramAPI.Push;
using InstagramAPI.Utils;

namespace BackgroundPushClient
{
    public sealed class SocketActivity : IBackgroundTask
    {
        private FileStream _lockFile;

        public async void Run(IBackgroundTaskInstance taskInstance)
        {
            Instagram.StartAppCenter();
            var deferral = taskInstance.GetDeferral();
            this.Log("-------------- Start of background task --------------");
            var details = (SocketActivityTriggerDetails) taskInstance.TriggerDetails;
            Utils.PopMessageToast($"{details.Reason}");
            this.Log($"{details.Reason}");
            try
            {
                if (!Instagram.InternetAvailable())
                {
                    return;
                }

                // TODO: Load specific session based on context data
                var session = await SessionManager.TryLoadLastSessionAsync();
                if (session == null)
                {
                    if (details.Reason == SocketActivityTriggerReason.SocketClosed)
                    {
                        return;
                    }

                    throw new Exception($"{nameof(SocketActivity)} triggered without session.");
                }

                var instagram = new Instagram(session);
                instagram.PushClient.MessageReceived += Utils.OnMessageReceived;
                instagram.PushClient.ExceptionsCaught += Utils.PushClientOnExceptionsCaught;
                switch (details.Reason)
                {
                    case SocketActivityTriggerReason.KeepAliveTimerExpired:
                    case SocketActivityTriggerReason.SocketActivity:
                    {
                        try
                        {
                            var socket = details.SocketInformation.StreamSocket;
                            await instagram.PushClient.StartWithExistingSocket(socket);
                        }
                        catch (Exception e)
                        {
                            await Task.Delay(TimeSpan.FromSeconds(1));
                            if (PushClient.SocketRegistered())
                            {
                                Utils.PopMessageToast($"[{details.Reason}] {e}");
                                return;
                            }
                            else
                            {
                                await instagram.PushClient.StartFresh();
                            }
                        }

                        break;
                    }
                    case SocketActivityTriggerReason.SocketClosed:
                    {
                        await Task.Delay(TimeSpan.FromSeconds(3));
                        if (!await Utils.TryAcquireSyncLock())
                        {
                            this.Log("Failed to open SyncLock file after extended wait. Main application might be running. Exit background task.");
                            return;
                        }

                        await instagram.PushClient.StartFresh();
                        try
                        {
                            var socket = details.SocketInformation.StreamSocket;
                            socket?.Dispose();
                        }
                        catch (Exception)
                        {
                            // pass
                        }

                        break;
                    }
                    default:
                        return;
                }

                await Task.Delay(TimeSpan.FromSeconds(PushClient.WaitTime));  // Wait 5s to complete all outstanding IOs (hopefully)
                await instagram.PushClient.TransferPushSocket();
                await SessionManager.SaveSessionAsync(instagram);
            }
            catch (Exception e)
            {
                Utils.PopMessageToast($"[{details.Reason}] {e}");
                DebugLogger.LogException(e, properties: new Dictionary<string, string>
                {
                    {"SocketActivityTriggerReason", details.Reason.ToString()}
                });
                this.Log($"{typeof(SocketActivity).FullName}: Can't finish push cycle. Abort.");
            }
            finally
            {
                ReleaseLock();
                this.Log("-------------- End of background task --------------");
                deferral.Complete();
            }
        }

        private async Task<bool> TryAcquireLock()
        {
            var storageFolder = ApplicationData.Current.LocalFolder;
            var storageItem = await storageFolder.CreateFileAsync("SocketActivity.mutex", CreationCollisionOption.OpenIfExists);
            try
            {
                _lockFile = new FileStream(storageItem.Path, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
            }
            catch (Exception)
            {
                return false;
            }

            return true;
        }

        private void ReleaseLock()
        {
            _lockFile?.Dispose();
        }
    }
}

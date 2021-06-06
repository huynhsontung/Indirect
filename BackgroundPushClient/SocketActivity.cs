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
using Microsoft.AppCenter;
using Microsoft.AppCenter.Analytics;
using Microsoft.AppCenter.Crashes;

namespace BackgroundPushClient
{
    public sealed class SocketActivity : IBackgroundTask
    {
        private FileStream _lockFile;

        public async void Run(IBackgroundTaskInstance taskInstance)
        {
#if !DEBUG
            AppCenter.Start(Secrets.APPCENTER_SECRET, typeof(Analytics), typeof(Crashes));
#endif
            var deferral = taskInstance.GetDeferral();
            this.Log("-------------- Start of background task --------------");
            var details = (SocketActivityTriggerDetails) taskInstance.TriggerDetails;
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
                instagram.PushClient.ExceptionsCaught += PushClientOnExceptionsCaught;
                switch (details.Reason)
                {
                    case SocketActivityTriggerReason.KeepAliveTimerExpired:
                    case SocketActivityTriggerReason.SocketActivity:
                    {
                        var socket = details.SocketInformation.StreamSocket;
                        instagram.PushClient.StartWithExistingSocket(socket);
                        break;
                    }
                    case SocketActivityTriggerReason.SocketClosed:
                    {
                        await Task.Delay(TimeSpan.FromSeconds(3));
                        if (!await Utils.TryAcquireSyncLock())
                        {
                            this.Log(
                                "Failed to open SyncLock file after extended wait. Main application might be running. Exit background task.");
                            return;
                        }

                        try
                        {
                            var socket = details.SocketInformation.StreamSocket;
                            socket?.Dispose();
                        }
                        catch (Exception)
                        {
                            // pass
                        }

                        await instagram.PushClient.StartFresh();
                        break;
                    }
                    default:
                        return;
                }

                await Task.Delay(TimeSpan.FromSeconds(PushClient.WaitTime));  // Wait 5s to complete all outstanding IOs (hopefully)
                await instagram.PushClient.TransferPushSocket(false);
                await SessionManager.SaveSessionAsync(instagram);
            }
            catch (Exception e)
            {
#if DEBUG
                Utils.PopMessageToast($"[{details.Reason}] {e}");
#endif
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

        private static void PushClientOnExceptionsCaught(object sender, UnhandledExceptionEventArgs e)
        {
            var exception = (Exception) e.ExceptionObject;
#if DEBUG
            Utils.PopMessageToast(exception.ToString());
#endif
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

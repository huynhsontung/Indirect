using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.Background;
using Windows.Storage;
using Windows.Web.Http;
using BackgroundPushClient.Push;
using InstaSharper.API.Builder;
using InstaSharper.API.Push;
using InstaSharper.Classes;
using InstaSharper.Classes.DeviceInfo;
using InstaSharper.Logger;
using HttpClient = System.Net.Http.HttpClient;

namespace BackgroundPushClient
{
    public sealed class BackgroundPushClient : IBackgroundTask
    {
        private const string STATE_FILE_NAME = "state.bin";

        private UserSessionData _user;
        private IHttpRequestProcessor _httpRequestProcessor;
        private AndroidDevice _device;
        private FbnsConnectionData _fbnsConnectionData;

        public async void Run(IBackgroundTaskInstance taskInstance)
        {
            var deferral = taskInstance.GetDeferral();
            try
            {
                await LoadData();
                if ((DateTime.Now - _fbnsConnectionData.FbnsTokenLastUpdated).TotalHours > 24) _fbnsConnectionData.FbnsToken = "";
                if (string.IsNullOrEmpty(_fbnsConnectionData.UserAgent))
                    _fbnsConnectionData.UserAgent = FbnsUserAgent.BuildFbUserAgent(_device);


            }
            catch
            {
                Debug.WriteLine("Can't finish push cycle. Abort.");
            }
            finally
            {
                deferral.Complete();
            }
        }

        private async Task LoadData()
        {
            var localFolder = ApplicationData.Current.LocalFolder;
            var stateFile = await localFolder.GetFileAsync(STATE_FILE_NAME);
            using (var stateStream = await stateFile.OpenStreamForReadAsync())
            {
                if (stateStream.Length > 0)
                {
                    stateStream.Seek(0, SeekOrigin.Begin);
                    var formatter = new BinaryFormatter();
                    var stateData = (StateData)formatter.Deserialize(stateStream);
                    if(!stateData.IsAuthenticated) throw new Exception("User not authenticated.");
                    _user = stateData.UserSession;
                    _device = stateData.DeviceInfo;
                    _fbnsConnectionData = stateData.FbnsConnectionData;
                    
                    var httpHandler = new HttpClientHandler();
                    httpHandler.CookieContainer = stateData.Cookies;
                    var httpClient = new HttpClient(httpHandler) {BaseAddress = new Uri("https://i.instagram.com")};
                    var requestMessage = new ApiRequestMessage
                    {
                        PhoneId = _device.PhoneId.ToString(),
                        Guid = _device.Uuid,
                        Password = _user?.Password,
                        Username = _user?.UserName,
                        DeviceId = _device.DeviceId,
                        AdId = _device.AdId.ToString()
                    };

                    _httpRequestProcessor =
                        new HttpRequestProcessor(RequestDelay.Empty(), httpClient, httpHandler, requestMessage, null);
                }
                else
                {
                    throw new Exception("Cannot load data required for push client.");
                }
            }
        }
    }
}

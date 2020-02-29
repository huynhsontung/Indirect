using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Windows.Web.Http;
using InstagramAPI.Classes;
using InstagramAPI.Classes.Direct;
using InstagramAPI.Classes.Responses;
using Newtonsoft.Json;
using HttpMethod = System.Net.Http.HttpMethod;

namespace InstagramAPI
{
    public partial class Instagram
    {
        public async Task<Result<InboxContainer>> GetInboxAsync(PaginationParameters paginationParameters)
        {
            ValidateLoggedIn();
            try
            {
                if (paginationParameters == null)
                    paginationParameters = PaginationParameters.MaxPagesToLoad(1);

                var inboxResult = await GetDirectInbox(paginationParameters.NextMaxId);
                if (!inboxResult.IsSucceeded)
                    return inboxResult;
                var inbox = inboxResult.Value;
                paginationParameters.NextMaxId = inbox.Inbox.OldestCursor;
                var pagesLoaded = 1;
                while (inbox.Inbox.HasOlder
                      && !string.IsNullOrEmpty(inbox.Inbox.OldestCursor)
                      && pagesLoaded < paginationParameters.MaximumPagesToLoad)
                {
                    var nextInbox = await GetDirectInbox(inbox.Inbox.OldestCursor);

                    if (!nextInbox.IsSucceeded)
                        return Result<InboxContainer>.Fail(inbox, nextInbox.Message, nextInbox.Json);

                    inbox.Inbox.OldestCursor = paginationParameters.NextMaxId = nextInbox.Value.Inbox.OldestCursor;
                    inbox.Inbox.HasOlder = nextInbox.Value.Inbox.HasOlder;
                    inbox.Inbox.BlendedInboxEnabled = nextInbox.Value.Inbox.BlendedInboxEnabled;
                    inbox.Inbox.UnseenCount = nextInbox.Value.Inbox.UnseenCount;
                    inbox.Inbox.UnseenCountTs = nextInbox.Value.Inbox.UnseenCountTs;
                    inbox.Inbox.Threads.AddRange(nextInbox.Value.Inbox.Threads);
                    pagesLoaded++;
                }

                return Result<InboxContainer>.Success(inbox);
            }
            catch (Exception exception)
            {
                _logger?.LogException(exception);
                return Result<InboxContainer>.Except(exception);
            }
        }

        private async Task<Result<InboxContainer>> GetDirectInbox(string maxId = null)
        {
            try
            {
                var directInboxUri = UriCreator.GetDirectInboxUri(maxId);
                var response = await _httpClient.GetAsync(directInboxUri);
                var json = await response.Content.ReadAsStringAsync();
                _logger?.LogResponse(response);

                if (response.StatusCode != HttpStatusCode.Ok)
                    return Result<InboxContainer>.Fail(json, response.ReasonPhrase);
                var inbox = JsonConvert.DeserializeObject<InboxContainer>(json);
                return Result<InboxContainer>.Success(inbox);
            }
            catch (Exception exception)
            {
                _logger?.LogException(exception);
                return Result<InboxContainer>.Except(exception);
            }
        }

        /// <summary>
        ///     Get direct inbox thread by its id asynchronously
        /// </summary>
        /// <param name="threadId">Thread id</param>
        /// <param name="paginationParameters">Pagination parameters: next id and max amount of pages to load</param>
        /// <returns>
        ///     <see cref="DirectThread" />
        /// </returns>
        public async Task<Result<DirectThread>> GetThreadAsync(string threadId, PaginationParameters paginationParameters)
        {
            ValidateLoggedIn();
            try
            {
                if (paginationParameters == null)
                    paginationParameters = PaginationParameters.MaxPagesToLoad(1);

                var thread = await GetDirectThread(threadId, paginationParameters.NextMaxId);
                if (!thread.IsSucceeded)
                    return thread;

                var threadResponse = thread.Value;
                paginationParameters.NextMaxId = threadResponse.OldestCursor;
                var pagesLoaded = 1;

                while ((threadResponse.HasOlder ?? false)
                      && !string.IsNullOrEmpty(threadResponse.OldestCursor)
                      && pagesLoaded < paginationParameters.MaximumPagesToLoad)
                {
                    var nextThread = await GetDirectThread(threadId, threadResponse.OldestCursor);

                    if (!nextThread.IsSucceeded)
                    {
                        threadResponse.Items.Reverse();
                        return Result<DirectThread>.Fail(threadResponse, nextThread.Message, nextThread.Json);
                    }

                    threadResponse.OldestCursor = paginationParameters.NextMaxId = nextThread.Value.OldestCursor;
                    threadResponse.HasOlder = nextThread.Value.HasOlder;
                    threadResponse.Canonical = nextThread.Value.Canonical;
                    threadResponse.ExpiringMediaReceiveCount = nextThread.Value.ExpiringMediaReceiveCount;
                    threadResponse.ExpiringMediaSendCount = nextThread.Value.ExpiringMediaSendCount;
                    threadResponse.HasNewer = nextThread.Value.HasNewer;
                    threadResponse.LastActivity = nextThread.Value.LastActivity;
                    threadResponse.LastSeenAt = nextThread.Value.LastSeenAt;
                    threadResponse.ReshareReceiveCount = nextThread.Value.ReshareReceiveCount;
                    threadResponse.ReshareSendCount = nextThread.Value.ReshareSendCount;
                    threadResponse.Status = nextThread.Value.Status;
                    threadResponse.Title = nextThread.Value.Title;
                    threadResponse.IsGroup = nextThread.Value.IsGroup;
                    threadResponse.IsSpam = nextThread.Value.IsSpam;
                    threadResponse.IsPin = nextThread.Value.IsPin;
                    threadResponse.Muted = nextThread.Value.Muted;
                    threadResponse.PendingScore = nextThread.Value.PendingScore;
                    threadResponse.Pending = nextThread.Value.Pending;
                    threadResponse.Users = nextThread.Value.Users;
                    threadResponse.ValuedRequest = nextThread.Value.ValuedRequest;
                    threadResponse.VCMuted = nextThread.Value.VCMuted;
                    threadResponse.ViewerId = nextThread.Value.ViewerId;
                    threadResponse.Items.AddRange(nextThread.Value.Items);
                    pagesLoaded++;
                }

                //Reverse for Chat Order
                threadResponse.Items.Reverse();

                return Result<DirectThread>.Success(threadResponse);
            }
            catch (Exception exception)
            {
                _logger?.LogException(exception);
                return Result<DirectThread>.Except(exception);
            }
        }

        private async Task<Result<DirectThread>> GetDirectThread(string threadId, string maxId = null)
        {
            try
            {
                var directInboxUri = UriCreator.GetDirectInboxThreadUri(threadId, maxId);
                var response = await _httpClient.GetAsync(directInboxUri);
                var json = await response.Content.ReadAsStringAsync();
                _logger?.LogResponse(response);

                if (response.StatusCode != HttpStatusCode.Ok)
                    return Result<DirectThread>.Fail(json, response.ReasonPhrase);
                var threadResponse = JsonConvert.DeserializeObject<DirectThread>(json);

                return Result<DirectThread>.Success(threadResponse);
            }
            catch (Exception exception)
            {
                _logger?.LogException(exception);
                return Result<DirectThread>.Except(exception);
            }
        }

        /// <summary>
        ///     Send a like to the conversation
        /// </summary>
        /// <param name="threadId">Thread id</param>
        public async Task<Result<ItemAckPayloadResponse>> SendLikeAsync(string threadId)
        {
            ValidateLoggedIn();
            try
            {
                var uri = UriCreator.GetDirectThreadBroadcastLikeUri();

                var data = new Dictionary<string, string>
                {
                    {"action", "send_item"},
                    {"_csrftoken", Session.CsrfToken},
                    {"_uuid", Device.Uuid.ToString()},
                    {"thread_id", $"{threadId}"},
                    {"client_context", Guid.NewGuid().ToString()}
                };
                var response = await _httpClient.PostAsync(uri, new HttpFormUrlEncodedContent(data));
                var json = await response.Content.ReadAsStringAsync();
                _logger?.LogResponse(response);
                if (response.StatusCode != HttpStatusCode.Ok)
                    return Result<ItemAckPayloadResponse>.Fail(json, response.ReasonPhrase);
                var obj = JsonConvert.DeserializeObject<ItemAckResponse>(json);
                return obj.IsOk()
                    ? Result<ItemAckPayloadResponse>.Success(obj.Payload)
                    : Result<ItemAckPayloadResponse>.Fail(json, response.ReasonPhrase);
            }
            catch (Exception exception)
            {
                _logger?.LogException(exception);
                return Result<ItemAckPayloadResponse>.Except(exception);
            }
        }
    }
}

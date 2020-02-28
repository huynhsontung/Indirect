using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using InstagramAPI.Classes;
using InstagramAPI.Classes.Direct;
using Newtonsoft.Json;

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
                if (inboxResult.Status != ResultStatus.Succeeded)
                    return inboxResult;
                var inbox = inboxResult.Value;
                paginationParameters.NextMaxId = inbox.Inbox.OldestCursor;
                var pagesLoaded = 1;
                while (inbox.Inbox.HasOlder
                      && !string.IsNullOrEmpty(inbox.Inbox.OldestCursor)
                      && pagesLoaded < paginationParameters.MaximumPagesToLoad)
                {
                    var nextInbox = await GetDirectInbox(inbox.Inbox.OldestCursor);

                    if (nextInbox.Status != ResultStatus.Succeeded)
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

                if (response.StatusCode != Windows.Web.Http.HttpStatusCode.Ok)
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
    }
}

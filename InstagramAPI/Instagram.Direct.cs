using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using InstagramAPI.Classes;
using InstagramAPI.Classes.Direct;

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

                InstaDirectInboxContainer Convert(InstaDirectInboxContainerResponse inboxContainerResponse)
                {
                    return ConvertersFabric.Instance.GetDirectInboxConverter(inboxContainerResponse).Convert();
                }

                var inbox = await GetDirectInbox(paginationParameters.NextMaxId);
                if (!inbox.Succeeded)
                    return Result.Fail(inbox.Info, default(InstaDirectInboxContainer));
                var inboxResponse = inbox.Value;
                paginationParameters.NextMaxId = inboxResponse.Inbox.OldestCursor;
                var pagesLoaded = 1;
                while (inboxResponse.Inbox.HasOlder
                      && !string.IsNullOrEmpty(inboxResponse.Inbox.OldestCursor)
                      && pagesLoaded < paginationParameters.MaximumPagesToLoad)
                {
                    var nextInbox = await GetDirectInbox(inboxResponse.Inbox.OldestCursor);

                    if (!nextInbox.Succeeded)
                        return Result.Fail(nextInbox.Info, Convert(nextInbox.Value));

                    inboxResponse.Inbox.OldestCursor = paginationParameters.NextMaxId = nextInbox.Value.Inbox.OldestCursor;
                    inboxResponse.Inbox.HasOlder = nextInbox.Value.Inbox.HasOlder;
                    inboxResponse.Inbox.BlendedInboxEnabled = nextInbox.Value.Inbox.BlendedInboxEnabled;
                    inboxResponse.Inbox.UnseenCount = nextInbox.Value.Inbox.UnseenCount;
                    inboxResponse.Inbox.UnseenCountTs = nextInbox.Value.Inbox.UnseenCountTs;
                    inboxResponse.Inbox.Threads.AddRange(nextInbox.Value.Inbox.Threads);
                    pagesLoaded++;
                }

                return Result.Success(ConvertersFabric.Instance.GetDirectInboxConverter(inboxResponse).Convert());
            }
            catch (HttpRequestException httpException)
            {
                _logger?.LogException(httpException);
                return Result.Fail(httpException, default(InstaDirectInboxContainer), ResponseType.NetworkProblem);
            }
            catch (Exception exception)
            {
                _logger?.LogException(exception);
                return Result.Fail<InstaDirectInboxContainer>(exception);
            }
        }
    }
}

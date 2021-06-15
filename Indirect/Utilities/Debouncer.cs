using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Threading;
using System.Threading.Tasks;

namespace Indirect.Utilities
{
    public static class Debouncer
    {
        public delegate void DelayedAction(bool cancelled);

        private static readonly Dictionary<string, CancellationTokenSource> TokenSources =
            new Dictionary<string, CancellationTokenSource>();

        public static void DelayExecute(string key, TimeSpan delay, DelayedAction action)
        {
            var _ = DelayExecuteAsync(key, delay, action);
        }

        public static void DelayExecute(string key, int delayInMilliseconds, DelayedAction action) =>
            DelayExecute(key, TimeSpan.FromMilliseconds(delayInMilliseconds), action);

        public static async Task DelayExecuteAsync(string key, TimeSpan delay, DelayedAction action)
        {
            var doneDelay = await Delay(key, delay).ConfigureAwait(true);
            action?.Invoke(!doneDelay);
        }

        public static async Task<bool> Delay(string key, TimeSpan delay)
        {
            Contract.Requires(!string.IsNullOrEmpty(key), nameof(key));
            CancellationTokenSource tokenSource;
            lock (TokenSources)
            {
                if (TokenSources.ContainsKey(key))
                {
                    TokenSources[key]?.Cancel();
                    TokenSources[key] = new CancellationTokenSource();
                }
                else
                {
                    TokenSources.Add(key, new CancellationTokenSource());
                }

                tokenSource = TokenSources[key];
            }

            try
            {
                var cancellationToken = tokenSource.Token;
                await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
                return !cancellationToken.IsCancellationRequested;
            }
            catch (TaskCanceledException)
            {
                return false;
            }
            finally
            {
                lock (TokenSources)
                {
                    tokenSource.Dispose();
                    if (TokenSources[key] == tokenSource)
                    {
                        TokenSources[key] = null;
                    }
                }
            }
        }

        public static Task<bool> Delay(string key, int delayInMilliseconds) =>
            Delay(key, TimeSpan.FromMilliseconds(delayInMilliseconds));
    }
}

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

        private static readonly Dictionary<string, Task> ThrottleTasks = new Dictionary<string, Task>();

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

        public static void CancelDelay(string key)
        {
            lock (TokenSources)
            {
                if (TokenSources.ContainsKey(key))
                {
                    TokenSources[key]?.Cancel();
                    TokenSources[key] = null;
                }
            }
        }

        public static bool Throttle(string key, TimeSpan delay)
        {
            lock (ThrottleTasks)
            {
                if (ThrottleTasks.ContainsKey(key))
                {
                    if (ThrottleTasks[key].IsCompleted)
                    {
                        ThrottleTasks[key].Dispose();
                        ThrottleTasks[key] = Task.Delay(delay);
                        return true;
                    }

                    return false;
                }

                ThrottleTasks.Add(key, Task.Delay(delay));
                return true;
            }
        }

        public static bool Throttle(string key, int delayInMilliseconds) =>
            Throttle(key, TimeSpan.FromMilliseconds(delayInMilliseconds));
    }
}

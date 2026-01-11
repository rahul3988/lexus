using System;
using System.Threading;
using System.Threading.Tasks;

namespace Lexus2_0.Core.Retry
{
    /// <summary>
    /// Retry policy with exponential backoff and failure handling
    /// </summary>
    public class RetryPolicy
    {
        private readonly int _maxRetries;
        private readonly TimeSpan _initialDelay;
        private readonly double _backoffMultiplier;
        private readonly TimeSpan _maxDelay;

        public RetryPolicy(int maxRetries = 3, TimeSpan? initialDelay = null, double backoffMultiplier = 2.0, TimeSpan? maxDelay = null)
        {
            _maxRetries = maxRetries;
            _initialDelay = initialDelay ?? TimeSpan.FromSeconds(2); // Increased default delay
            _backoffMultiplier = backoffMultiplier;
            _maxDelay = maxDelay ?? TimeSpan.FromMinutes(2); // Increased max delay for slow networks
        }

        /// <summary>
        /// Execute action with retry logic
        /// </summary>
        public async Task<T> ExecuteAsync<T>(Func<Task<T>> action, Func<Exception, bool>? shouldRetry = null)
        {
            Exception? lastException = null;
            var delay = _initialDelay;

            for (int attempt = 0; attempt <= _maxRetries; attempt++)
            {
                try
                {
                    return await action();
                }
                catch (Exception ex)
                {
                    lastException = ex;

                    // Check if we should retry this exception
                    if (shouldRetry != null && !shouldRetry(ex))
                    {
                        throw;
                    }

                    // Don't delay on last attempt
                    if (attempt < _maxRetries)
                    {
                        await Task.Delay(delay);
                        delay = TimeSpan.FromMilliseconds(Math.Min(delay.TotalMilliseconds * _backoffMultiplier, _maxDelay.TotalMilliseconds));
                    }
                }
            }

            throw new AggregateException($"Action failed after {_maxRetries + 1} attempts", lastException!);
        }

        /// <summary>
        /// Execute action with retry logic (void return)
        /// </summary>
        public async Task ExecuteAsync(Func<Task> action, Func<Exception, bool>? shouldRetry = null)
        {
            await ExecuteAsync(async () =>
            {
                await action();
                return true;
            }, shouldRetry);
        }
    }
}


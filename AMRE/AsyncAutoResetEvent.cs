//Based on https://badflyer.com/asyncmanualresetevent/

using System;
using System.Threading;
using System.Threading.Tasks;

namespace AMRE
{
    public sealed class AsyncAutoResetEvent
    {
        private volatile TaskCompletionSource<bool> _completionSource = new TaskCompletionSource<bool>();

        public Task WaitAsync(CancellationToken cancellationToken = default) => AwaitCompletion(Timeout.InfiniteTimeSpan, cancellationToken);

        public Task<bool> WaitAsync(TimeSpan timeout, CancellationToken cancellationToken = default) => AwaitCompletion(timeout, cancellationToken);

        public void Set()
        {
            _completionSource.TrySetResult(true);
            var currentCompletionSource = _completionSource;
            Interlocked.CompareExchange(ref _completionSource, new TaskCompletionSource<bool>(), currentCompletionSource);
        }

        private async Task<bool> AwaitCompletion(TimeSpan timeout, CancellationToken cancellationToken)
        {
            CancellationTokenSource timeoutToken = null;
            if (!cancellationToken.CanBeCanceled)
            {
                if (timeout == Timeout.InfiniteTimeSpan)
                    return await _completionSource.Task;
                timeoutToken = new CancellationTokenSource();
            }
            else
            {
                timeoutToken = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            }

            using (timeoutToken)
            {
                Task delayTask = Task.Delay(timeout, timeoutToken.Token).ContinueWith((result) => { var e = result.Exception; }, TaskContinuationOptions.ExecuteSynchronously);
                var resultingTask = await Task.WhenAny(_completionSource.Task, delayTask).ConfigureAwait(false);

                if (resultingTask != delayTask)
                {
                    timeoutToken.Cancel();
                    return true;
                }

                cancellationToken.ThrowIfCancellationRequested();
                return false;
            }
        }
    }
}

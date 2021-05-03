using System.Threading.Tasks;

namespace System.Threading
{
    public static class WaitHandleExtensions
    {
        public static async Task<bool> WaitOneAsync(this WaitHandle @this, int millisecondsTimeout, CancellationToken cancellationToken)
        {
            if (@this.GetType() == typeof(Mutex))
                throw new Exception("Async extensions do not work with mutex");
            
            RegisteredWaitHandle registeredHandle = null;
            CancellationTokenRegistration tokenRegistration = default;
            try
            {
                var tcs = new TaskCompletionSource<bool>();
                registeredHandle = ThreadPool.RegisterWaitForSingleObject(
                    @this,
                    (state, timedOut) => ((TaskCompletionSource<bool>)state).TrySetResult(!timedOut),
                    tcs,
                    millisecondsTimeout,
                    true);

                tokenRegistration = cancellationToken.Register(
                    state => ((TaskCompletionSource<bool>)state).TrySetCanceled(),
                    tcs);
                return await tcs.Task;
            }
            finally
            {
                if (registeredHandle != null)
                    registeredHandle.Unregister(null);
                tokenRegistration.Dispose();
            }
        }

        public static Task WaitOneAsync(this WaitHandle @this) => @this.WaitOneAsync(Timeout.Infinite, default);

        public static Task<bool> WaitOneAsync(this WaitHandle @this, int millisecondsTimeout) => @this.WaitOneAsync(millisecondsTimeout, default);

        public static Task<bool> WaitOneAsync(this WaitHandle @this, TimeSpan timeout) => @this.WaitOneAsync((int)timeout.TotalMilliseconds, default);

        public static Task<bool> WaitOneAsync(this WaitHandle @this, TimeSpan timeout, CancellationToken cancellationToken) => @this.WaitOneAsync((int)timeout.TotalMilliseconds, cancellationToken);

        public static Task WaitOneAsync(this WaitHandle @this, CancellationToken cancellationToken) => @this.WaitOneAsync(Timeout.Infinite, cancellationToken);    
    }
}

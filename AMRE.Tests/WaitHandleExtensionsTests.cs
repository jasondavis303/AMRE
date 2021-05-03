using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace AMRE.Tests
{
    [TestClass]
    public class WaitHandleExtensionsTests
    {
        [TestMethod]
        public async Task WaitOneTest()
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();

            ManualResetEvent mre = new ManualResetEvent(false);
            _ = Task.Run(async () =>
              {
                  await Task.Delay(1000);
                  mre.Set();
              });
            await mre.WaitOneAsync().ConfigureAwait(false);
           
            sw.Stop();

            //If this test runs forever, it fails
            //If it finishes instantly, it fails
            //If it takes about 1 second, it passes
            double secs = sw.Elapsed.TotalSeconds;
            Assert.IsTrue(secs > 0.9 && secs < 1.5);
        }

        [TestMethod]
        public async Task TimeoutTest()
        {
            ManualResetEvent mre = new ManualResetEvent(false);
            _ = Task.Run(async () =>
            {
                await Task.Delay(Timeout.Infinite);
                mre.Set();
            });
            bool ranToCompletion = await mre.WaitOneAsync(500).ConfigureAwait(false);
            Assert.IsFalse(ranToCompletion);
        }

        [TestMethod]
        public async Task RanToCompletionTest()
        {
            ManualResetEvent mre = new ManualResetEvent(false);
            _ = Task.Run(() => mre.Set());
            
            bool ranToCompletion = await mre.WaitOneAsync(Timeout.Infinite).ConfigureAwait(false);
            Assert.IsTrue(ranToCompletion);
        }

        [TestMethod]
        public async Task CancellationTest()
        {
            var cts = new CancellationTokenSource();
            ManualResetEvent mre = new ManualResetEvent(false);
            _ = Task.Run(() =>
            {
                cts.Cancel();
                mre.Set();
            });

            bool wasCancelled = false;
            bool ranToCompletion = false;
            try { ranToCompletion = await mre.WaitOneAsync(Timeout.Infinite, cts.Token).ConfigureAwait(false); }
            catch { wasCancelled = true; }

            Assert.IsTrue(wasCancelled);
            Assert.IsFalse(ranToCompletion);
        }


        [TestMethod]
        public async Task ThrowIfMutex()
        {
            Mutex mutex = new Mutex();
            bool objectWasMutex = false;
            try { await mutex.WaitOneAsync(); }
            catch { objectWasMutex = true; }
            Assert.IsTrue(objectWasMutex);        
        }
    }
}

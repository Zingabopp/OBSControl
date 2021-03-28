using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using OBSControl.Utilities;
using System.Threading;
using System.Diagnostics;

namespace OBSControlTests
{
    [TestClass]
    public class AsyncListener_Tests
    {
        [TestMethod]
        public async Task RepeatListen()
        {
            ClassWithEvent testClass = new ClassWithEvent();
            int listenerTimeout = 500;
            int maxAttempts = 5;
            bool successful = false;
            AsyncEventListener<bool, bool> listener = new AsyncEventListener<bool, bool>((s, e) =>
            {
                return new EventListenerResult<bool>(e, e);
            }, listenerTimeout);
            testClass.TestEvent += listener.OnEvent;
            int attempt = 1;
            int raiseEventDelay = (int)(listenerTimeout * (maxAttempts - 0.5f));
            Stopwatch sw = new Stopwatch();
            //listener.sw = sw;
            sw.Start();
            _ = Task.Delay(raiseEventDelay).ContinueWith(t => testClass.RaiseEvent(true, sw.ElapsedMilliseconds)); ;
            do
            {
                Console.WriteLine($"Starting attempt {attempt} @ {sw.ElapsedMilliseconds}ms...");
                try
                {
                    listener.StartListening();
                    Task<bool> t = listener.Task;
                    bool result = await t.ConfigureAwait(false);
                    await Task.Yield();
                    if (result)
                    {
                        successful = true;
                        Console.WriteLine($"Attempt {attempt} successful @ {sw.ElapsedMilliseconds}ms");
                        break;
                    }
                    else
                        Console.WriteLine($"Attempt {attempt} failed @ {sw.ElapsedMilliseconds}ms");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Attempt {attempt} failed @ {sw.ElapsedMilliseconds}ms: {ex.Message}");
                }
                finally
                {
                    listener.Reset();
                }
                attempt++;

            } while (attempt <= maxAttempts);
            Assert.IsTrue(successful);
            Assert.AreEqual(maxAttempts, attempt);
        }

        [TestMethod]
        public async Task CancellationRegistrationTest()
        {
            var tcs = new CancellationTokenSource(100);
            var registration = tcs.Token.Register(() => Console.WriteLine("Registered callback raised."));
            registration.Dispose();
            await Task.Delay(200);
        }
    }

    public class ClassWithEvent
    {
        public event EventHandler<bool>? TestEvent;
        public void RaiseEvent(bool arg, long elapsedMilliseconds)
        {
            TestEvent?.Invoke(this, arg);
            Console.WriteLine($"------TestEvent Raised @ {elapsedMilliseconds}ms------");
        }
    }

}

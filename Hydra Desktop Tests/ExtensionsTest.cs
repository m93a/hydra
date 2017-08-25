using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NUnit.Framework;

//*
using Assert = NUnit.Framework.Assert;
using TimeoutAttribute = NUnit.Framework.TimeoutAttribute;
/*/
using Assert = Microsoft.VisualStudio.TestTools.UnitTesting.Assert;
using TimeoutAttribute = Microsoft.VisualStudio.TestTools.UnitTesting.TimeoutAttribute;
//*/

using HydraExtensions;

/**
 * To execute the tests in VS, just build the Tests Project,
 * otherwise:
 * 
 * Build the project
 * Open the output folder
 * Execute RunTests.bat
 **/


namespace HydraTests
{
    [TestClass, TestFixture]
    public class ExtensionsTest
    {
        /*
         * Fields
         */

        public event EventHandler<int> MyEvent;
        
        /*
         * General methods (init, terminate, clean)
         */
        
        [ClassInitialize]
        public static void Initialize(object a = null) { Initialize(); }

        [OneTimeSetUp]
        public static void Initialize()
        {
            //Initialize
        }


        [ClassCleanup, OneTimeTearDown]
        public static void Terminate()
        {
            //Terminate
        }

        [TestInitialize, TearDown]
        public void Clean()
        {
            //Clean
            MyEvent = null;
        }



        /*
         * The tests
         */

        // if this fails, try again possibly w/ longer time
        [TestMethod, Test, Timeout(1000)]
        public async Task TaskTimeout()
        {
            // Duration 250ms, timeout 500ms ⇒ success
            var tcp = new TaskCompletionSource<bool>();
            var task = tcp.Task.Timeout(500);
            var done = Task.Run(async () => {
                await Task.Delay(250);
                tcp.SetResult(true);
            });

            Assert.IsTrue(await task,"Task should've successfully completed with `true`.");
            await done;


            // Duration 500ms, timeout 250ms ⇒ fail
            tcp = new TaskCompletionSource<bool>();
            task = tcp.Task.Timeout(250);
            done = Task.Run(async () => {
                await Task.Delay(500);
                tcp.SetResult(true);
            });

            var failed = false;

            try { await task; }
            catch(TimeoutException) { failed = true; }

            if (!failed)
                throw new Exception("Task should've timed out.");
            
            await done;
        }


        // if this fails, try again possibly w/ longer time
        [TestMethod, Test, Timeout(750)]
        public async Task EventOnce()
        {
            var task = this.Once("MyEvent");
            var done = Task.Run(async ()=> {
                await Task.Delay(250);
                MyEvent(this, 42);
            });

            var (sender, num) = await task;
            Assert.AreEqual(this, sender);
            Assert.AreEqual(42, num);
            await done;
        }

        [TestMethod, Test, Timeout(500)]
        public async Task ProofOfConcept()
        {
            var e = MyEvent;

            var tcs = new TaskCompletionSource<(object, int)>();
            EventHandler<int> d = null;

            d = delegate (object sender, int args)
            {
                tcs.SetResult((sender, args));
                MyEvent -= d;
            };

            MyEvent += d;
            
            var done = Task.Run(async () => {
                await Task.Delay(250);
                MyEvent(this, 42);
            });

            var (sendeder, num) = await tcs.Task;
            Assert.AreEqual(this, sendeder);
            Assert.AreEqual(42, num);
            await done;
        }
    }
}

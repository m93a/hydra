using System;
using Hydra;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Windows.Forms;
using System.Threading.Tasks;
using NUnit.Framework;


using Assert = NUnit.Framework.Assert;
using System.IO;
using System.Collections.Generic;
//using Assert = Microsoft.VisualStudio.TestTools.UnitTesting.Assert;


/**
 * To execute the tests:
 * Test Explorer > Run All
 * Win+R cmd Enter
 * cd "Hydra Desktop Tests/bin/[x86|x64]/Debug"
 * nunit3-console.exe "Hydra Desktop Tests.dll" --domain=None --inprocess
 **/


namespace HydraTests
{
    //[TestClass, /*TestFixture*/]
    public class GeoGebraTest
    {

        static GeoGebra instance;
        static Task application;
        static TaskCompletionSource<bool> loaded = new TaskCompletionSource<bool>();


        /**
         * Initialize tests: start Windows Forms and wait for
         * GeoGebra to load. This requires a lot of asyncs,
         * awaits and Tasks...
         **/
        
        [ClassInitialize, STAThread]
        public static void Initialize(object a = null) { Initialize(); }
        
        [OneTimeSetUp, STAThread]
        public static void Initialize()
        {
            //Directory.SetCurrentDirectory(AppDomain.CurrentDomain‌​.BaseDirectory);

            //Run Windows Forms asynchronously
            application = Task.Run(()=> {
                Application.Run(new TestForm());
            });

            //Wait for ggb to load
            loaded.Task.Wait();
        }

        //Dummy form
        class TestForm : BlankForm
        {
            public TestForm() : base() {
                Console.WriteLine("Form initialized");
                Load += OnLoad;
            }

            //When dummy form is ready to go
            new protected void OnLoad(object sender, EventArgs e)
            {
                //Start GeoGebra
                instance = new GeoGebra();

                //When it's loaded, tell them know!
                instance.Load += delegate {
                    loaded.SetResult(true);
                };
            }
        }



        /**
         * Tell Windows Forms to shut down and wait untill
         * they finish doing so.
         **/
        [ClassCleanup, OneTimeTearDown]
        public static void Terminate()
        {
            Application.Exit();
            application.Wait();
        }

        [TestInitialize, TearDown]
        public void CleanGeoGebra()
        {
            //TODO
        }




        /*
         * Tests for IPoint
         */


        [TestMethod, Test]
        public async Task PointBasic()
        {
            //Basic functionality

            var pt = await instance.CreatePoint(1, 2, 3);

            Assert.IsTrue(pt is GeoGebra.IObject, "Point implements IObject");
            Assert.IsTrue(pt is GeoGebra.IPoint, "Point implements IPoint");

            Assert.IsTrue(await pt.Exists, "Point exists");

            Assert.IsNotNull(pt.Name, "Point has a name");

            Assert.AreEqual(await pt.X, 1);
            Assert.AreEqual(await pt.Y, 2);
            Assert.AreEqual(await pt.Z, 3);

            Assert.AreEqual(await pt.Coords, new List<double>() { 1, 2, 3 });
        }

        [TestMethod, Test]
        public async Task PointRename()
        {
            //Renaming

            var pt = await instance.CreatePoint(1, 2, 3);

            var name = "František";
            await pt.Rename(name);
            Assert.AreEqual(pt.Name, name);

            Assert.IsTrue(await pt.Exists);
            Assert.AreEqual(await pt.Coords, new List<double>() { 1, 2, 3 });
        }

        [TestMethod, Test]
        public async Task PointDelete()
        {
            //Deleting

            var pt = await instance.CreatePoint(1, 2, 3);

            await pt.Delete();

            Assert.IsNull(pt.Name);
            Assert.IsFalse(await pt.Exists);

        }
    }

    /**
     * TODO 
     * * *
     * IPoint.CopyFreeObject
     * IFreePoint.SetCoords
     **/
}

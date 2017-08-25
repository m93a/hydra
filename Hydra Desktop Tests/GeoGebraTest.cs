using System;
using Hydra;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Windows.Forms;
using System.Threading.Tasks;
using NUnit.Framework;


using System.Collections.Generic;
using CefSharp;

//*
using Assert = NUnit.Framework.Assert;
using TimeoutAttribute = NUnit.Framework.TimeoutAttribute;
/*/
using Assert = Microsoft.VisualStudio.TestTools.UnitTesting.Assert;
using TimeoutAttribute = Microsoft.VisualStudio.TestTools.UnitTesting.TimeoutAttribute;
//*/


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

                //When it's loaded, let us know!
                instance.Loaded += delegate {
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


        [TestMethod, Test, Timeout(2000)]
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

        [TestMethod, Test, Timeout(10000)]
        public async Task PointRename()
        {
            //Basic renaming

            var pt = await instance.CreatePoint(1, 2, 3);

            var name = "František";
            await pt.Rename(name);
            Assert.AreEqual(pt.Name, name);

            Assert.IsTrue(await pt.Exists);
            Assert.AreEqual(await pt.Coords, new List<double>() { 1, 2, 3 });
        }

        [TestMethod, Test, Timeout(10000)]
        public async Task PointNameConflict()
        {
            //Resolving name conflicts

            var name1 = "ABC";
            var pt1 = await instance.CreatePoint(1, 2, 3, name1);

            Assert.AreEqual(pt1.Name, name1);

            var pt2 = await instance.CreatePoint(2, 4, 6, name1);

            Assert.AreEqual(pt2.Name, name1);
            Assert.AreNotEqual(pt1.Name, name1);
            
            Assert.IsTrue(await pt1.Exists);
            Assert.IsTrue(await pt2.Exists);

            Assert.AreEqual(await pt1.Coords, new List<double>() { 1, 2, 3 });
            Assert.AreEqual(await pt2.Coords, new List<double>() { 2, 4, 6 });
        }

        [TestMethod, Test, Timeout(10000)]
        public async Task PointRenameConflict()
        {
            //Resolving rename conflicts

            var name1 = "ABC";
            var pt1 = await instance.CreatePoint(1, 2, 3, name1);
            var pt2 = await instance.CreatePoint(2, 4, 6);

            Assert.AreEqual(pt1.Name, name1);

            await pt2.Rename(name1);

            Assert.AreEqual(pt2.Name, name1);
            Assert.AreNotEqual(pt1.Name, name1);

            Assert.IsTrue(await pt1.Exists);
            Assert.IsTrue(await pt2.Exists);

            Assert.AreEqual(await pt1.Coords, new List<double>() { 1, 2, 3 });
            Assert.AreEqual(await pt2.Coords, new List<double>() { 2, 4, 6 });
        }

        [TestMethod, Test, Timeout(2000)]
        public async Task PointDelete()
        {
            //Deleting

            var pt = await instance.CreatePoint(1, 2, 3);
            var name = pt.Name;

            await pt.Delete();

            Assert.IsNull(pt.Name);
            Assert.IsFalse(await pt.Exists, "IObject API says obj is deleted");
            Assert.IsFalse(await instance.Exists(name), "Object is really deleted from GeoGebra");
        }
    }

    /**
     * TODO 
     * * *
     * IPoint.CopyFreeObject
     * IFreePoint.SetCoords
     * test undefined object
     * make sure to check (x,y), (x,y,z) and (list) everywhere
     * make sure nothing can be called on a deleted object
     * make sure objects get garbage-collected
     * 
     * IRestrictedPoint
     * IPointOnPath
     * IPath
     * ILine
     * IVector
     **/
}

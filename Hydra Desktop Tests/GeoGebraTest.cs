using System;
using Hydra;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Windows.Forms;
using System.Threading.Tasks;

namespace HydraTests
{
    [TestClass]
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
        public static void Initialize(TestContext fortyTwo)
        {
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
        [ClassCleanup]
        public static void Terminate()
        {
            Application.Exit();
            application.Wait();
        }

        //[TestInitialize]
        public void CleanGeoGebra()
        {

        }

        [TestMethod]
        public async Task Point()
        {
            //Basic functionality

            var pt = await instance.CreatePoint(1, 2, 3);

            Assert.IsTrue(pt is GeoGebra.IObject, "Point implements IObject");
            Assert.IsTrue(pt is GeoGebra.IPoint, "Point implements IPoint");

            Assert.IsTrue(await pt.Exists, "Point exists");

            Assert.IsNotNull(pt.Name, "Point has a name");

            Assert.Equals(await pt.X, 1);
            Assert.Equals(await pt.Y, 2);
            Assert.Equals(await pt.Z, 3);

            Assert.Equals(await pt.Coords, Tuple.Create(1d,2d,3d));


            //Renaming

            var name = "František";
            await pt.Rename(name);
            Assert.Equals(pt.Name, name);

            Assert.IsTrue(await pt.Exists);
            Assert.Equals(await pt.Coords, Tuple.Create(1d, 2d, 3d));


            //Deleting

            await pt.Delete();

            Assert.IsNull(pt.Name);
            Assert.IsFalse(await pt.Exists);

        }
    }
}

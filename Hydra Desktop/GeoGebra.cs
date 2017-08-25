using CefSharp;
using CefSharp.MinimalExample.WinForms;
using CefSharp.WinForms;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using HydraExtensions;

/**
 * This is the main GeoGebra library-thing.
 * TODO Explain how it works.
 **/


namespace Hydra
{
    public partial class GeoGebra
    {

        BrowserForm browserForm;
        ChromiumWebBrowser browser;

        Dictionary<string, WeakReference<Object>> objects;


        ///<summary>This event is called only once when GeoGebra is initialized.</summary>
        public event EventHandler Loaded;


        ///<summary>This event is called every time an object is renamed.</summary>
        public event EventHandler<RenamedEventArgs> Renamed;

        /// <summary>Important fields are (the new) Name and OldName</summary>
        public class RenamedEventArgs : EventArgs
        {
            public string Name;
            public string OldName;
            public IObject Target;
            public RenamedEventArgs(string A, string B, IObject obj)
            {
                Name = A;
                OldName = B;
                Target = obj;
            }
        }


        /**
         * <summary>Returns the main frame of the CEF browser,
         * where GeoGebra lives. Don't use this unless you need
         * to perform some dirty hacks.</summary>
         **/
        public IFrame MainFrame
        {
            get {
                try { return browser.GetMainFrame(); }
                catch { return null; }
            }
        }



        /**
         * Open GeoGebra in Cef and create bindings
         * <summary>This is the main class of the GeoGebra library.
         * You need to create an instance to use it. When you call
         * this instructor, a new window appears with GeoGebra inside.</summary>
         **/
        public GeoGebra()
        {
            //For Windows 7 and above, best to include relevant app.manifest entries as well
            Cef.EnableHighDPISupport();

            var settings = new CefSettings()
            {
                //By default CefSharp will use an in-memory cache, you need to specify a Cache Folder to persist data
                CachePath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CefSharp\\Cache")
            };

            //Perform dependency check to make sure all relevant resources are in our output directory.
            Cef.Initialize(settings, performDependencyCheck: true, browserProcessHandler: null);

            //Initialize the browser
            var url = System.IO.Path.Combine(Environment.CurrentDirectory, @"GeoGebra\index.html");
            browserForm = new BrowserForm(url);
            browser = browserForm.browser;

            //Expose C# methods to JavaScript
            browser.RegisterAsyncJsObject("hydra", new ExposedMethods(this));

            //Listen for the load event
            browser.FrameLoadStart += delegate(object sender, FrameLoadStartEventArgs args)
            {
                var frame = args.Frame;
                if (frame.IsMain)
                {
                    //Listen for GeoGebra's load event
                    //Which actually doesn't fire for some reason
                    //So we need to hack around a little
                    frame.ExecuteJavaScriptAsync(@"
                        (function repeat(){
                            if(window.ggbApplet && ggbApplet.evalCommand) {
                                
                                window.hydra.loaded();
                                
                            } else {
                                requestAnimationFrame(repeat);
                            }
                        })();
                    ");
                }
            };

            //Open the window
            browserForm.Show();

            //Initialize the list of objects
            objects = new Dictionary<string, WeakReference<Object>>();
        }
        



        /**
         * This object is exposed to the JavaScript code
         **/
        class ExposedMethods
        {
            //Store reference to the parent object
            GeoGebra self;
            public ExposedMethods(GeoGebra instance)
            {
                self = instance;
            }


            //Now you can safely execute code!
            public async void Loaded()
            {
                Console.WriteLine("GeoGebra Fully Loaded!");

                //Dispatch event listeners
                await self.MainFrame.EvaluateScriptAsync(@"
                    window.renameListener = function(A,B){
                        hydra.renamed(""""+A,""""+B);
                    };
                    
                    ggbApplet.registerRenameListener(""renameListener"")
                ");

                //Let everybody know the party begins!
                self.Loaded(self, new EventArgs());
            }


            //Hook for the rename event
            public void Renamed(string A, string B)
            {
                if (!self.objects.ContainsKey(A)) return;

                Object obj;
                if (!self.objects[A].TryGetTarget(out obj)) return;

                var args = new RenamedEventArgs(A, B, obj);

                obj.OnRenamed(self, args);

                if (self.Renamed == null) return;
                self.Renamed(self, args);
            }
        }


        
        
        
        
        
        


        /**
         * <summary>Checks whether object with given name exists in GeoGebra</summary>
         **/
        public async Task<bool> Exists(string name)
        {
            if (name == null) return false;

            var command = "ggbApplet.exists(\"" + name + "\")";
            var result = this.MainFrame.EvaluateScriptAsync(command);
            
            var exists = (bool)(await result).Result;
            if (!exists) { name = null; }
            return exists;
        }


        /**
         * <summary>Create new free point with the given coordinates.</summary>
         * <see cref="CreatePoint(double, double, double)"/>
         **/

        public Task<IFreePoint> CreatePoint(List<double> coords, string name = null)
        {
            var c = coords;

            switch (c.Count)
            {
                case 2:
                    return CreatePoint(c[0], c[1], name);

                case 3:
                    return CreatePoint(c[0], c[1], c[2], name);

                default:
                    throw new ArgumentException("Wrong number of coordinates!");
            }
        }


        /**
         * <summary>Create new free point with the given coordinates.</summary>
         **/
        
        public async Task<IFreePoint> CreatePoint(double x, double y, double z=0)
        {
            var command = string.Format("({0},{1},{2})",x,y,z);

            command = string.Format(@"
                ggbApplet.evalCommandGetLabels(""{0}"");
            ",command);
            
            var name = (string)(await MainFrame.EvaluateScriptAsync(command)).Result;

            var result = new FreePoint(this, name);
            objects.Add(name, new WeakReference<Object>(result));

            return result;
        }

        /**
         * <summary>Create new free point with the given coordinates.</summary>
         * <see cref="CreatePoint(double, double, double)"/>
         **/
        public async Task<IFreePoint> CreatePoint(double x, double y, double z, string name)
        {
            // this is because of name conflicts
            // otherwise, the create function would just override
            // the existing point/line/whatever with that name
            var pt = await CreatePoint(x, y, z);
            await pt.Rename(name);
            return pt;
        }

        /**
         * <summary>Create new free point with the given coordinates.</summary>
         * <see cref="CreatePoint(double, double, double)"/>
         **/
        public Task<IFreePoint> CreatePoint(double x, double y, string name)
        {
            return CreatePoint(x, y, 0, name);
        }
    }
}

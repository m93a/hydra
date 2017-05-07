using CefSharp;
using CefSharp.MinimalExample.WinForms;
using CefSharp.WinForms;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

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

        IFrame mainFrame
        {
            get {
                try { return browser.GetMainFrame(); }
                catch { return null; }
            }
        }


        /**
         * Open GeoGebra in Cef and create bindings 
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
            public void Loaded()
            {
                Console.WriteLine("GeoGebra Fully Loaded!");
                self.Load(self, new EventArgs());
            }
        }





        /**
         * GeoGebra.Load event
         **/
        public event EventHandler Load;
        
        
        
        
        


        /**
         * <summary>Create new free point with the given coordinates.</summary>
         **/
        
        public Task<IFreePoint> CreatePoint(List<double> coords, string name = null)
        {
            var c = coords;

            switch (c.Count)
            {
                case 2:
                    return CreatePoint(c[0], c[1], 0, name);

                case 3:
                    return CreatePoint(c[0], c[1], c[2], name);

                default:
                    throw new ArgumentException("Wrong number of coordinates!");
            }
        }


        /**
         * <summary>Create new free point with the given coordinates.</summary>
         **/
        
        public async Task<IFreePoint> CreatePoint(double x, double y, double z=0, string name=null)
        {
            var command = name==null ? "" : name+"=";
            command += string.Format("({0},{1},{2})",x,y,z);

            command = @"
                ggbApplet.evalCommandGetLabels("""+command+@""");
            ";
            
            name = (string)(await mainFrame.EvaluateScriptAsync(command)).Result;

            var result = new FreePoint(this, name);

            return result;
        }
    }
}

using CefSharp;
using CefSharp.MinimalExample.WinForms;
using CefSharp.WinForms;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Security.Cryptography.X509Certificates;

namespace Hydra
{
    public class GeoGebra
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
                CachePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CefSharp\\Cache")
            };

            //Perform dependency check to make sure all relevant resources are in our output directory.
            Cef.Initialize(settings, performDependencyCheck: true, browserProcessHandler: null);

            //Initialize the browser
            var url = Path.Combine(Environment.CurrentDirectory, @"GeoGebra\index.html");
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
         * General GeoGebra.Object class
         **/
        
        public interface IObject
        {
            string Name { get; }
            Task<bool> Exists { get; }
            Task Rename(string name);
            Task Delete();
        }

        class Object : IObject
        {
            protected GeoGebra self;
            public string _name;
            public string Name
            {
                get { return _name; }
            }

            public Object(GeoGebra instance, string name = null)
            {
                self = instance;
                _name = name;
            }


            public Task<bool> Exists
            {
                get
                {
                    if(_name == null)
                        return Task.Run(()=>false);

                    var command = "ggbApplet.exists(\"" + _name + "\")";
                    var result = self.mainFrame.EvaluateScriptAsync(command);
                    return Task.Run<bool>(async () => {
                        var exists = (bool)(await result).Result;
                        if (!exists) { _name = null; }
                        return exists;
                    });
                }
            }


            public async Task Rename(string name)
            {
                if (_name == null)
                    throw new ObjectDisposedException("This object no longer exists.");

                var command = string.Format("ggbApplet.renameObject({0},{1})",_name,name);
                var result = self.mainFrame.EvaluateScriptAsync(command);
                if (!(bool)(await result).Result)
                    throw new Exception("There was an error while renaming "+_name+" to "+name+".");
            }


            public async Task Delete()
            {
                if (_name == null) { return; };

                var command = string.Format("ggbApplet.deleteObject({0})",_name);
                await self.mainFrame.EvaluateScriptAsync(command);
            }
        }





        /**
         * GeoGebra.Point
         **/
        
        public interface IPoint : IObject
        {
            Task<double> X { get; }
            Task<double> Y { get; }
            Task<double> Z { get; }

            Task<Tuple<double, double, double>> Coords { get; }
        }

        class Point : Object, IPoint
        {
            public Point(GeoGebra instance, string name = null) : base(instance, name) { }
            

            async Task<double> getCoord(string c)
            {
                if (_name == null)
                    throw new ObjectDisposedException("This object no longer exists.");

                var command = string.Format(@"ggbApplet.get{0}coord(""{1}"")", c, _name);
                var result = self.mainFrame.EvaluateScriptAsync(command);
                return (double)(await result).Result;
            }

            public Task<double> X
            {
                get
                {
                    return Task<double>.Run(async () =>
                    {
                        return await getCoord("X");
                    });
                }
            }

            public Task<double> Y
            {
                get
                {
                    return Task<double>.Run(async () =>
                    {
                        return await getCoord("Y");
                    });
                }
            }

            public Task<double> Z
            {
                get
                {
                    return Task<double>.Run(async () =>
                    {
                        return await getCoord("Z");
                    });
                }
            }

            public Task<Tuple<double,double,double>> Coords
            {
                get
                {
                    return Task<Tuple<double, double, double>>.Run(async () =>
                    {
                        if (_name == null)
                            throw new ObjectDisposedException("This object no longer exists.");

                        var command = string.Format(@"
                        [
                            ggbApplet.getXcoord(""{1}""),
                            ggbApplet.getYcoord(""{1}""),
                            ggbApplet.getZcoord(""{1}""),
                        ]
                        ", _name);

                        var task = self.mainFrame.EvaluateScriptAsync(command);
                        var result = (object[])(await task).Result;
                        return Tuple.Create(
                            (double)result[0],
                            (double)result[1],
                            (double)result[2]
                        );
                    });
                }
            }
        }

        

        public async Task<IPoint> CreatePoint(double x, double y, double z, string name=null)
        {
            var command = name==null ? "" : name+"=";
            command += string.Format("({0},{1},{2})",x,y,z);

            command = @"
                ggbApplet.evalCommandGetLabels("""+command+@""");
            ";
            
            name = (string)(await mainFrame.EvaluateScriptAsync(command)).Result;

            var result = new Point(this);
            result._name = name;

            return result;
        }
    }
}

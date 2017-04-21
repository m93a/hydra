// Copyright © 2010-2015 The CefSharp Authors. All rights reserved.
//
// Use of this source code is governed by a BSD-style license that can be found in the LICENSE file.

using CefSharp;
using CefSharp.MinimalExample.WinForms;
using CefSharp.WinForms;
using System;
using System.IO;
using System.Windows.Forms;

namespace Hydra
{
    public class Program
    {
        [STAThread]
        public static void Main()
        {
            //Launch GeoGebra in Chromium
            var path = Path.Combine(Environment.CurrentDirectory, @"GeoGebra\index.html");
            var browserForm = InitCef(path);
            var browser = browserForm.browser;

            //When Ggb loads
            browser.LoadingStateChanged += delegate (object sender, LoadingStateChangedEventArgs args)
            {
                Console.WriteLine("GeoGebra Loaded");
                Console.WriteLine(args.IsLoading);
            };

            //Show the form
            Application.Run(browserForm);
        }

        
        
        /**
         * Initialize Chromium browser
         **/
        static BrowserForm InitCef(string url)
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

            var browser = new BrowserForm(url);
            return browser;
        }
    }
}

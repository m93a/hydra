using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hydra
{
    public partial class GeoGebra
    {
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

            public Object(GeoGebra instance, string name)
            {
                self = instance;
                _name = name;
            }


            public Task<bool> Exists
            {
                get
                {
                    if (_name == null)
                        return Task.Run(() => false);

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

                var command = string.Format(@"ggbApplet.renameObject(""{0}"",""{1}"")", _name, name);
                var result = self.mainFrame.EvaluateScriptAsync(command);
                if (!(bool)(await result).Result)
                    throw new Exception("There was an error while renaming " + _name + " to " + name + ".");

                _name = name;
            }


            public async Task Delete()
            {
                if (_name == null) { return; };

                var command = string.Format(@"ggbApplet.deleteObject(""{0}"")", _name);
                await self.mainFrame.EvaluateScriptAsync(command);

                if (await Exists)
                    throw new Exception("Could not delete the object.");

                _name = null;
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HydraExtensions;

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

            event EventHandler<RenamedEventArgs> Renamed;
        }

        class Object : IObject
        {
            protected GeoGebra self;
            public string Name;
            string IObject.Name
            {
                get { return Name; }
            }

            public Object(GeoGebra instance, string name)
            {
                self = instance;
                Name = name;
            }

            ~Object()
            {
                // Will this hurt, mommy?
                // var t = Delete();

                // tl;dr Yes it will.
                // First we need to make sure not to GC the objects
                // that are needed by other dependent ones.
            }


            public Task<bool> Exists
            {
                get
                {
                    return Task.Run<bool>(async () => {
                        return await self.Exists(Name);
                    });
                }
            }


            public async Task Rename(string name)
            {
                var locktask = this.Once("Renamed");

                if (Name == null)
                    throw new ObjectDisposedException("This object no longer exists.");

                var command = string.Format(@"ggbApplet.renameObject(""{0}"",""{1}"")", Name, name);
                var result = self.MainFrame.EvaluateScriptAsync(command);
                if (!(bool)(await result).Result)
                    throw new Exception("There was an error while renaming " + Name + " to " + name + ".");

                await locktask;
            }


            public async Task Delete()
            {
                if (Name == null) { return; };

                var command = string.Format(@"ggbApplet.deleteObject(""{0}"")", Name);
                await self.MainFrame.EvaluateScriptAsync(command);

                if (await Exists)
                    throw new Exception("Could not delete the object.");

                Name = null;
            }


            public event EventHandler<RenamedEventArgs> Renamed;
            public void OnRenamed(object sender, RenamedEventArgs args)
            {
                self.objects.Remove(Name);
                self.objects.Add(args.Name, new WeakReference<Object>(this));

                Name = args.Name;

                if (Renamed == null) return;
                Renamed(sender, args);
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hydra
{
    public partial class GeoGebra
    {
        class FreePoint : Point, IFreePoint
        {
            public FreePoint(GeoGebra instance, string name) : base(instance, name) { }

            public Task SetCoords(List<double> c)
            {
                switch (c.Count)
                {
                    case 2:
                        return SetCoords(c[0], c[1]);

                    case 3:
                        return SetCoords(c[0], c[1], c[2]);

                    default:
                        throw new ArgumentException("Wrong number of coordinates!");
                }
            }

            public Task SetCoords(double x, double y)
            {
                return SetCoords(x, y, 0);
            }

            public async Task SetCoords(double x, double y, double z)
            {
                if (_name == null)
                    throw new ObjectDisposedException("This object no longer exists.");

                var command = string.Format(@"
                    ggbApplet.setCoords(""{0}"",{1},{2},{3}),
                ", _name, x, y, z);

                await self.mainFrame.EvaluateScriptAsync(command);
            }
        }
    }
}

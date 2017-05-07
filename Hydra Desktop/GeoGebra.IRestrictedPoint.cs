using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Hydra
{
    public partial class GeoGebra
    {
        public interface IRestrictedPoint : IPoint
        {
            Task SetCoordsNear(IPoint point);
            Task SetCoordsNear(List<double> coords);
            Task SetCoordsNear(double x, double y, double z = 0);
        }

        class RestrictedPoint : Point, IRestrictedPoint
        {
            public RestrictedPoint(GeoGebra instance, string name) : base(instance, name) {
                if(!(this is PointOnPath || this is PointInRegion))
                {
                    throw new InvalidOperationException("RestrictedPoint has to be initialized using"+
                        "either PointOnLine or PointInRegion, do not use RestrictedPoint as a constructor!");
                }
            }

            public Task SetCoordsNear(List<double> c)
            {
                return SetCoords(c);
            }

            public Task SetCoordsNear(double x, double y, double z = 0)
            {
                return SetCoords(x, y, z);
            }

            public async Task SetCoordsNear(IPoint point)
            {
                await SetCoords(await point.Coords);
            }
        }
    }
}

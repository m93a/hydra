using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Hydra
{
    public partial class GeoGebra
    {
        
        public interface IFreePoint : IPoint
        {
            Task SetCoords(List<double> c);
            Task SetCoords(double x, double y, double z = 0);
        }

        class FreePoint : Point, IFreePoint
        {
            public FreePoint(GeoGebra instance, string name) : base(instance, name) { }
            
            //SetCoords defined in Point class
        }
    }
}

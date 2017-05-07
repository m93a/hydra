using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hydra
{
    public partial class GeoGebra
    {
        public interface IPointInRegion : IRestrictedPoint
        {
            Task<IRegion> Region { get; }
        }

        class PointInRegion : RestrictedPoint, IPointInRegion
        {
            public PointInRegion(GeoGebra instance, string name) : base(instance, name) { }

            public Task<IRegion> Region
            {
                get
                {
                    throw new NotImplementedException();
                }
            }
        }
    }
}

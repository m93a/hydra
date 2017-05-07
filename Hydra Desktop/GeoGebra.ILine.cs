using System;
using System.Threading.Tasks;

namespace Hydra
{
    public partial class GeoGebra
    {
        public interface ILine : IPath
        {
            Task<ILine> CreateParallel(IPoint through);
            Task<ILine> CreatePerpendicular(IPoint through);
        }

        class Line : Path, ILine
        {
            public Line(GeoGebra instance, string name) : base(instance, name) { }


            public async override Task<ILine> CreatePerpendicular(double parameter)
            {
                var pt = await CreatePoint(parameter);
                return await CreatePerpendicular(pt);
            }

            public override Task<ILine> CreatePerpendicular(IPointOnPath there)
            {
                return CreatePerpendicular((IPoint)there);
            }

            public Task<ILine> CreatePerpendicular(IPoint through)
            {
                throw new NotImplementedException();
            }

            public Task<ILine> CreateParallel(IPoint through)
            {
                throw new NotImplementedException();
            }
        }
    }
}

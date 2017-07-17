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

            public async Task<ILine> CreatePerpendicular(IPoint through)
            {
                if (Name == null)
                    throw new ObjectDisposedException("This object no longer exists.");

                var command = string.Format(@"
                    PerpendicularLine[{0},{1}]
                ", through.Name, Name);

                command = string.Format(@"
                    ggbApplet.evalCommandGetLabels(""{0}"");
                ", command);

                var task = self.MainFrame.EvaluateScriptAsync(command);
                var name = (string)(await task).Result;

                return new Line(self, name);
            }



            public async Task<ILine> CreateParallel(IPoint through)
            {
                if (Name == null)
                    throw new ObjectDisposedException("This object no longer exists.");

                var command = string.Format(@"
                    Line[{0},{1}]
                ", through.Name, Name);

                command = string.Format(@"
                    ggbApplet.evalCommandGetLabels(""{0}"");
                ", command);

                var task = self.MainFrame.EvaluateScriptAsync(command);
                var name = (string)(await task).Result;

                return new Line(self, name);
            }
        }
    }
}

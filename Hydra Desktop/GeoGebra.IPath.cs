using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Hydra
{
    public partial class GeoGebra
    {
        public interface IPath : IObject
        {
            Task<IPointOnPath> CreatePoint(double? parameter = null);

            Task<IPointOnPath> CreatePointNear(IPoint point);
            Task<IPointOnPath> CreatePointNear(List<double> coords);
            Task<IPointOnPath> CreatePointNear(double x, double y, double z=0);

            Task<ILine> CreatePerpendicular(IPointOnPath there);
            Task<ILine> CreatePerpendicular(double parameter);

            Task<IVector> Normal(IPointOnPath there);
            Task<IVector> Normal(double parameter);

            Task<List<IPoint>> Intersect(IPath path);
        }

        class Path : Object, IPath
        {
            public Path(GeoGebra instance, string name) : base(instance, name) { }


            public async Task<IPointOnPath> CreatePoint(double? parameter = null)
            {
                var command = string.Format(@"
                    Point[{0}{1}]
                ", Name, (parameter == null) ? "" : (", "+parameter) );

                command = string.Format(@"
                    ggbApplet.evalCommandGetLabels(""{0}"");
                ",command);

                var name = (string)(await self.MainFrame.EvaluateScriptAsync(command)).Result;

                var result = new PointOnPath(self, name);

                return result;
            }




            public Task<IPointOnPath> CreatePointNear(List<double> c)
            {
                switch (c.Count)
                {
                    case 2:
                        return CreatePointNear(c[0], c[1]);

                    case 3:
                        return CreatePointNear(c[0], c[1], c[2]);

                    default:
                        throw new ArgumentException("Wrong number of coordinates!");
                }
            }

            public async Task<IPointOnPath> CreatePointNear(double x, double y, double z = 0)
            {
                var pt = await self.CreatePoint(x, y, z);
                return await CreatePointNear(pt);
            }

            public async Task<IPointOnPath> CreatePointNear(IPoint point)
            {
                var command = string.Format(@"
                    ClosestPoint[{0},{1}]
                ", Name, point.Name);

                command = string.Format(@"
                    ggbApplet.evalCommandGetLabels(""{0}"");
                ", command);

                var name = (string)(await self.MainFrame.EvaluateScriptAsync(command)).Result;

                var result = new PointOnPath(self, name);

                return result;
            }




            public async Task<List<IPoint>> Intersect(IPath path)
            {
                if (Name == null)
                    throw new ObjectDisposedException("This object no longer exists.");

                var command = string.Format(@"
                    Intersect[{0},{1}]
                ", path.Name, Name);

                command = string.Format(@"
                    Array(ggbApplet.evalCommandGetLabels(""{0}""));
                ", command);

                var task = self.MainFrame.EvaluateScriptAsync(command);
                var result = (List<object>)(await task).Result;

                //Convert from any boxed type to string
                var names =  result.ConvertAll(item => (string)Convert.ChangeType(item, typeof(string)));

                //Create IPoints from names
                var points = new List<IPoint>();
                foreach(var n in names)
                {
                    points.Add( new Point(self, n) );
                }

                return points;
            }

            public Task<IVector> Normal(double parameter)
            {
                throw new NotImplementedException();
            }

            public Task<IVector> Normal(IPointOnPath there)
            {
                throw new NotImplementedException();
            }

            public virtual Task<ILine> CreatePerpendicular(double parameter)
            {
                throw new NotImplementedException();
            }

            public virtual Task<ILine> CreatePerpendicular(IPointOnPath there)
            {
                throw new NotImplementedException();
            }
        }
    }
}

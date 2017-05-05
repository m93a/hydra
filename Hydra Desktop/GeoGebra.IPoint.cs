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
         * GeoGebra.Point
         **/
        
        public interface IPoint : IObject
        {
            Task<double> X { get; }
            Task<double> Y { get; }
            Task<double> Z { get; }

            Task<List<double>> Coords { get; }

            Task<IFreePoint> CopyFreeObject();
        }

        public interface IFreePoint : IPoint
        {
            Task SetCoords(List<double> c);
            Task SetCoords(double x, double y);
            Task SetCoords(double x, double y, double z);
        }

        class Point : Object, IPoint
        {
            public Point(GeoGebra instance, string name) : base(instance, name) { }
            

            //To get the X coord, you call getCoord("x")
            async Task<double> getCoord(string c)
            {
                if (_name == null)
                    throw new ObjectDisposedException("This object no longer exists.");

                var command = string.Format(@"ggbApplet.get{0}coord(""{1}"")", c, _name);
                var result = self.mainFrame.EvaluateScriptAsync(command);

                //Convert from any boxed type to double
                return (double)Convert.ChangeType(
                    (await result).Result, typeof(double)
                );
            }

            public Task<double> X
            {
                get
                {
                    return Task<double>.Run(async () =>
                    {
                        return await getCoord("X");
                    });
                }
            }

            public Task<double> Y
            {
                get
                {
                    return Task<double>.Run(async () =>
                    {
                        return await getCoord("Y");
                    });
                }
            }

            public Task<double> Z
            {
                get
                {
                    return Task<double>.Run(async () =>
                    {
                        return await getCoord("Z");
                    });
                }
            }

            public Task<List<double>> Coords
            {
                get
                {
                    return Task.Run(async () =>
                    {
                        if (_name == null)
                            throw new ObjectDisposedException("This object no longer exists.");

                        var command = string.Format(@"
                        [
                            ggbApplet.getXcoord(""{0}""),
                            ggbApplet.getYcoord(""{0}""),
                            ggbApplet.getZcoord(""{0}""),
                        ]
                        ", _name);

                        var task = self.mainFrame.EvaluateScriptAsync(command);
                        var result = (List<object>)(await task).Result;

                        //Convert from any boxed type to double
                        return result.ConvertAll(item => (double)Convert.ChangeType(item, typeof(double)));

                    });
                }
            }

            
            public async Task<IFreePoint> CopyFreeObject()
            {
                if (_name == null)
                    throw new ObjectDisposedException("This object no longer exists.");

                var command = string.Format(@"
                    CopyFreeObject[{0}]
                ", _name);

                command = string.Format(@"
                    ggbApplet.evalCommandGetLabels(""{0}"");
                ",command);

                var task = self.mainFrame.EvaluateScriptAsync(command);
                var name = (string)(await task).Result;
                
                return new FreePoint(self, name);
            }
        }
    }
}

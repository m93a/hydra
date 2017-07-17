using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hydra
{
    public partial class GeoGebra
    {
        public interface IPointOnPath : IRestrictedPoint
        {
            Task<double> Parameter { get; }
            Task<IPath> Path { get; }
        }

        class PointOnPath : RestrictedPoint, IPointOnPath
        {
            public PointOnPath(GeoGebra instance, string name) : base(instance, name) { }

            public Task<double> Parameter
            {
                get
                {
                    return Task.Run(async () =>
                    {
                        if (Name == null)
                            throw new ObjectDisposedException("This object no longer exists.");

                        var command = string.Format(@"
                            PathParameter[{0}]
                        ", Name);

                        command = string.Format(@"
                            (function(){
                                var name = ggbApplet.evalCommandGetLabels(""{0}"");
                                var value = ggbApplet.getValue(name);
                                ggbApplet.deleteObject(name);
                                return +value;
                            })();
                        ", command);

                        var result = self.MainFrame.EvaluateScriptAsync(command);

                        //Convert from any boxed type to double
                        return (double)Convert.ChangeType(
                            (await result).Result, typeof(double)
                        );
                    });
                }
            }

            public Task<IPath> Path
            {
                get
                {
                    throw new NotImplementedException();
                }
            }
        }
    }
}

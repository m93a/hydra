using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HydraExtensions
{
    public static class TypeExtensions
    {
        /**
         * <summary>
         *     Checks whether the type is a subtype of the generic type T.
         * </summary>
         * <example>
         *     var foo = new List&lt;int>();
         *     foo.Type.IsSubtypeOfGeneric(typeof(List&lt;>) == true;
         * </example>
         **/
        public static bool IsSubclassOfGeneric(this Type T, Type generic)
        {
            // source: https://stackoverflow.com/a/457708/1137334

            while (T != null && T != typeof(object))
            {
                var foo = T.IsGenericType ? T.GetGenericTypeDefinition() : T;
                if (generic == foo)
                {
                    return true;
                }
                T = T.BaseType;
            }
            return false;
        }
    }
}

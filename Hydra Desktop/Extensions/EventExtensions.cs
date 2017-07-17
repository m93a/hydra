using System;
using System.Threading.Tasks;

namespace HydraExtensions
{
    /**
     * <summary>
     *     This class contains extensions to event-related
     *     types, such as EventHandler and EventHandler&lt;T>.
     * </summary>
     **/
    public static class EventExtensions
    {
        /**
         * <summary>
         *     Given an EventHandler, returns a Task that will
         *     be completed when the event fires.
         * </summary>
         * 
         * <returns>
         *     A task which will complete when the event fires.
         *     The task.Result is a ValueTuple containing the
         *     two arguments that would be passed to an event
         *     handler (i.e. sender and args).
         * </returns>
         * 
         * <example>
         *     var form = new MyForm();
         *     await form.Load.Once(); //once the form loads
         *     
         *     var (sender, args) = await form.Button.Click.Once();
         *     Console.WriteLine("The button was just clicked");
         * </example>
         **/
        public static Task<(object sender, T args)> Once<T>(this EventHandler<T> e)
        {
            var tcs = new TaskCompletionSource<(object, T)>();
            EventHandler<T> d = null;

            d = delegate (object sender, T args)
            {
                tcs.SetResult( (sender, args) );
                e -= d;
            };

            e += d;
            return tcs.Task;
        }

        /// <see cref="Once{T}(EventHandler{T})"/>
        public static Task Once(this EventHandler nonGeneric)
        {
            EventHandler<EventArgs> generic = (s, a) => nonGeneric(s, a);
            return Once(generic);
        }

    }
}

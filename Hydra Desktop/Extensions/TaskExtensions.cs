using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace HydraExtensions
{
    /**
     * <summary>This class contains extension methods for Task.</summary>
     **/
    public static class TaskExtensions
    {
        /**
         * 
         * <summary>
         *     Sets a timeout to the task. If the timeout exceeds,
         *     a TimeoutException is thrown.
         * </summary>
         * 
         * <example>
         * var task = new Task(func);
         * try
         * {
         *     var result = task.Timeout(5000);
         *     Console.WriteLine(result);
         * }
         * catch(e)
         * {
         *     Console.WriteLine("Timeout exceeded");
         * }
         * </example>
         * 
         * 
         * <param name="task">
         *     The task you want to delay.
         * </param>
         * <param name="delay">
         *     Timeout as a TimeSpan or as an integer in milliseconds.
         * </param>
         * 
         * <returns>
         *     A new task that is equivalent to the former one,
         *     except for the fact that this one will eventually exceed.
         * </returns>
         * 
         **/
        public static async Task Timeout(this Task task, TimeSpan delay)
        {
            var canceller = new CancellationTokenSource();
            var timeout = Task.Delay(delay, canceller.Token);
            var first = await Task.WhenAny(task, timeout);

            if (first == task)
            {
                canceller.Cancel();
                await task;
                return;
            }
            else
            {
                throw new TimeoutException("The operation exceeded the specified time.");
            }
        }

        /// <see cref="Timeout(Task, TimeSpan)"/>
        public static Task Timeout(this Task task, int delay)
        {
            return Timeout(task, TimeSpan.FromMilliseconds(delay));
        }


        /// <see cref="Timeout(Task, TimeSpan)"/>
        public static async Task<T> Timeout<T>(this Task<T> task, TimeSpan delay)
        {
            await Timeout((Task)task, delay);
            return await task;
        }

        /// <see cref="Timeout(Task, TimeSpan)"/>
        public static Task<T> Timeout<T>(this Task<T> task, int delay)
        {
            return Timeout(task, TimeSpan.FromMilliseconds(delay));
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
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
        // Caching mechanism for the Once method
        static Dictionary<Type, DynamicMethod> dynamicHandlerCache = new Dictionary<Type, DynamicMethod>();

        /**
         * <summary>
         *     The return type of <ref>Once</ref>, contains some useful casts.
         * </summary>
         **/
        
        public class EventResult
        {
            object[] arguments;
            Type type;

            public EventResult(object[] args, Type T)
            {
                arguments = args;
                type = T;
            }

            public void Deconstruct(out object sender, out object args)
            {
                if (type.IsSubclassOfGeneric(typeof(EventHandler<>)))
                {
                    sender = arguments[0];
                    args = arguments[1];
                }
                else
                    throw new InvalidCastException("Only EventHandler<T> can be cast to a tuple, not "+type);
            }

            public static implicit operator (object sender, object args)(EventResult e)
            {
                var (s, a) = e;
                return (s, a);
            }

            public static implicit operator object[](EventResult e)
            {
                return e.arguments;
            }

            public object sender { get => arguments[0]; }
        }


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
         *     await form.Once("Load"); //once the form loads
         *     
         *     var (sender, args) = await form.Button.Once("Click");
         *     Console.WriteLine("The button was just clicked");
         * </example>
         **/
        public static async
            Task<EventResult>
            BadOnce<T>(this T target, string eventName)
            where T : class
        {
            // Unfortunately this won't do without reflections and IL generators :'(
            // Inspired by https://stackoverflow.com/questions/12865848/general-purpose-fromevent-method
            
            var tcs = new TaskCompletionSource<object[]>();

            // Get the event
            var eventInfo = typeof(T).GetEvent(eventName,
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);

            if (eventInfo == null)
                throw new NullReferenceException("The object doesn't contain any event named " + eventName);

            var delegateType = eventInfo.EventHandlerType;

            // Check for the event handler in the cache
            DynamicMethod handler;
            if (dynamicHandlerCache.ContainsKey(delegateType))
            {
                // Handler exists in the cache
                handler = dynamicHandlerCache[delegateType];
            }
            else lock (dynamicHandlerCache)
            {
                if (dynamicHandlerCache.ContainsKey(delegateType))
                {
                    // Handler was just created
                    handler = dynamicHandlerCache[delegateType];
                }
                else
                {
                    // No handler in the cache, we need to create it

                    // Get event handler type info
                    var invoke = eventInfo.EventHandlerType.GetMethod("Invoke");
                    var parameters = (from p in invoke.GetParameters() select p.ParameterType).ToList();

                    // Add a zeroth "this" argument
                    parameters.Insert(0, tcs.GetType());

                    // Create the event handler
                    handler = new DynamicMethod("lambda", invoke.ReturnType, parameters.ToArray());

                    // Generate body of the handler
                    var gen = handler.GetILGenerator();

                    /* What does the handler do:
                     * 1) initiate an array with the length of parameters minus one
                     * 2) add the parameters except the first one into the array
                     * 3) call the tcs.SetResult(array)
                     */

                    // object[parameters.Count-1] arr;
                    var arr = gen.DeclareLocal(typeof(object[]));   // object[] arr;
                    gen.Emit(OpCodes.Ldc_I4, parameters.Count - 1); // stack = (int32) params.Count - 1;
                    gen.Emit(OpCodes.Newarr, typeof(object)      ); // stack = new object[stack];
                    gen.Emit(OpCodes.Stloc,  arr                 ); // arr = stack;

                    // arr = params.Skip(1);
                    for (int i = 1; i < parameters.Count; i++)
                    {
                        gen.Emit(OpCodes.Ldloc,  arr  ); // var = arr;
                        gen.Emit(OpCodes.Ldc_I4, i - 1); // stack0 = (int32) i - 1;
                        gen.Emit(OpCodes.Ldarg,  i    ); // stack1 = params[i];

                        if (parameters[i].IsValueType)   // if(stack1.Type !== object)
                            gen.Emit(OpCodes.Box, parameters[i]); // stack1 = (object)stack1;

                        gen.Emit(OpCodes.Stelem, typeof(object)); // arr[stack0] = stack1;
                    }

                    // tcs.SetResult(arr);
                    gen.Emit(OpCodes.Ldarg_0   ); // stack0 = params[0] = tcs;
                    gen.Emit(OpCodes.Ldloc, arr); // stack1 = arr;
                    gen.Emit(OpCodes.Call, tcs.GetType().GetMethod("SetResult")); // stack0.SetResult(stack1);

                    // return
                    gen.Emit(OpCodes.Ret);

                    dynamicHandlerCache.Add(delegateType, handler);
                }
            }

            // Construct the delegate
            Delegate deleg = handler.CreateDelegate(delegateType, tcs);

            // target.event += deleg;
            eventInfo.AddEventHandler(target, deleg);

            // wait for it to fire
            var args = await tcs.Task;

            // target.event -= deleg;
            eventInfo.RemoveEventHandler(target, deleg);

            return new EventResult(args,delegateType);
        }


        public static async Task<EventResult> Once<T>(this T target, string eventName)
        {
            var tcs = new TaskCompletionSource<object[]>();

            var eventInfo = target.GetType().GetEvent(eventName);
            var delegateType = eventInfo.EventHandlerType;

            DynamicMethod handler;
            if (!dynamicHandlerCache.TryGetValue(delegateType, out handler))
            {
                Type returnType;
                List<Type> parameterTypes;
                ExtensionMethods.GetDelegateParameterAndReturnTypes(delegateType,
                    out parameterTypes, out returnType);

                var invoke = delegateType.GetMethod("Invoke");
                var parameters = (from p in invoke.GetParameters() select p.ParameterType).ToList();

                if (returnType != typeof(void))
                    throw new NotSupportedException();

                Type tcsType = tcs.GetType();
                MethodInfo setResultMethodInfo = tcsType.GetMethod("SetResult");

                // I'm going to create an instance-like method
                // so, first argument must an instance itself
                // i.e. TaskCompletionSourceHolder *this*
                parameters.Insert(0, tcsType);
                Type[] parameterTypesAr = parameters.ToArray();

                handler = new DynamicMethod("unnamed",
                    returnType, parameterTypesAr, tcsType);

                ILGenerator ilgen = handler.GetILGenerator();

                // declare local variable of type object[]
                LocalBuilder arr = ilgen.DeclareLocal(typeof(object[]));
                // push array's size onto the stack 
                ilgen.Emit(OpCodes.Ldc_I4, parameterTypesAr.Length - 1);
                // create an object array of the given size
                ilgen.Emit(OpCodes.Newarr, typeof(object));
                // and store it in the local variable
                ilgen.Emit(OpCodes.Stloc, arr);

                // iterate thru all arguments except the zero one (i.e. *this*)
                // and store them to the array
                for (int i = 1; i < parameterTypesAr.Length; i++)
                {
                    // push the array onto the stack
                    ilgen.Emit(OpCodes.Ldloc, arr);
                    // push the argument's index onto the stack
                    ilgen.Emit(OpCodes.Ldc_I4, i - 1);
                    // push the argument onto the stack
                    ilgen.Emit(OpCodes.Ldarg, i);

                    // check if it is of a value type
                    // and perform boxing if necessary
                    if (parameterTypesAr[i].IsValueType)
                        ilgen.Emit(OpCodes.Box, parameterTypesAr[i]);

                    // store the value to the argument's array
                    ilgen.Emit(OpCodes.Stelem, typeof(object));
                }

                // load zero-argument (i.e. *this*) onto the stack
                ilgen.Emit(OpCodes.Ldarg_0);
                // load the array onto the stack
                ilgen.Emit(OpCodes.Ldloc, arr);
                // call this.SetResult(arr);
                ilgen.Emit(OpCodes.Call, setResultMethodInfo);
                // and return
                ilgen.Emit(OpCodes.Ret);

                dynamicHandlerCache.Add(delegateType, handler);
            }

            // Construct the delegate
            Delegate deleg = handler.CreateDelegate(delegateType, tcs);

            // target.event += deleg;
            eventInfo.AddEventHandler(target, deleg);

            // wait for it to fire
            var args = await tcs.Task;

            // target.event -= deleg;
            eventInfo.RemoveEventHandler(target, deleg);

            return new EventResult(args, delegateType);
        }

    }


    internal class TaskCompletionSourceHolder
    {
        private readonly TaskCompletionSource<object[]> m_tcs;

        internal object Target { get; set; }
        internal EventInfo EventInfo { get; set; }
        internal Delegate Delegate { get; set; }

        internal TaskCompletionSourceHolder(TaskCompletionSource<object[]> tsc)
        {
            m_tcs = tsc;
        }

        private void SetResult(params object[] args)
        {
            // this method will be called from emitted IL
            // so we can set result here, unsubscribe from the event
            // or do whatever we want.

            // object[] args will contain arguments
            // passed to the event handler
            m_tcs.SetResult(args);
            EventInfo.RemoveEventHandler(Target, Delegate);
        }
    }

    public static class ExtensionMethods
    {
        public static void GetDelegateParameterAndReturnTypes(Type delegateType,
            out List<Type> parameterTypes, out Type returnType)
        {
            if (delegateType.BaseType != typeof(MulticastDelegate))
                throw new ArgumentException("delegateType is not a delegate");

            MethodInfo invoke = delegateType.GetMethod("Invoke");
            if (invoke == null)
                throw new ArgumentException("delegateType is not a delegate.");

            ParameterInfo[] parameters = invoke.GetParameters();
            parameterTypes = new List<Type>(parameters.Length);
            for (int i = 0; i < parameters.Length; i++)
                parameterTypes.Add(parameters[i].ParameterType);

            returnType = invoke.ReturnType;
        }

        
    }
}

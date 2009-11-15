using System;

namespace Void
{
    public static class ObjectExtensions
    {
        /// <summary>
        /// Applies <paramref name="transformation"/> to the object and returns the result.
        /// combined with <see cref="Do{T}"/> this allows for writing code that would usually 
        /// be a series of nested functioncalls, or consecutive lines action on temporary 
        /// variables created by previous lines, to be converted into sequential operations
        /// where each operation acts upon the result of the previous operation.
        /// <example >
        /// <para/>
        /// <para>Nested calls:</para>
        /// <code>
        /// Op4(Op3(Op2(Op1(start))))
        /// </code>   
        /// 
        /// Using temporary variables.
        /// <code>
        /// var temp1 = Op1(start);
        /// var temp2 = Op2(temp1);
        /// var temp3 = Op3(temp2);
        /// Op4(temp3);
        /// </code>
        /// 
        /// Using tranform and do.
        /// <code>
        /// start.Transform(Op1).Transform(Op2).Transform(Op3).Do(Op4);
        /// </code>
        /// </example>
        /// </summary>
        /// <typeparam name="TSource">The type of the object being transformed.</typeparam>
        /// <typeparam name="TReturn">The type of the result of the transformation.</typeparam>
        /// <param name="me">The object being transformed</param>
        /// <param name="transformation">The transformation to be performed</param>
        /// <returns></returns>
        public static TReturn Transform<TSource, TReturn>(this TSource me, Func<TSource, TReturn> transformation)
        {
            return transformation(me);
        }

        /// <summary>
        /// Performes the <paramref name="action"/> using <paramref name="me"/> as the parameter.
        /// 
        /// see <see cref="Transform{TSource,TReturn}"/> for usage.
        /// </summary>
        /// <typeparam name="T">the type of the object being acted upon</typeparam>
        /// <param name="me">the object haveing something done to it</param>
        /// <param name="action">what should be done to the object</param>
        public static void Do<T>(this T me, Action<T> action)
        {
            action(me);
        }
    }
}
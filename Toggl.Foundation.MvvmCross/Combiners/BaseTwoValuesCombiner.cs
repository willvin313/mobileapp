using System;
using System.Collections.Generic;
using System.Linq;
using MvvmCross.Binding.Bindings.SourceSteps;
using MvvmCross.Binding.Combiners;
using MvvmCross.Converters;

namespace Toggl.Foundation.MvvmCross.Combiners
{
    public abstract class BaseTwoValuesCombiner<T1, T2> : MvxValueCombiner
    {
        public override bool TryGetValue(IEnumerable<IMvxSourceStep> steps, out object value)
        {
            T1 first;
            T2 second;

            try
            {
                (first, second) = getParameters(steps);
            }
            catch (ArgumentException)
            {
                value = MvxBindingConstant.UnsetValue;
                return false;
            }

            value = Combine(first, second);
            return true;
        }

        private (T1, T2) getParameters(IEnumerable<IMvxSourceStep> steps)
        {
            T1 first;
            T2 second;

            var parameters = steps.ToArray();
            if (parameters.Length != 2)
            {
                throw new ArgumentException(
                    $"The {GetType().Name} must be used with 2 parameters: a DateTimeOffset and a TimeFormat. It was instead used with {parameters.Length} parameters.");
            }

            first = parameters[0].GetValue() is T1 firstValue
                ? firstValue
                : throw new ArgumentException($"The first parameter of the {GetType().FullName} must be a {nameof(T1)}, but the given parameter is of type {parameters[0].SourceType.FullName}");

            second = parameters[1].GetValue() is T2 secondValue
                ? secondValue 
                : throw new ArgumentException($"The second parameter of the {GetType().FullName} must be a {nameof(T2)}, but the given parameter is of type {parameters[1].SourceType.FullName}");

            return (first, second);
        }

        protected abstract object Combine(T1 first, T2 second);
    }
}

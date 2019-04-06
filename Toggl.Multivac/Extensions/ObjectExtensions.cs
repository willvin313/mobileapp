
using System;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Linq;

namespace Toggl.Multivac.Extensions
{
    public static class ObjectExtensions
    {
        private const string category = "Toggl";

        public static T Dump<T>(this T obj)
        {
            var text = obj == null
                ? "<null>"
                : obj.ToString();

            Debug.WriteLine(text, category);
            return obj;
        }
    }
}

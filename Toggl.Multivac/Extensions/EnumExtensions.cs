using System;
using System.Collections.Generic;
using System.Linq;

namespace Toggl.Multivac.Extensions
{
    public static class EnumExtensions
    {
        public static T ToEnumValue<T>(this string enumAsString)
            => (T)Enum.Parse(typeof(T), enumAsString);
    }
}

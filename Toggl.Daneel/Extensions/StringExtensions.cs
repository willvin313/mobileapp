using System;
using Foundation;

namespace Toggl.Daneel.Extensions
{
    public static class StringExtensions
    {
        public static NSString ToNSString(this string str)
            => new NSString(str);
    }
}

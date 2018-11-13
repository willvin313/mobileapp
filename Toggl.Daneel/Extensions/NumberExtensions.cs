using System;
using Foundation;

namespace Toggl.Daneel.Extensions
{
    public static class NumberExtensions
    {
        public static NSNumber ToNSNumber(this long n)
            => new NSNumber(n);
    }
}

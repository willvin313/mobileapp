using System;
using Foundation;
using Toggl.Foundation.Models.Interfaces;

namespace Toggl.Daneel.Extensions.Models
{
    public static class ProjectExtensions
    {
        public static NSDictionary<NSString, NSObject> ToNSDictionary(this IThreadSafeProject project)
        {
            var keys = new NSString[]
                {
                    "Id".ToNSString(),
                    "Name".ToNSString(),
                    "Color".ToNSString(),
                };

            var values = new NSObject[]
            {
                    project.Id.ToNSNumber(),
                    project.Name.ToNSString(),
                    project.Color.ToNSString(),
            };

            return NSDictionary<NSString, NSObject>.FromObjectsAndKeys(values, keys);
        }
    }
}

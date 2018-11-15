using System;
using Foundation;
using Toggl.Foundation.Models.Interfaces;

namespace Toggl.Daneel.Extensions.Models
{
    public static class TimeEntryExtensions
    {
        public static NSDictionary<NSString, NSObject> ToNSDictionary(this IThreadSafeTimeEntry timeEntry)
        {
            var dict = new NSMutableDictionary<NSString, NSObject>();

            dict.Add("Id".ToNSString(), timeEntry.Id.ToNSNumber());
            dict.Add("Description".ToNSString(), timeEntry.Description.ToNSString());
            dict.Add("Start".ToNSString(), timeEntry.Start.ToNSDate());

            if (timeEntry.Duration.HasValue)
                dict.Add("Duration".ToNSString(), timeEntry.Duration.Value.ToNSNumber());

            if (timeEntry.ProjectId.HasValue)
            {
                dict.Add("ProjectId".ToNSString(), timeEntry.ProjectId.Value.ToNSNumber());
                dict.Add("Project".ToNSString(), timeEntry.Project.ToNSDictionary());
            }

            return NSDictionary<NSString, NSObject>.FromObjectsAndKeys(dict.Values, dict.Keys);
        }
    }
}

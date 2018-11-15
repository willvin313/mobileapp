using System;
using Foundation;
using Toggl.Foundation.Suggestions;

namespace Toggl.Daneel.Extensions.Models
{
    public static class SuggestionExtensions
    {
        public static NSDictionary<NSString, NSObject> ToNSDictionary(this Suggestion suggestion)
        {
            var dict = new NSMutableDictionary<NSString, NSObject>();

            dict.Add("Description".ToNSString(), suggestion.Description.ToNSString());

            if (suggestion.ProjectId.HasValue)
            {
                dict.Add("ProjectId".ToNSString(), suggestion.ProjectId.Value.ToNSNumber());
                dict.Add("ProjectName".ToNSString(), suggestion.ProjectName.ToNSString());
                dict.Add("ProjectColor".ToNSString(), suggestion.ProjectColor.ToNSString());
            }
                    
            return NSDictionary<NSString, NSObject>.FromObjectsAndKeys(dict.Values, dict.Keys);
        }
    }
}

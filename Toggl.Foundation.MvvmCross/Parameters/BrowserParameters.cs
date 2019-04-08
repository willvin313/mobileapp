using System;
using Toggl.Multivac;

namespace Toggl.Foundation.UI.Parameters
{
    [Preserve(AllMembers = true)]
    public sealed class BrowserParameters
    {
        public string Url { get; set; }

        public string Title { get; set; }

        public static BrowserParameters WithUrlAndTitle(string url, string title) =>
            new BrowserParameters
            {
                Url = url,
                Title = title
            };
    }
}

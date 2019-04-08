using System;
using Toggl.Foundation.UI.Collections;
using Toggl.Foundation.UI.ViewModels.TimeEntriesLog.Identity;
using Toggl.Multivac;

namespace Toggl.Foundation.UI.ViewModels.TimeEntriesLog
{
    public sealed class DaySummaryViewModel : IDiffable<IMainLogKey>
    {
        public string Title { get; }

        public string TotalTrackedTime { get; }

        public IMainLogKey Identity { get; }

        public DaySummaryViewModel(DateTime day, string title, string totalTrackedTime)
        {
            Title = title;
            TotalTrackedTime = totalTrackedTime;
            Identity = new DayHeaderKey(day);
        }
    }
}

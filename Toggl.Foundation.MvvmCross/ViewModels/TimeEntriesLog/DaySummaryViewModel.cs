using System;
using Toggl.Foundation.MvvmCross.Collections;
using Toggl.Foundation.MvvmCross.ViewModels.TimeEntriesLog.Identity;
using Toggl.Multivac;

namespace Toggl.Foundation.MvvmCross.ViewModels.TimeEntriesLog
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

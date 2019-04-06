using System;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Toggl.Foundation.Analytics;
using Toggl.Foundation.Sync;
using Toggl.Multivac.Extensions;

namespace Toggl.Foundation.Extensions
{
    public static class SubjectExtensions
    {
        public static void ReemitLastValue<T>(this BehaviorSubject<T> subject)
        {
            subject.OnNext(subject.Value);
        }
    }
}

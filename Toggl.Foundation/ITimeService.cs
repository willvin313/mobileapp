using System;
using System.Reactive;
using System.Reactive.Subjects;
using System.Threading.Tasks;

namespace Toggl.Foundation
{
    public interface ITimeService
    {
        DateTimeOffset CurrentDateTime { get; }
        Task RunAfterDelay(TimeSpan delay, Action action);
        IObservable<DateTimeOffset> MidnightObservable { get; }
        IObservable<DateTimeOffset> CurrentDateTimeObservable { get; }
        IObservable<Unit> SignificantTimeChangeObservable { get; }

        void SignificantTimeChanged();
    }
}

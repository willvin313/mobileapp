using System;
using System.Reactive.Subjects;

namespace Toggl.Multivac.Extensions
{
    public static class SubjectExtensions
    {
        public static void OnNext<T>(this BehaviorSubject<T> subject, Func<T, T> transformation)
        {
            var newValue = transformation.Invoke(subject.Value);
            subject.OnNext(newValue);
        }
    }
}

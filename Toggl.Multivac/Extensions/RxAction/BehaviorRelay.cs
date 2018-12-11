using System;
using System.Reactive.Subjects;

namespace Toggl.Multivac.Extensions.Reactive
{
    public class BehaviorRelay<T> : IObservable<T>
    {
        private BehaviorSubject<T> internalSubject;

        public BehaviorRelay(T value)
        {
            internalSubject = new BehaviorSubject<T>(value);
        }

        public T Value => internalSubject.Value;

        public void Accept(T value)
        {
            internalSubject.OnNext(value);
        }

        public IDisposable Subscribe(IObserver<T> observer)
        {
            return internalSubject.Subscribe(observer);
        }
    }
}
using System;
using System.Reactive.Subjects;

namespace Toggl.Multivac.Extensions.Reactive
{
    /// <summary>
    /// A wrapper for BehaviorSubject.
    /// It behaves just like BehaviorSubject, except that it can't terminate or error out.
    /// </summary>
    /// <typeparam name="T">The type of the elements processed by the subject.</typeparam>
    public class BehaviorRelay<T> : IObservable<T>
    {
        private BehaviorSubject<T> internalSubject;

        public BehaviorRelay(T value) => internalSubject = new BehaviorSubject<T>(value);

        public T Value => internalSubject.Value;

        public void Accept(T value) => internalSubject.OnNext(value);

        public IDisposable Subscribe(IObserver<T> observer) => internalSubject.Subscribe(observer);
    }
}
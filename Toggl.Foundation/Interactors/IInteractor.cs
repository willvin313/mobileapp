using System;
using System.Reactive.Linq;
using System.Runtime.InteropServices;
using Toggl.Foundation.Analytics;
using Toggl.Multivac;

namespace Toggl.Foundation.Interactors
{
    public interface IInteractor<out T>
    {
        T Execute();
    }

    public abstract class TrackableInteractor
    {
        internal IAnalyticsService analyticsService;

        public TrackableInteractor(IAnalyticsService analyticsService)
        {
            Ensure.Argument.IsNotNull(analyticsService, nameof(analyticsService));

            this.analyticsService = analyticsService;
        }
    }

    public class TrackedInteractor<TException, TType> : IInteractor<IObservable<TType>>
    where TException : Exception
    {
        internal IInteractor<IObservable<TType>> innerInteractor;
        private string message;

        public TrackedInteractor(IInteractor<IObservable<TType>> innerInteractor, string message)
        {
            this.innerInteractor = innerInteractor;
            this.message = message;
        }

        public IObservable<TType> Execute()
        {
            return innerInteractor.Execute()
                .Catch<TType, TException>(e =>
                {
                    ((TrackableInteractor)innerInteractor).analyticsService.Track(e, message);
                    return Observable.Throw<TType>(e);
                });
        }
    }

    public static class InteractorExtensions
    {
        public static TrackedInteractor<TException, TType> TrackException<TException, TType>(this IInteractor<IObservable<TType>> interactor, string message)
        where TException : Exception
        {
            return new TrackedInteractor<TException, TType>(interactor, message);
        }
    }
}

using System;
using System.Reactive.Linq;
using Toggl.Multivac;
using Toggl.PrimeRadiant.Onboarding;

namespace Toggl.Foundation.UI.Onboarding.MainView
{
    public sealed class SwipeRightOnboardingStep : IOnboardingStep
    {
        private const int minimumTimeEntriesCount = 5;

        public IObservable<bool> ShouldBeVisible { get; }

        public SwipeRightOnboardingStep(IObservable<bool> conflictingStepsAreNotVisibleObservable, IObservable<int> timeEntriesCountObservable)
        {
            Ensure.Argument.IsNotNull(conflictingStepsAreNotVisibleObservable, nameof(conflictingStepsAreNotVisibleObservable));
            Ensure.Argument.IsNotNull(timeEntriesCountObservable, nameof(timeEntriesCountObservable));

            ShouldBeVisible = Observable.CombineLatest(
                conflictingStepsAreNotVisibleObservable,
                timeEntriesCountObservable,
                (conflictingStepsAreNotVisible, timeEntriesCount) =>
                    conflictingStepsAreNotVisible && timeEntriesCount >= minimumTimeEntriesCount);
        }
    }
}

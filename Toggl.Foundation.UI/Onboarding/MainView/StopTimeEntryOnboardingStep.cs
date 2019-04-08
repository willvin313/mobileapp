using System;
using System.Reactive.Linq;
using Toggl.Multivac;
using Toggl.PrimeRadiant.Onboarding;
using Toggl.PrimeRadiant.Settings;

namespace Toggl.Foundation.UI.Onboarding.MainView
{
    public sealed class StopTimeEntryOnboardingStep : IOnboardingStep
    {
        public IObservable<bool> ShouldBeVisible { get; }

        public StopTimeEntryOnboardingStep(IOnboardingStorage onboardingStorage, IObservable<bool> isTimeEntryRunning)
        {
            Ensure.Argument.IsNotNull(onboardingStorage, nameof(onboardingStorage));
            Ensure.Argument.IsNotNull(isTimeEntryRunning, nameof(isTimeEntryRunning));

            ShouldBeVisible = onboardingStorage.IsNewUser
                .CombineLatest(
                    onboardingStorage.StopButtonWasTappedBefore,
                    isTimeEntryRunning,
                    (isNewUser, stopButtonWasTapped, isRunning) => isNewUser && !stopButtonWasTapped && isRunning)
                .DistinctUntilChanged();
        }
    }
}

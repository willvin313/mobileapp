using System;
using System.Reactive.Linq;
using Toggl.Multivac;
using Toggl.PrimeRadiant.Onboarding;
using Toggl.PrimeRadiant.Settings;

namespace Toggl.Foundation.UI.Onboarding.MainView
{
    public sealed class EditTimeEntryOnboardingStep : IOnboardingStep
    {
        public IObservable<bool> ShouldBeVisible { get; }

        public EditTimeEntryOnboardingStep(IOnboardingStorage onboardingStorage, IObservable<bool> isTimeEntriesLogEmpty)
        {
            Ensure.Argument.IsNotNull(onboardingStorage, nameof(onboardingStorage));
            Ensure.Argument.IsNotNull(isTimeEntriesLogEmpty, nameof(isTimeEntriesLogEmpty));

            ShouldBeVisible = onboardingStorage.UserSignedUpUsingTheApp.CombineLatest(
                onboardingStorage.HasTappedTimeEntry,
                onboardingStorage.NavigatedAwayFromMainViewAfterTappingStopButton,
                onboardingStorage.HasTimeEntryBeenContinued,
                isTimeEntriesLogEmpty,
                shouldBeVisible);
        }

        private bool shouldBeVisible(
            bool signedUpUsingTheApp,
            bool hasTappedTimeEntry,
            bool navigatedAwayFromMainViewAfterTappingStopButton,
            bool hasTimeEntryBeenContinued,
            bool isEmpty)
        {
            return signedUpUsingTheApp 
                && !navigatedAwayFromMainViewAfterTappingStopButton 
                && !hasTimeEntryBeenContinued 
                && !hasTappedTimeEntry 
                && !isEmpty;
        }
    }
}

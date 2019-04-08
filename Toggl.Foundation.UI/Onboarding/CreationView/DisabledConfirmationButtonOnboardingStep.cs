using System;
using System.Reactive.Linq;
using Toggl.Multivac;
using Toggl.PrimeRadiant.Onboarding;
using Toggl.PrimeRadiant.Settings;

namespace Toggl.Foundation.UI.Onboarding.CreationView
{
    public sealed class DisabledConfirmationButtonOnboardingStep : IOnboardingStep
    {
        public IObservable<bool> ShouldBeVisible { get; }

        public DisabledConfirmationButtonOnboardingStep(IOnboardingStorage onboardingStorage, IObservable<bool> isDescriptionEmpty)
        {
            Ensure.Argument.IsNotNull(onboardingStorage, nameof(onboardingStorage));
            Ensure.Argument.IsNotNull(isDescriptionEmpty, nameof(isDescriptionEmpty));

            ShouldBeVisible = onboardingStorage.IsNewUser.CombineLatest(isDescriptionEmpty,
                (isNewUser, isEmpty) => isNewUser && isEmpty);
        }
    }
}

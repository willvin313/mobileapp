using System;
using System.Reactive.Linq;
using Toggl.Multivac;
using Toggl.PrimeRadiant.Onboarding;
using Toggl.PrimeRadiant.Settings;

namespace Toggl.Foundation.UI.Onboarding.EditView
{
    public class CategorizeTimeUsingProjectsOnboardingStep : IOnboardingStep
    {
        public IObservable<bool> ShouldBeVisible { get; }

        public CategorizeTimeUsingProjectsOnboardingStep(IOnboardingStorage storage, IObservable<bool> hasProjectObservable)
        {
            Ensure.Argument.IsNotNull(storage, nameof(storage));
            Ensure.Argument.IsNotNull(hasProjectObservable, nameof(hasProjectObservable));

            ShouldBeVisible = storage.HasEditedTimeEntry.CombineLatest(
                storage.HasSelectedProject,
                hasProjectObservable,
                (hasEditedTimeEntry, hasSelectedProject, hasProject) => !hasEditedTimeEntry && !hasSelectedProject && !hasProject)
                .DistinctUntilChanged();
        }
    }
}

using System;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using MvvmCross.ViewModels;
using Toggl.Foundation.DataSources;
using Toggl.Foundation.Interactors;
using Toggl.Foundation.Suggestions;
using Toggl.Multivac;
using Toggl.Multivac.Extensions;
using Toggl.PrimeRadiant.Settings;
using Toggl.Foundation.MvvmCross.Parameters;
using MvvmCross.Navigation;
using System.Collections.Immutable;

namespace Toggl.Foundation.MvvmCross.ViewModels
{
    [Preserve(AllMembers = true)]
    public sealed class SuggestionsViewModel : MvxViewModel
    {
        private const int suggestionCount = 3;

        public IObservable<IImmutableList<Suggestion>> Suggestions { get; private set; }

        public IObservable<bool> IsEmpty { get; private set; }

        public InputAction<Suggestion> StartTimeEntry { get; private set; }
        public InputAction<Suggestion> StartAndEditTimeEntry { get; private set; }

        private readonly ITimeService timeService;
        private readonly ITogglDataSource dataSource;
        private readonly IOnboardingStorage onboardingStorage;
        private readonly ISchedulerProvider schedulerProvider;
        private readonly IInteractorFactory interactorFactory;
        private readonly IMvxNavigationService navigationService;

        public SuggestionsViewModel(
            ITimeService timeService,
            ITogglDataSource dataSource,
            IInteractorFactory interactorFactory,
            IOnboardingStorage onboardingStorage,
            ISchedulerProvider schedulerProvider,
            IMvxNavigationService navigationService)
        {
            Ensure.Argument.IsNotNull(dataSource, nameof(dataSource));
            Ensure.Argument.IsNotNull(timeService, nameof(timeService));
            Ensure.Argument.IsNotNull(interactorFactory, nameof(interactorFactory));
            Ensure.Argument.IsNotNull(onboardingStorage, nameof(onboardingStorage));
            Ensure.Argument.IsNotNull(schedulerProvider, nameof(schedulerProvider));
            Ensure.Argument.IsNotNull(navigationService, nameof(navigationService));

            this.dataSource = dataSource;
            this.timeService = timeService;
            this.interactorFactory = interactorFactory;
            this.onboardingStorage = onboardingStorage;
            this.schedulerProvider = schedulerProvider;
            this.navigationService = navigationService;

            StartTimeEntry = InputAction<Suggestion>.FromAsync(startTimeEntry);
            StartAndEditTimeEntry = InputAction<Suggestion>.FromAsync(startAndEditTimeEntry);
        }

        public override async Task Initialize()
        {
            await base.Initialize();

            Suggestions = interactorFactory.ObserveWorkspaceOrTimeEntriesChanges().Execute()
                .StartWith(Unit.Default)
                .SelectMany(_ => getSuggestions())
                .AsDriver(onErrorJustReturn: ImmutableList.Create<Suggestion>(), schedulerProvider: schedulerProvider);

            IsEmpty = Suggestions
                .Select(suggestions => suggestions.Count() == 0)
                .StartWith(true)
                .AsDriver(onErrorJustReturn: true, schedulerProvider: schedulerProvider);
        }

        private IObservable<IImmutableList<Suggestion>> getSuggestions()
            => interactorFactory.GetSuggestions(suggestionCount).Execute()
                .Select(suggestions => suggestions.ToImmutableList());

        private async Task startTimeEntry(Suggestion suggestion)
        {
            onboardingStorage.SetTimeEntryContinued();

            await interactorFactory
                .StartSuggestion(suggestion)
                .Execute();
        }

        private async Task startAndEditTimeEntry(Suggestion suggestion)
        {
            var parameters = new StartTimeEntryParameters(
                startTime: timeService.CurrentDateTime,
                placeholderText: "",
                duration: null,
                workspaceId: suggestion.WorkspaceId,
                entryDescription: suggestion.Description,
                projectId: suggestion.ProjectId,
                taskId: suggestion.TaskId
            );

            await navigationService.Navigate<StartTimeEntryViewModel, StartTimeEntryParameters>(parameters);
        }
    }
}

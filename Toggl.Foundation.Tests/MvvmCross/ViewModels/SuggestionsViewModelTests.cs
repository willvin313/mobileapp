using System;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Reactive.Testing;
using NSubstitute;
using Toggl.Foundation.Interactors;
using Toggl.Foundation.MvvmCross.ViewModels;
using Toggl.Foundation.Suggestions;
using Toggl.Foundation.Tests.Generators;
using Xunit;
using TimeEntry = Toggl.Foundation.Models.TimeEntry;
using Toggl.Foundation.Models.Interfaces;
using System.Reactive.Subjects;
using FsCheck.Xunit;
using Toggl.Foundation.Tests.Mocks;
using Toggl.Foundation.MvvmCross.Parameters;
using System.Collections.Immutable;
using FsCheck;

namespace Toggl.Foundation.Tests.MvvmCross.ViewModels
{
    public sealed class SuggestionsViewModelTests
    {
        public abstract class SuggestionsViewModelTest : BaseViewModelTests<SuggestionsViewModel>
        {
            protected override SuggestionsViewModel CreateViewModel()
                => new SuggestionsViewModel(TimeService, DataSource, RxActionFactory, AnalyticsService, InteractorFactory, OnboardingStorage, SchedulerProvider, NavigationService);

            protected override void AdditionalViewModelSetup()
            {
                base.AdditionalViewModelSetup();

                var provider = Substitute.For<ISuggestionProvider>();
                provider.GetSuggestions().Returns(Observable.Empty<Suggestion>());
            }
        }

        public sealed class TheConstructor : SuggestionsViewModelTest
        {
            [Theory, LogIfTooSlow]
            [ConstructorData]
            public void ThrowsIfAnyOfTheArgumentsIsNull(
                bool useDataSource,
                bool useTimeService,
                bool useAnalyticsService,
                bool useNavigationService,
                bool useOnboardingStorage,
                bool useInteractorFactory,
                bool useSchedulerProvider,
                bool useRxActionFactory)
            {
                var dataSource = useDataSource ? DataSource : null;
                var timeService = useTimeService ? TimeService : null;
                var rxActionFactory = useRxActionFactory ? RxActionFactory : null;
                var analyticsService = useAnalyticsService ? AnalyticsService : null;
                var navigationService = useNavigationService ? NavigationService : null;
                var onboardingStorage = useOnboardingStorage ? OnboardingStorage : null;
                var interactorFactory = useInteractorFactory ? InteractorFactory : null;
                var schedulerProvider = useSchedulerProvider ? SchedulerProvider : null;

                Action tryingToConstructWithEmptyParameters =
                    () => new SuggestionsViewModel(
                        timeService,
                        dataSource,
                        rxActionFactory,
                        analyticsService,
                        interactorFactory,
                        onboardingStorage,
                        schedulerProvider,
                        navigationService);

                tryingToConstructWithEmptyParameters
                    .Should().Throw<ArgumentNullException>();
            }
        }

        public sealed class TheSuggestionsProperty : SuggestionsViewModelTest
        {
            [Fact, LogIfTooSlow]
            public async Task IsEmptyIfThereAreNoSuggestions()
            {
                InteractorFactory.GetSuggestions(Arg.Any<int>()).Execute().Returns(Observable.Return(new Suggestion[0]));
                var observer = TestScheduler.CreateObserver<IImmutableList<Suggestion>>();

                await ViewModel.Initialize();
                ViewModel.Suggestions.Subscribe(observer);
                TestScheduler.Start();

                var suggestions = observer.Messages.First().Value.Value;
                suggestions.Should().HaveCount(0);
            }

            [Fact, LogIfTooSlow]
            public async Task ReloadsSuggestionsWhenWorkspacesUpdate()
            {
                var workspaceUpdatedSubject = new Subject<Unit>();
                var observer = TestScheduler.CreateObserver<IImmutableList<Suggestion>>();
                InteractorFactory.ObserveWorkspaceOrTimeEntriesChanges().Execute().Returns(workspaceUpdatedSubject.AsObservable());

                await ViewModel.Initialize();
                ViewModel.Suggestions.Subscribe(observer);

                workspaceUpdatedSubject.OnNext(Unit.Default);

                TestScheduler.Start();

                observer.Messages.Should().HaveCount(2);
                observer.Messages.First().Value.Value.Should().HaveCount(0);
                observer.Messages.Last().Value.Value.Should().HaveCount(0);
            }

            [Fact, LogIfTooSlow]
            public async Task ReloadsSuggestionsWhenTimeEntriesUpdate()
            {
                var provider = Substitute.For<ISuggestionProvider>();
                var getSuggestionsInteractor = Substitute.For<IInteractor<IObservable<IImmutableList<Suggestion>>>>();
                var changesSubject = new Subject<Unit>();
                InteractorFactory.ObserveWorkspaceOrTimeEntriesChanges().Execute().Returns(changesSubject);
                InteractorFactory.GetSuggestions(Arg.Any<int>()).Returns(getSuggestionsInteractor);

                var observer = TestScheduler.CreateObserver<IImmutableList<Suggestion>>();

                await ViewModel.Initialize();
                ViewModel.Suggestions.Subscribe(observer);

                changesSubject.OnNext(Unit.Default);

                TestScheduler.Start();

                observer.Messages.Should().HaveCount(2);
                observer.Messages.First().Value.Value.Should().HaveCount(0);
                observer.Messages.Last().Value.Value.Should().HaveCount(0);

                await getSuggestionsInteractor.Received(2).Execute();
            }

            private ISuggestionProvider suggestionProvider()
            {
                var provider = Substitute.For<ISuggestionProvider>();

                provider.GetSuggestions().Returns(Observable.Empty<Suggestion>());

                return provider;
            }

            private Suggestion createSuggestion(int index) => createSuggestion($"te{index}", 0, 0);

            private Suggestion createSuggestion(string description, long taskId, long projectId) => new Suggestion(
                TimeEntry.Builder.Create(0)
                    .SetDescription(description)
                    .SetStart(DateTimeOffset.UtcNow)
                    .SetAt(DateTimeOffset.UtcNow)
                    .SetTaskId(taskId)
                    .SetProjectId(projectId)
                    .SetWorkspaceId(11)
                    .SetUserId(12)
                    .Build(),
                0.5f,
                SuggestionProviderType.Calendar
            );

            private Recorded<Notification<Suggestion>> createRecorded(int ticks, Suggestion suggestion)
                => new Recorded<Notification<Suggestion>>(ticks, Notification.CreateOnNext(suggestion));
        }

        public sealed class TheStartTimeEntryAction : SuggestionsViewModelTest
        {
            public TheStartTimeEntryAction()
            {
                var user = Substitute.For<IThreadSafeUser>();
                user.Id.Returns(10);
                DataSource.User.Current.Returns(Observable.Return(user));

                TimeService.CurrentDateTime.Returns(DateTimeOffset.Now);
            }

            [Fact, LogIfTooSlow]
            public async Task CallsTheCreateTimeEntryInteractor()
            {
                var suggestion = createSuggestion();
                await ViewModel.Initialize();

                ViewModel.StartTimeEntry.Execute(suggestion);
                TestScheduler.Start();

                InteractorFactory.Received().StartSuggestion(suggestion);
            }

            [Fact, LogIfTooSlow]
            public async Task ExecutesTheContinueTimeEntryInteractor()
            {
                var suggestion = createSuggestion();
                var mockedInteractor = Substitute.For<IInteractor<IObservable<IThreadSafeTimeEntry>>>();
                InteractorFactory.StartSuggestion(Arg.Any<Suggestion>()).Returns(mockedInteractor);
                await ViewModel.Initialize();

                ViewModel.StartTimeEntry.Execute(suggestion);
                TestScheduler.Start();

                await mockedInteractor.Received().Execute();
            }

            [Fact, LogIfTooSlow]
            public async Task CanBeExecutedForTheSecondTimeIfStartingTheFirstOneFinishesSuccessfully()
            {
                var suggestion = createSuggestion();
                var timeEntry = Substitute.For<IThreadSafeTimeEntry>();
                var mockedInteractor = Substitute.For<IInteractor<IObservable<IThreadSafeTimeEntry>>>();
                InteractorFactory.StartSuggestion(Arg.Any<Suggestion>()).Returns(mockedInteractor);
                mockedInteractor.Execute()
                    .Returns(Observable.Return(timeEntry));
                await ViewModel.Initialize();

                var auxObservable = TestScheduler.CreateObserver<Unit>();
                Observable.Concat(
                        Observable.Defer(() => ViewModel.StartTimeEntry.Execute(suggestion)),
                        Observable.Defer(() => ViewModel.StartTimeEntry.Execute(suggestion))
                    )
                    .Subscribe(auxObservable);
                TestScheduler.Start();

                InteractorFactory.Received(2).StartSuggestion(suggestion);
            }

            [Fact, LogIfTooSlow]
            public async Task MarksTheActionForOnboardingPurposes()
            {
                var suggestion = createSuggestion();
                await ViewModel.Initialize();

                var auxObservable = TestScheduler.CreateObserver<Unit>();
                Observable.Concat(
                        Observable.Defer(() => ViewModel.StartTimeEntry.Execute(suggestion)),
                        Observable.Defer(() => ViewModel.StartTimeEntry.Execute(suggestion))
                    )
                    .Subscribe(auxObservable);
                TestScheduler.Start();

                OnboardingStorage.Received().SetTimeEntryContinued();
            }

            [Theory, LogIfTooSlow]
            [InlineData(SuggestionProviderType.MostUsedTimeEntries, 0.3f)]
            [InlineData(SuggestionProviderType.Calendar, 0.8f)]
            public void TracksStartedSuggestion(SuggestionProviderType providerType, float certainty)
            {
                var suggestion = new Suggestion(new MockTimeEntry(), certainty, providerType);

                ViewModel.StartTimeEntry.Execute(suggestion);

                AnalyticsService.SuggestionStarted.Received().Track(providerType.ToString(), certainty);
            }

            private Suggestion createSuggestion()
            {
                var timeEntry = Substitute.For<IThreadSafeTimeEntry>();
                timeEntry.Duration.Returns((long)TimeSpan.FromMinutes(30).TotalSeconds);
                timeEntry.Description.Returns("Testing");
                timeEntry.WorkspaceId.Returns(10);
                return new Suggestion(timeEntry, 0.5f, SuggestionProviderType.Calendar);
            }
        }

        public sealed class TheStartAndEditTimeEntryAction
        {
            public sealed class NavigatesToTheStartTimeEntryView : SuggestionsViewModelTest
            {
                [Fact, LogIfTooSlow]
                public async Task PassingTheCurrentTime()
                {
                    var now = new DateTimeOffset(2020, 1, 2, 3, 4, 5, TimeSpan.Zero);
                    NavigationService.ClearReceivedCalls();
                    TimeService.CurrentDateTime.Returns(now);
                    var suggestion = new Suggestion(new MockTimeEntry(), 0.5f, SuggestionProviderType.Calendar);

                    ViewModel.StartAndEditTimeEntry.Execute(suggestion);

                    await NavigationService.Received().Navigate<StartTimeEntryViewModel, StartTimeEntryParameters>(
                        Arg.Is<StartTimeEntryParameters>(parameters => parameters.StartTime == now)
                    );
                }

                [Theory, LogIfTooSlow]
                [InlineData("")]
                [InlineData("Some description")]
                public async Task PassingTheDescriptionFromSuggestion(string description)
                {
                    var suggestion = new Suggestion(new MockTimeEntry { Description = description }, 0.5f, SuggestionProviderType.Calendar);

                    ViewModel.StartAndEditTimeEntry.Execute(suggestion);

                    await NavigationService.Received().Navigate<StartTimeEntryViewModel, StartTimeEntryParameters>(
                        Arg.Is<StartTimeEntryParameters>(parameters => parameters.EntryDescription == description)
                    );
                }

                [Fact, LogIfTooSlow]
                public async Task PassingTheWorkspaceIdFromSuggestion()
                {
                    long workspaceId = 123;
                    var suggestion = new Suggestion(new MockTimeEntry { WorkspaceId = workspaceId }, 0.5f, SuggestionProviderType.Calendar);

                    ViewModel.StartAndEditTimeEntry.Execute(suggestion);

                    await NavigationService.Received().Navigate<StartTimeEntryViewModel, StartTimeEntryParameters>(
                        Arg.Is<StartTimeEntryParameters>(parameters => parameters.WorkspaceId == workspaceId)
                    );
                }

                [Theory, LogIfTooSlow]
                [InlineData(123)]
                [InlineData(null)]
                public async Task PassingTheProjectIdFromSuggestion(long? projectId)
                {
                    var suggestion = new Suggestion(new MockTimeEntry { ProjectId = projectId }, 0.5f, SuggestionProviderType.Calendar);

                    ViewModel.StartAndEditTimeEntry.Execute(suggestion);

                    await NavigationService.Received().Navigate<StartTimeEntryViewModel, StartTimeEntryParameters>(
                        Arg.Is<StartTimeEntryParameters>(parameters => parameters.ProjectId == projectId)
                    );
                }

                [Theory, LogIfTooSlow]
                [InlineData(420)]
                [InlineData(null)]
                public async Task PassingTheTaskIdFromSuggestion(long? taskId)
                {
                    var suggestion = new Suggestion(new MockTimeEntry { TaskId = taskId }, 0.5f, SuggestionProviderType.Calendar);

                    ViewModel.StartAndEditTimeEntry.Execute(suggestion);

                    await NavigationService.Received().Navigate<StartTimeEntryViewModel, StartTimeEntryParameters>(
                        Arg.Is<StartTimeEntryParameters>(parameters => parameters.TaskId == taskId)
                    );
                }
            }
        }
    }
}

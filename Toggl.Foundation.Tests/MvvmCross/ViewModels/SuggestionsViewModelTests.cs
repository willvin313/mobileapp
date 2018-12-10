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
using Toggl.Foundation.DataSources;
using System.Reactive.Subjects;
using FsCheck.Xunit;
using Toggl.Foundation.Tests.Mocks;
using Toggl.Foundation.MvvmCross.Parameters;
using System.Collections.Immutable;

namespace Toggl.Foundation.Tests.MvvmCross.ViewModels
{
    public sealed class SuggestionsViewModelTests
    {
        public abstract class SuggestionsViewModelTest : BaseViewModelTests<SuggestionsViewModel>
        {
            protected override SuggestionsViewModel CreateViewModel()
                => new SuggestionsViewModel(TimeService, DataSource, InteractorFactory, OnboardingStorage, SchedulerProvider, NavigationService);

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
                bool useNavigationService,
                bool useOnboardingStorage,
                bool useInteractorFactory,
                bool useSchedulerProvider)
            {
                var dataSource = useDataSource ? DataSource : null;
                var timeService = useTimeService ? TimeService : null;
                var navigationService = useNavigationService ? NavigationService : null;
                var onboardingStorage = useOnboardingStorage ? OnboardingStorage : null;
                var interactorFactory = useInteractorFactory ? InteractorFactory : null;
                var schedulerProvider = useSchedulerProvider ? SchedulerProvider : null;

                Action tryingToConstructWithEmptyParameters =
                    () => new SuggestionsViewModel(timeService, dataSource, interactorFactory, onboardingStorage, schedulerProvider, navigationService);

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
                var workspaceUpdatedSubject = new Subject<EntityUpdate<IThreadSafeWorkspace>>();
                DataSource.Workspaces.Updated.Returns(workspaceUpdatedSubject.AsObservable());
                DataSource.Workspaces.Created.Returns(Observable.Empty<IThreadSafeWorkspace>());
                DataSource.Workspaces.Deleted.Returns(Observable.Empty<long>());
                var observer = TestScheduler.CreateObserver<IImmutableList<Suggestion>>();

                await ViewModel.Initialize();
                ViewModel.Suggestions.Subscribe(observer);

                workspaceUpdatedSubject.OnNext(new EntityUpdate<IThreadSafeWorkspace>());

                TestScheduler.Start();

                observer.Messages.Should().HaveCount(2);
                observer.Messages.First().Value.Value.Should().HaveCount(0);
                observer.Messages.Last().Value.Value.Should().HaveCount(0);
            }

            [Fact, LogIfTooSlow]
            public async Task ReloadsSuggestionsWhenTimeEntriesUpdate()
            {
                var timeEntriesUpdatedSubject = new Subject<EntityUpdate<IThreadSafeTimeEntry>>();
                DataSource.TimeEntries.Updated.Returns(timeEntriesUpdatedSubject.AsObservable());
                DataSource.TimeEntries.Created.Returns(Observable.Empty<IThreadSafeTimeEntry>());
                DataSource.TimeEntries.Deleted.Returns(Observable.Empty<long>());

                var observer = TestScheduler.CreateObserver<IImmutableList<Suggestion>>();

                await ViewModel.Initialize();
                ViewModel.Suggestions.Subscribe(observer);

                timeEntriesUpdatedSubject.OnNext(new EntityUpdate<IThreadSafeTimeEntry>());

                TestScheduler.Start();

                observer.Messages.Should().HaveCount(2);
                observer.Messages.First().Value.Value.Should().HaveCount(0);
                observer.Messages.Last().Value.Value.Should().HaveCount(0);
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
                0.5f
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

                await ViewModel.StartTimeEntry.Execute(suggestion);

                InteractorFactory.Received().StartSuggestion(suggestion);
            }

            [Fact, LogIfTooSlow]
            public async Task ExecutesTheContinueTimeEntryInteractor()
            {
                var suggestion = createSuggestion();
                var mockedInteractor = Substitute.For<IInteractor<IObservable<IThreadSafeTimeEntry>>>();
                InteractorFactory.StartSuggestion(Arg.Any<Suggestion>()).Returns(mockedInteractor);
                await ViewModel.Initialize();

                await ViewModel.StartTimeEntry.Execute(suggestion);

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

                await ViewModel.StartTimeEntry.Execute(suggestion);
                await ViewModel.StartTimeEntry.Execute(suggestion);

                InteractorFactory.Received(2).StartSuggestion(suggestion);
            }

            [Fact, LogIfTooSlow]
            public async Task MarksTheActionForOnboardingPurposes()
            {
                var suggestion = createSuggestion();
                await ViewModel.Initialize();

                await ViewModel.StartTimeEntry.Execute(suggestion);
                await ViewModel.StartTimeEntry.Execute(suggestion);

                OnboardingStorage.Received().SetTimeEntryContinued();
            }

            private Suggestion createSuggestion()
            {
                var timeEntry = Substitute.For<IThreadSafeTimeEntry>();
                timeEntry.Duration.Returns((long)TimeSpan.FromMinutes(30).TotalSeconds);
                timeEntry.Description.Returns("Testing");
                timeEntry.WorkspaceId.Returns(10);
                return new Suggestion(timeEntry, 0.5f);
            }
        }

        public sealed class TheStartAndEditTimeEntryAction
        {
            public sealed class NavigatesToTheStartTimeEntryView : SuggestionsViewModelTest
            {
                [Property, LogIfTooSlow]
                public void PassingTheCurrentTime(DateTimeOffset now)
                {
                    TimeService.CurrentDateTime.Returns(now);
                    var suggestion = new Suggestion(new MockTimeEntry(), 0.5f);

                    ViewModel.StartAndEditTimeEntry.Execute(suggestion).Wait();

                    NavigationService.Received().Navigate<StartTimeEntryViewModel, StartTimeEntryParameters>(
                        Arg.Is<StartTimeEntryParameters>(parameters => parameters.StartTime == now)
                    ).Wait();
                }

                [Property, LogIfTooSlow]
                public void PassingTheDescriptionFromSuggestion(string description)
                {
                    var suggestion = new Suggestion(new MockTimeEntry { Description = description }, 0.5f);

                    ViewModel.StartAndEditTimeEntry.Execute(suggestion).Wait();

                    NavigationService.Received().Navigate<StartTimeEntryViewModel, StartTimeEntryParameters>(
                        Arg.Is<StartTimeEntryParameters>(parameters => parameters.EntryDescription == description)
                    ).Wait();
                }

                [Property, LogIfTooSlow]
                public void PassingTheWorkspaceIdFromSuggestion(long workspaceId)
                {
                    var suggestion = new Suggestion(new MockTimeEntry { WorkspaceId = workspaceId }, 0.5f);

                    ViewModel.StartAndEditTimeEntry.Execute(suggestion).Wait();

                    NavigationService.Received().Navigate<StartTimeEntryViewModel, StartTimeEntryParameters>(
                        Arg.Is<StartTimeEntryParameters>(parameters => parameters.WorkspaceId == workspaceId)
                    ).Wait();
                }

                [Property, LogIfTooSlow]
                public void PassingTheProjectIdFromSuggestion(long? projectId)
                {
                    var suggestion = new Suggestion(new MockTimeEntry { ProjectId = projectId }, 0.5f);

                    ViewModel.StartAndEditTimeEntry.Execute(suggestion).Wait();

                    NavigationService.Received().Navigate<StartTimeEntryViewModel, StartTimeEntryParameters>(
                        Arg.Is<StartTimeEntryParameters>(parameters => parameters.ProjectId == projectId)
                    ).Wait();
                }

                [Property, LogIfTooSlow]
                public void PassingTheTaskIdFromSuggestion(long? taskId)
                {
                    var suggestion = new Suggestion(new MockTimeEntry { TaskId = taskId }, 0.5f);

                    ViewModel.StartAndEditTimeEntry.Execute(suggestion).Wait();

                    NavigationService.Received().Navigate<StartTimeEntryViewModel, StartTimeEntryParameters>(
                        Arg.Is<StartTimeEntryParameters>(parameters => parameters.TaskId == taskId)
                    ).Wait();
                }
            }
        }
    }
}

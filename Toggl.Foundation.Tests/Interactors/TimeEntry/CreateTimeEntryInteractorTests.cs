using System;
using System.Reactive.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Reactive.Testing;
using NSubstitute;
using Toggl.Foundation.Analytics;
using Toggl.Foundation.Models.Interfaces;
using Toggl.Foundation.Suggestions;
using Toggl.Foundation.Tests.Mocks;
using Toggl.PrimeRadiant;
using Toggl.PrimeRadiant.Models;
using Toggl.Multivac.Models;
using Xunit;
using ITimeEntryPrototype = Toggl.Foundation.Models.ITimeEntryPrototype;

namespace Toggl.Foundation.Tests.Interactors
{
    public sealed class CreateTimeEntryInteractorTests
    {
        public abstract class BaseCreateTimeEntryInteractorTest : BaseInteractorTests
        {
            protected const long ProjectId = 10;

            protected const long WorkspaceId = 11;

            protected const long UserId = 12;

            protected const long TaskId = 14;

            protected TestScheduler TestScheduler { get; } = new TestScheduler();

            protected string ValidDescription { get; } = "Testing software";

            protected DateTimeOffset ValidTime { get; } = DateTimeOffset.UtcNow;

            protected ITimeEntryPrototype CreatePrototype(
                DateTimeOffset startTime,
                string description,
                bool billable,
                long? projectId,
                long? taskId = null,
                TimeSpan? duration = null) => new MockTimeEntryPrototype
                {
                    TaskId = taskId,
                    WorkspaceId = WorkspaceId,
                    StartTime = startTime,
                    Description = description,
                    IsBillable = billable,
                    ProjectId = projectId,
                    Duration = duration
                };

            protected BaseCreateTimeEntryInteractorTest()
            {
                var user = new MockUser { Id = UserId, DefaultWorkspaceId = WorkspaceId };
                DataSource.User.Current.Returns(Observable.Return(user));

                DataSource.TimeEntries
                    .Create(Arg.Any<IThreadSafeTimeEntry>())
                    .Returns(callInfo => Observable.Return(callInfo.Arg<IThreadSafeTimeEntry>()));
            }

            protected abstract IObservable<IDatabaseTimeEntry> CallInteractor(ITimeEntryPrototype prototype);

            [Fact, LogIfTooSlow]
            public async Task CreatesANewTimeEntryInTheDatabase()
            {
                await CallInteractor(CreatePrototype(ValidTime, ValidDescription, true, ProjectId));

                await DataSource.TimeEntries.ReceivedWithAnyArgs().Create(null);
            }

            [Fact, LogIfTooSlow]
            public async Task CreatesASyncNeededTimeEntry()
            {
                await CallInteractor(CreatePrototype(ValidTime, ValidDescription, true, ProjectId));

                await DataSource.TimeEntries.Received().Create(Arg.Is<IThreadSafeTimeEntry>(
                    te => te.SyncStatus == SyncStatus.SyncNeeded
                ));
            }

            [Theory, LogIfTooSlow]
            [InlineData(true)]
            [InlineData(false)]
            public async Task CreatesATimeEntryWithTheProvidedValueForBillable(bool billable)
            {
                await CallInteractor(CreatePrototype(ValidTime, ValidDescription, billable, ProjectId));

                await DataSource.TimeEntries.Received().Create(Arg.Is<IThreadSafeTimeEntry>(
                    te => te.Billable == billable
                ));
            }

            [Fact, LogIfTooSlow]
            public async Task CreatesATimeEntryWithTheProvidedValueForDescription()
            {
                await CallInteractor(CreatePrototype(ValidTime, ValidDescription, true, ProjectId));

                await DataSource.TimeEntries.Received().Create(Arg.Is<IThreadSafeTimeEntry>(
                    te => te.Description == ValidDescription
                ));
            }

            [Fact, LogIfTooSlow]
            public async Task CreatesATimeEntryWithTheProvidedValueForProjectId()
            {
                await CallInteractor(CreatePrototype(ValidTime, ValidDescription, true, ProjectId));

                await DataSource.TimeEntries.Received().Create(Arg.Is<IThreadSafeTimeEntry>(
                    te => te.ProjectId == ProjectId
                ));
            }

            [Fact, LogIfTooSlow]
            public async Task CreatesATimeEntryWithTheProvidedValueForUserId()
            {
                await CallInteractor(CreatePrototype(ValidTime, ValidDescription, true, ProjectId));

                await DataSource.TimeEntries.Received().Create(Arg.Is<IThreadSafeTimeEntry>(
                    te => te.UserId == UserId
                ));
            }

            [Fact, LogIfTooSlow]
            public async Task CreatesATimeEntryWithTheProvidedValueForTaskId()
            {
                await CallInteractor(CreatePrototype(ValidTime, ValidDescription, true, ProjectId, TaskId));

                await DataSource.TimeEntries.Received().Create(Arg.Is<IThreadSafeTimeEntry>(
                    te => te.TaskId == TaskId
                ));
            }

            [Fact, LogIfTooSlow]
            public async Task CreatesATimeEntryWithTheProvidedValueForWorkspaceId()
            {
                await CallInteractor(CreatePrototype(ValidTime, ValidDescription, true, ProjectId));

                await DataSource.TimeEntries.Received().Create(Arg.Is<IThreadSafeTimeEntry>(
                    te => te.WorkspaceId == WorkspaceId
                ));
            }

            [Fact, LogIfTooSlow]
            public async Task CreatesATimeEntryWithAnIdProvidedByTheIdProvider()
            {
                IdProvider.GetNextIdentifier().Returns(-1);

                await CallInteractor(CreatePrototype(ValidTime, ValidDescription, true, ProjectId));

                await DataSource.TimeEntries.Received().Create(Arg.Is<IThreadSafeTimeEntry>(
                    te => te.Id == -1
                ));
            }

            [Fact, LogIfTooSlow]
            public async Task InitiatesPushSyncWhenThereIsARunningTimeEntry()
            {
                await CallInteractor(CreatePrototype(ValidTime, ValidDescription, true, ProjectId));

                await DataSource.SyncManager.Received().PushSync();
            }

            [Fact, LogIfTooSlow]
            public async Task DoesNotInitiatePushSyncWhenStartingFails()
            {
                var observable = Observable.Throw<IThreadSafeTimeEntry>(new Exception());
                DataSource.TimeEntries.Create(Arg.Any<IThreadSafeTimeEntry>())
                          .Returns(observable);

                Action executeCommand =
                    () => CallInteractor(CreatePrototype(ValidTime, ValidDescription, true, ProjectId)).Wait();

                executeCommand.Should().Throw<Exception>();
                await DataSource.SyncManager.DidNotReceive().PushSync();
            }
        }

        public sealed class TheContinueTimeEntryInteractor : BaseCreateTimeEntryInteractorTest
        {
            protected override IObservable<IDatabaseTimeEntry> CallInteractor(ITimeEntryPrototype prototype)
                => InteractorFactory.ContinueTimeEntry(prototype).Execute();

            public TheContinueTimeEntryInteractor()
            {
                TimeService.CurrentDateTime.Returns(DateTimeOffset.Now);
            }

            [Fact, LogIfTooSlow]
            public async Task RegistersTheEventAsATimerEventIfManualModeIsDisabled()
            {
                await CallInteractor(CreatePrototype(ValidTime, ValidDescription, true, ProjectId));

                AnalyticsService.Received().Track(Arg.Is<StartTimeEntryEvent>(
                        startTimeEntryEvent => startTimeEntryEvent.Origin == TimeEntryStartOrigin.Continue));
            }

            [Fact, LogIfTooSlow]
            public async Task CreatesATimeEntryWithTheCurrentTimeProvidedByTheTimeService()
            {
                await CallInteractor(CreatePrototype(ValidTime, ValidDescription, true, ProjectId));

                await DataSource.TimeEntries.Received().Create(Arg.Is<IThreadSafeTimeEntry>(
                    te => te.Start == TimeService.CurrentDateTime
                ));
            }
        }

        public sealed class TheStartSuggestionInteractor : BaseCreateTimeEntryInteractorTest
        {
            protected override IObservable<IDatabaseTimeEntry> CallInteractor(ITimeEntryPrototype prototype)
            {
                var suggestion = new Suggestion(new MockTimeEntry
                {
                    WorkspaceId = prototype.WorkspaceId,
                    ProjectId = prototype.ProjectId,
                    TaskId = prototype.TaskId,
                    Billable = prototype.IsBillable,
                    Start = prototype.StartTime,
                    Duration = prototype.Duration?.Ticks,
                    Description = prototype.Description,
                    TagIds = prototype.TagIds
                });

                return InteractorFactory.StartSuggestion(suggestion).Execute();
            }

            public TheStartSuggestionInteractor()
            {
                TimeService.CurrentDateTime.Returns(DateTimeOffset.Now);
            }

            [Fact, LogIfTooSlow]
            public async Task RegistersTheEventAsATimerEventIfManualModeIsDisabled()
            {
                await CallInteractor(CreatePrototype(ValidTime, ValidDescription, true, ProjectId));

                AnalyticsService.Received().Track(Arg.Is<StartTimeEntryEvent>(
                    startTimeEntryEvent => startTimeEntryEvent.Origin == TimeEntryStartOrigin.Suggestion));
            }

            [Fact, LogIfTooSlow]
            public async Task CreatesATimeEntryWithTheCurrentTimeProvidedByTheTimeService()
            {
                await CallInteractor(CreatePrototype(ValidTime, ValidDescription, true, ProjectId));

                await DataSource.TimeEntries.Received().Create(Arg.Is<IThreadSafeTimeEntry>(
                    te => te.Start == TimeService.CurrentDateTime
                ));
            }
        }

        public sealed class TheCreateTimeEntryInteractor : BaseCreateTimeEntryInteractorTest
        {
            protected override IObservable<IDatabaseTimeEntry> CallInteractor(ITimeEntryPrototype prototype)
                => InteractorFactory.CreateTimeEntry(prototype).Execute();

            [Fact, LogIfTooSlow]
            public async Task RegistersTheEventAsATimerEventIfManualModeIsDisabled()
            {
                await CallInteractor(CreatePrototype(ValidTime, ValidDescription, true, ProjectId));

                AnalyticsService.Received().Track(Arg.Is<StartTimeEntryEvent>(
                    startTimeEntryEvent => startTimeEntryEvent.Origin == TimeEntryStartOrigin.Timer));
            }

            [Fact, LogIfTooSlow]
            public async Task RegistersTheEventAsAManualEventIfManualModeIsEnabled()
            {
                var prototype = CreatePrototype(ValidTime, ValidDescription, true, ProjectId, duration: TimeSpan.FromMinutes(1));
                await CallInteractor(prototype);

                AnalyticsService.Received().Track(Arg.Is<StartTimeEntryEvent>(
                    startTimeEntryEvent => startTimeEntryEvent.Origin == TimeEntryStartOrigin.Manual));
            }

            [Fact, LogIfTooSlow]
            public async Task CreatesATimeEntryWithTheProvidedValueForStartTime()
            {
                await CallInteractor(CreatePrototype(ValidTime, ValidDescription, true, ProjectId));

                await DataSource.TimeEntries.Received().Create(Arg.Is<IThreadSafeTimeEntry>(
                    te => te.Start == ValidTime
                ));
            }

            [Fact, LogIfTooSlow]
            public async Task DonatesTheIntent()
            {
                await CallInteractor(CreatePrototype(ValidTime, ValidDescription, true, ProjectId));

                IntentDonationService.Received().DonateStartTimeEntry(Arg.Any<IWorkspace>(), Arg.Any<ITimeEntry>());
            }
        }
    }
}

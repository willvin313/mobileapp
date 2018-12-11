using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using FsCheck.Xunit;
using NSubstitute;
using Toggl.Foundation.Sync;
using Toggl.Foundation.Sync.States;
using Toggl.Foundation.Sync.States.Pull;
using Toggl.Multivac.Models;
using Toggl.PrimeRadiant;
using Toggl.PrimeRadiant.Models;
using Toggl.Ultrawave;
using Xunit;

namespace Toggl.Foundation.Tests.Sync.States.Pull
{
    public sealed class FetchAllSinceStateTests
    {
        public sealed class TheStartMethod
        {
            private readonly ITogglDatabase database;
            private readonly ITogglApi api;
            private readonly ITimeService timeService;
            private readonly ILeakyBucket leakyBucket;
            private readonly FetchAllSinceState state;
            private readonly DateTimeOffset now = new DateTimeOffset(2017, 02, 15, 13, 50, 00, TimeSpan.Zero);

            public TheStartMethod()
            {
                database = Substitute.For<ITogglDatabase>();
                api = Substitute.For<ITogglApi>();
                timeService = Substitute.For<ITimeService>();
                leakyBucket = Substitute.For<ILeakyBucket>();
                leakyBucket.TryClaimFreeSlots(Arg.Any<DateTimeOffset>(), Arg.Any<int>(), out _).Returns(true);
                timeService.CurrentDateTime.Returns(now);
                state = new FetchAllSinceState(database, api, timeService, leakyBucket);
            }

            [Fact, LogIfTooSlow]
            public void EmitsTransitionToFetchStartedResult()
            {
                var transition = state.Start().SingleAsync().Wait();

                transition.Result.Should().Be(state.FetchStarted);
            }

            [Fact, LogIfTooSlow]
            public void MakesNoApiCallsBeforeSubscription()
            {
                state.Start();

                var w = api.DidNotReceive().Workspaces;
                var wf = api.DidNotReceive().WorkspaceFeatures;
                var u = api.DidNotReceive().User;
                var c = api.DidNotReceive().Clients;
                var p = api.DidNotReceive().Projects;
                var t = api.DidNotReceive().TimeEntries;
                var ta = api.DidNotReceive().Tags;
                var ts = api.DidNotReceive().Tasks;
                var pr = api.DidNotReceive().Preferences;
            }

            [Property]
            public void MakesCorrectCallsWithSinceThresholdsWhenSinceIsLessThanTwoMonthsInThePast(int seed)
            {
                var rnd = new Random(seed);
                var percent = rnd.NextDouble();

                var now = timeService.CurrentDateTime;
                var twoMonths = (now.AddMonths(2) - now);
                var seconds = twoMonths.TotalSeconds * percent;
                var since = now.AddSeconds(-seconds);

                var sinceParameters = Substitute.For<ISinceParameterRepository>();
                sinceParameters.Get<IDatabaseClient>().Returns(since);
                sinceParameters.Get<IDatabaseProject>().Returns(since);
                sinceParameters.Get<IDatabaseTask>().Returns(since);
                sinceParameters.Get<IDatabaseTag>().Returns(since);
                sinceParameters.Get<IDatabaseWorkspace>().Returns(since);
                sinceParameters.Get<IDatabaseTimeEntry>().Returns(since);

                database.SinceParameters.Returns(sinceParameters);

                state.Start().Wait();

                api.Workspaces.Received().GetAll();
                api.Clients.Received().GetAllSince(since);
                api.Projects.Received().GetAllSince(since);
                api.TimeEntries.Received().GetAllSince(since);
                api.Tasks.Received().GetAllSince(since);
                api.Tags.Received().GetAllSince(since);
            }

            [Fact, LogIfTooSlow]
            public void MakesApiCallsWithoutTheSinceParameterWhenTheThresholdIsMoreThanTwoMonthsInThePast()
            {
                var now = timeService.CurrentDateTime;

                var sinceParameters = Substitute.For<ISinceParameterRepository>();
                sinceParameters.Get<IDatabaseClient>().Returns(now.AddMonths(-3));
                sinceParameters.Get<IDatabaseProject>().Returns(now.AddMonths(-4));
                sinceParameters.Get<IDatabaseTask>().Returns(now.AddMonths(-5));
                sinceParameters.Get<IDatabaseTag>().Returns(now.AddMonths(-6));
                sinceParameters.Get<IDatabaseWorkspace>().Returns(now.AddMonths(-7));
                sinceParameters.Get<IDatabaseTimeEntry>().Returns(now.AddMonths(-8));

                database.SinceParameters.Returns(sinceParameters);

                state.Start().Wait();

                api.Workspaces.Received().GetAll();
                api.Clients.Received().GetAll();
                api.Projects.Received().GetAll();
                api.TimeEntries.Received().GetAll(Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>());
                api.Tasks.Received().GetAll();
                api.Tags.Received().GetAll();
            }

            [Fact, LogIfTooSlow]
            public void MakesCorrectCallsWithoutSinceThresholds()
            {
                state.Start().Wait();

                api.Workspaces.Received().GetAll();
                api.Clients.Received().GetAll();
                api.Projects.Received().GetAll();
                api.TimeEntries.Received().GetAll(Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>());
                api.Tasks.Received().GetAll();
                api.Tags.Received().GetAll();
            }

            [Fact, LogIfTooSlow]
            public void ConnectsToApiCallObservables()
            {
                var workspaceCall = false;
                var workspaceFeaturesCall = false;
                var userCall = false;
                var clientCall = false;
                var projectCall = false;
                var timeEntriesCall = false;
                var taskCall = false;
                var tagCall = false;
                var preferencesCall = false;

                api.Workspaces.GetAll().Returns(Observable.Create<List<IWorkspace>>(o => { workspaceCall = true; return () => { }; }));
                api.WorkspaceFeatures.GetAll().Returns(Observable.Create<List<IWorkspaceFeatureCollection>>(o => { workspaceFeaturesCall = true; return () => { }; }));
                api.User.Get().Returns(Observable.Create<IUser>(o => { userCall = true; return () => { }; }));
                api.Clients.GetAll().Returns(Observable.Create<List<IClient>>(o => { clientCall = true; return () => { }; }));
                api.Projects.GetAll().Returns(Observable.Create<List<IProject>>(o => { projectCall = true; return () => { }; }));
                api.TimeEntries.GetAll(Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>())
                    .Returns(Observable.Create<List<ITimeEntry>>(o => { timeEntriesCall = true; return () => { }; }));
                api.Tasks.GetAll().Returns(Observable.Create<List<ITask>>(o => { taskCall = true; return () => { }; }));
                api.Tags.GetAll().Returns(Observable.Create<List<ITag>>(o => { tagCall = true; return () => { }; }));
                api.Preferences.Get().Returns(Observable.Create<IPreferences>(o => { preferencesCall = true; return () => { }; }));

                state.Start().Wait();

                workspaceCall.Should().BeTrue();
                workspaceFeaturesCall.Should().BeTrue();
                userCall.Should().BeTrue();
                clientCall.Should().BeTrue();
                projectCall.Should().BeTrue();
                timeEntriesCall.Should().BeTrue();
                taskCall.Should().BeTrue();
                tagCall.Should().BeTrue();
                preferencesCall.Should().BeTrue();
            }

            [Fact, LogIfTooSlow]
            public void ReturnsReplayingApiCallObservables()
            {
                api.Workspaces.GetAll().Returns(Observable.Return<List<IWorkspace>>(null));
                api.WorkspaceFeatures.GetAll().Returns(Observable.Return<List<IWorkspaceFeatureCollection>>(null));
                api.User.Get().Returns(Observable.Return<IUser>(null));
                api.Clients.GetAll().Returns(Observable.Return<List<IClient>>(null));
                api.Projects.GetAll().Returns(Observable.Return<List<IProject>>(null));
                api.TimeEntries.GetAll(Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>())
                    .Returns(Observable.Return<List<ITimeEntry>>(null));
                api.Tasks.GetAll().Returns(Observable.Return<List<ITask>>(null));
                api.Tags.GetAll().Returns(Observable.Return<List<ITag>>(null));
                api.Preferences.Get().Returns(Observable.Return<IPreferences>(null));

                var transition = (Transition<IFetchObservables>)state.Start().SingleAsync().Wait();

                var observables = transition.Parameter;
                observables.GetList<IWorkspace>().SingleAsync().Wait().Should().BeNull();
                observables.GetList<IWorkspaceFeatureCollection>().SingleAsync().Wait().Should().BeNull();
                observables.GetSingle<IUser>().SingleAsync().Wait().Should().BeNull();
                observables.GetList<IClient>().SingleAsync().Wait().Should().BeNull();
                observables.GetList<IProject>().SingleAsync().Wait().Should().BeNull();
                observables.GetList<ITimeEntry>().SingleAsync().Wait().Should().BeNull();
                observables.GetList<ITask>().SingleAsync().Wait().Should().BeNull();
                observables.GetList<ITag>().SingleAsync().Wait().Should().BeNull();
                observables.GetSingle<IPreferences>().SingleAsync().Wait().Should().BeNull();
            }

            [Fact, LogIfTooSlow]
            public async Task FetchesTwoMonthsOfTimeEntriesDataIncludingTwoDaysAfterNow()
            {
                var sinceParameters = Substitute.For<ISinceParameterRepository>();
                sinceParameters.Get<IDatabaseTimeEntry>().Returns(now.AddMonths(-8));

                await state.Start().SingleAsync();

                var max = TimeSpan.FromDays(62);
                var min = TimeSpan.FromDays(59);

                await api.TimeEntries.Received().GetAll(
                    Arg.Is<DateTimeOffset>(start => min <= now - start && now - start <= max), Arg.Is(now.AddDays(2)));
            }

            [Property]
            public void ReturnsPreventServerOverloadWithCorrectDelayWhenTheLeakyBucketIsFull(TimeSpan delay)
            {
                leakyBucket.TryClaimFreeSlots(Arg.Any<DateTimeOffset>(), Arg.Any<int>(), out _)
                    .Returns(x =>
                    {
                        x[2] = delay;
                        return false;
                    });

                var transition = state.Start().SingleAsync().Wait();

                transition.Result.Should().Be(state.PreventOverloadingServer);
                var parameter = ((Transition<TimeSpan>)transition).Parameter;
            }
        }
    }
}

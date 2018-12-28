using System;
using System.Linq;
using System.Reactive.Linq;
using FluentAssertions;
using NSubstitute;
using Toggl.Foundation.Interactors;
using Toggl.Foundation.Interactors.Suggestions;
using Toggl.Foundation.Models.Interfaces;
using Toggl.Foundation.Tests.Generators;
using Toggl.Foundation.Tests.Mocks;
using Xunit;

namespace Toggl.Foundation.Tests.Interactors.Suggestions
{
    public class GetSuggestionProvidersInteractorTests
    {
        public sealed class TheConstructor : BaseInteractorTests
        {
            private readonly IInteractor<IObservable<IThreadSafeWorkspace>> defaultWorkspaceInteractor = Substitute.For<IInteractor<IObservable<IThreadSafeWorkspace>>>();

            [Theory, LogIfTooSlow]
            [ConstructorData]
            public void ThrowsIfAnyOfTheArgumentsIsNull(
                bool useStopWatchProvider,
                bool useDataSource,
                bool useTimeService,
                bool useCalendarService,
                bool useDefaultWorkspaceInteractor)
            {
                Action createInstance = () => new GetSuggestionProvidersInteractor(
                    3,
                    useStopWatchProvider ? StopwatchProvider : null,
                    useDataSource ? DataSource : null,
                    useTimeService ? TimeService : null,
                    useCalendarService ? CalendarService : null,
                    useDefaultWorkspaceInteractor ? defaultWorkspaceInteractor : null);

                createInstance.Should().Throw<ArgumentNullException>();
            }

            [Theory, LogIfTooSlow]
            [InlineData(0)]
            [InlineData(-1)]
            [InlineData(10)]
            [InlineData(-100)]
            [InlineData(256)]
            public void ThrowsIfTheCountIsOutOfRange(int count)
            {
                Action createInstance = () => new GetSuggestionProvidersInteractor(
                        count,
                        StopwatchProvider,
                        DataSource,
                        TimeService,
                        CalendarService,
                        defaultWorkspaceInteractor);

                createInstance.Should().Throw<ArgumentException>();
            }
        }

        public sealed class TheExecuteMethod : BaseInteractorTests
        {
            private GetSuggestionProvidersInteractor interactor;
            private IThreadSafeWorkspace defaultWorkspace = new MockWorkspace { Id = 12, IsInaccessible = false };

            public TheExecuteMethod()
            {
                var defaultWorkspaceInteractor = Substitute.For<IInteractor<IObservable<IThreadSafeWorkspace>>>();
                defaultWorkspaceInteractor.Execute().Returns(Observable.Return(defaultWorkspace));
                interactor = new GetSuggestionProvidersInteractor(
                    3,
                    StopwatchProvider,
                    DataSource,
                    TimeService,
                    CalendarService,
                    defaultWorkspaceInteractor);
            }

            [Fact, LogIfTooSlow]
            public void ReturnsAllProviders()
            {
                var providers = interactor.Execute().ToList();

                providers.Count.Should().Be(3);
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using FluentAssertions;
using NSubstitute;
using Toggl.Foundation.Interactors;
using Toggl.Foundation.Models;
using Toggl.Foundation.UI.ViewModels;
using Toggl.Foundation.Tests.Generators;
using Toggl.Foundation.Tests.Mocks;
using Toggl.PrimeRadiant.Models;
using Xunit;

namespace Toggl.Foundation.Tests.MvvmCross.ViewModels
{
    public sealed class SyncFailuresViewModelTests
    {
        public abstract class SyncFailuresViewModelTest : BaseViewModelTests<SyncFailuresViewModel>
        {
            protected override SyncFailuresViewModel CreateViewModel()
            {
                var vm = new SyncFailuresViewModel(InteractorFactory);
                return vm;
            }
        }

        public sealed class TheConstructor : SyncFailuresViewModelTest
        {
            [Fact, LogIfTooSlow]
            public void ThrowsIfTheArgumentIsNull()
            {
                Action tryingToConstructWithEmptyParameters =
                    () => new SyncFailuresViewModel(null);

                tryingToConstructWithEmptyParameters
                    .Should().Throw<ArgumentNullException>();
            }
        }

        public sealed class TheSyncFailuresProperty : SyncFailuresViewModelTest
        {
            [Fact, LogIfTooSlow]
            public void ExecutesGetItemsThatFailedInteractor()
            {
                var mockedInteractor = Substitute.For<IInteractor<IObservable<IEnumerable<SyncFailureItem>>>>();
                InteractorFactory.GetItemsThatFailedToSync().Returns(mockedInteractor);

                ViewModel.Initialize();

                mockedInteractor.Received().Execute();
            }

            [Fact, LogIfTooSlow]
            public void ReturnsTheInteractorResultAsAList()
            {
                var mockedInteractor = Substitute.For<IInteractor<IObservable<IEnumerable<SyncFailureItem>>>>();
                InteractorFactory.GetItemsThatFailedToSync().Returns(mockedInteractor);

                SyncFailureItem[] result = {
                    new SyncFailureItem(new MockProject { Name = "MyProject" }),
                    new SyncFailureItem(new MockProject { Name = "MyProject 2" }),
                    new SyncFailureItem(new MockTag { Name = "MyTag" })
                };

                mockedInteractor.Execute().Returns(Observable.Return(result));

                ViewModel.Initialize();

                ViewModel.SyncFailures.Should().BeEquivalentTo(result);
            }
        }
    }
}

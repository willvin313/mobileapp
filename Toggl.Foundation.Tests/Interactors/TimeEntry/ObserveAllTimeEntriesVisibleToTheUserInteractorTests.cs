using System.Collections.Generic;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using FluentAssertions;
using Microsoft.Reactive.Testing;
using NSubstitute;
using Toggl.Foundation.DataSources;
using Toggl.Foundation.Interactors;
using Toggl.Foundation.Models.Interfaces;
using Toggl.Foundation.Tests.Mocks;
using Xunit;

namespace Toggl.Foundation.Tests.Interactors.TimeEntry
{
    public class ObserveAllTimeEntriesVisibleToTheUserInteractorTests
    {
        public sealed class WhenTimeEntriesChange : BaseInteractorTests
        {
            [Fact, LogIfTooSlow]
            public void EmitsAnEventWhenATimeEntryIsCreated()
            {
                var createSubject = new Subject<IThreadSafeTimeEntry>();
                DataSource.TimeEntries.Created.Returns(createSubject);
                DataSource.TimeEntries.Updated.Returns(Observable.Never<EntityUpdate<IThreadSafeTimeEntry>>());
                DataSource.TimeEntries.Deleted.Returns(Observable.Never<long>());

                var testScheduler = new TestScheduler();
                var observer = testScheduler.CreateObserver<IEnumerable<IThreadSafeTimeEntry>>();

                InteractorFactory.ObserveAllTimeEntriesVisibleToTheUser()
                    .Execute()
                    .Subscribe(observer);

                var mockTimeEntry = new MockTimeEntry { Id = 42 };
                createSubject.OnNext(mockTimeEntry);

                observer.Messages.Should().HaveCount(1);
            }

            [Fact, LogIfTooSlow]
            public void EmitsAnEventWhenATimeEntryIsUpdated()
            {
                var updateSubject = new Subject<EntityUpdate<IThreadSafeTimeEntry>>();
                DataSource.TimeEntries.Created.Returns(Observable.Never<IThreadSafeTimeEntry>());
                DataSource.TimeEntries.Updated.Returns(updateSubject);
                DataSource.TimeEntries.Deleted.Returns(Observable.Never<long>());

                var testScheduler = new TestScheduler();
                var observer = testScheduler.CreateObserver<IEnumerable<IThreadSafeTimeEntry>>();

                InteractorFactory.ObserveAllTimeEntriesVisibleToTheUser()
                    .Execute()
                    .Subscribe(observer);

                var mockTimeEntry = new MockTimeEntry { Id = 42 };
                updateSubject.OnNext(new EntityUpdate<IThreadSafeTimeEntry>(mockTimeEntry.Id, mockTimeEntry));

                observer.Messages.Should().HaveCount(1);
            }

            [Fact, LogIfTooSlow]
            public void EmitsAnEventWhenATimeEntryIsDeleted()
            {
                var deleteSubject = new Subject<long>();
                DataSource.TimeEntries.Created.Returns(Observable.Never<IThreadSafeTimeEntry>());
                DataSource.TimeEntries.Updated.Returns(Observable.Never<EntityUpdate<IThreadSafeTimeEntry>>());
                DataSource.TimeEntries.Deleted.Returns(deleteSubject);

                var testScheduler = new TestScheduler();
                var observer = testScheduler.CreateObserver<IEnumerable<IThreadSafeTimeEntry>>();

                InteractorFactory.ObserveAllTimeEntriesVisibleToTheUser()
                    .Execute()
                    .Subscribe(observer);

                deleteSubject.OnNext(42);

                observer.Messages.Should().HaveCount(1);
            }
        }

        public sealed class WhenWorkspacesChange : BaseInteractorTests
        {
            [Fact, LogIfTooSlow]
            public void EmitsAnEventWhenATimeEntryIsCreated()
            {
                var createSubject = new Subject<IThreadSafeWorkspace>();
                DataSource.Workspaces.Created.Returns(createSubject);
                DataSource.Workspaces.Updated.Returns(Observable.Never<EntityUpdate<IThreadSafeWorkspace>>());
                DataSource.Workspaces.Deleted.Returns(Observable.Never<long>());

                var testScheduler = new TestScheduler();
                var observer = testScheduler.CreateObserver<IEnumerable<IThreadSafeTimeEntry>>();

                InteractorFactory.ObserveAllTimeEntriesVisibleToTheUser()
                    .Execute()
                    .Subscribe(observer);

                var mockWorkspace = new MockWorkspace { Id = 42 };
                createSubject.OnNext(mockWorkspace);

                observer.Messages.Should().HaveCount(1);
            }

            [Fact, LogIfTooSlow]
            public void EmitsAnEventWhenAWorkspaceIsUpdated()
            {
                var updateSubject = new Subject<EntityUpdate<IThreadSafeWorkspace>>();
                DataSource.Workspaces.Created.Returns(Observable.Never<IThreadSafeWorkspace>());
                DataSource.Workspaces.Updated.Returns(updateSubject);
                DataSource.Workspaces.Deleted.Returns(Observable.Never<long>());

                var testScheduler = new TestScheduler();
                var observer = testScheduler.CreateObserver<IEnumerable<IThreadSafeTimeEntry>>();

                InteractorFactory.ObserveAllTimeEntriesVisibleToTheUser()
                    .Execute()
                    .Subscribe(observer);

                var mockWorkspace = new MockWorkspace { Id = 42 };
                updateSubject.OnNext(new EntityUpdate<IThreadSafeWorkspace>(mockWorkspace.Id, mockWorkspace));

                observer.Messages.Should().HaveCount(1);
            }

            [Fact, LogIfTooSlow]
            public void EmitsAnEventWhenAWorkspaceIsDeleted()
            {
                var deleteSubject = new Subject<long>();
                DataSource.Workspaces.Created.Returns(Observable.Never<IThreadSafeWorkspace>());
                DataSource.Workspaces.Updated.Returns(Observable.Never<EntityUpdate<IThreadSafeWorkspace>>());
                DataSource.Workspaces.Deleted.Returns(deleteSubject);

                var testScheduler = new TestScheduler();
                var observer = testScheduler.CreateObserver<IEnumerable<IThreadSafeTimeEntry>>();

                InteractorFactory.ObserveAllTimeEntriesVisibleToTheUser()
                    .Execute()
                    .Subscribe(observer);

                deleteSubject.OnNext(42);

                observer.Messages.Should().HaveCount(1);
            }
        }
    }
}

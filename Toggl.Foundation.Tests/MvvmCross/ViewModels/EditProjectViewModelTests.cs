using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Reactive.Testing;
using MvvmCross.UI;
using NSubstitute;
using Toggl.Foundation.DTOs;
using Toggl.Foundation.Models.Interfaces;
using Toggl.Foundation.MvvmCross.Parameters;
using Toggl.Foundation.MvvmCross.ViewModels;
using Toggl.Foundation.Tests.Generators;
using Toggl.Foundation.Tests.Mocks;
using Xunit;
using ProjectPredicate = System.Func<Toggl.PrimeRadiant.Models.IDatabaseProject, bool>;

namespace Toggl.Foundation.Tests.MvvmCross.ViewModels
{
    public sealed class EditProjectViewModelTests
    {
        public abstract class EditProjectViewModelTest : BaseViewModelTests<EditProjectViewModel>
        {
            protected const long DefaultWorkspaceId = 10;
            protected const string DefaultWorkspaceName = "Some workspace name";
            protected IThreadSafeWorkspace Workspace { get; } = Substitute.For<IThreadSafeWorkspace>();

            protected EditProjectViewModelTest()
            {
                ViewModel.Prepare("A valid name");
            }

            protected override EditProjectViewModel CreateViewModel()
                => new EditProjectViewModel(
                    DataSource,
                    DialogService,
                    RxActionFactory,
                    InteractorFactory,
                    SchedulerProvider,
                    StopwatchProvider,
                    NavigationService
                );
        }

        public abstract class EditProjectWithSpecificNameViewModelTest : EditProjectViewModelTest
        {
            private const long otherWorkspaceId = DefaultWorkspaceId + 1;
            private const long projectId = 12345;

            protected ITestableObserver<bool> Observer { get; }

            protected string ProjectName { get; } = "A random project";

            protected EditProjectWithSpecificNameViewModelTest()
            {
                Observer = TestScheduler.CreateObserver<bool>();
            }

            protected void SetupDataSource(bool isFromSameWorkspace)
            {
                var project = Substitute.For<IThreadSafeProject>();
                project.Id.Returns(projectId);
                project.Name.Returns(ProjectName);
                project.WorkspaceId.Returns(isFromSameWorkspace ? DefaultWorkspaceId : otherWorkspaceId);

                var defaultWorkspace = Substitute.For<IThreadSafeWorkspace>();
                defaultWorkspace.Id.Returns(DefaultWorkspaceId);
                defaultWorkspace.Name.Returns(Guid.NewGuid().ToString());

                InteractorFactory
                    .GetDefaultWorkspace()
                    .Execute()
                    .Returns(Observable.Return(defaultWorkspace));

                InteractorFactory
                    .AreCustomColorsEnabledForWorkspace(DefaultWorkspaceId)
                    .Execute()
                    .Returns(Observable.Return(false));

                DataSource
                    .Projects
                    .GetAll(Arg.Any<ProjectPredicate>())
                    .Returns(callInfo => Observable
                        .Return(new[] { project })
                        .Select(projects => projects.Where<IThreadSafeProject>(callInfo.Arg<ProjectPredicate>())));
            }

            protected async Task TestChangeAfterWorkspaceChange(
                string projectName,
                IObservable<bool> testedObservable,
                bool result)
            {
                Observer.Messages.Clear();
                using (var disposable = testedObservable.Subscribe(Observer))
                {
                    setupChangingWorkspaceScenario();
                    NavigationService
                        .Navigate<SelectWorkspaceViewModel, long, long>(Arg.Any<long>())
                        .Returns(Task.FromResult(1L));
                    ViewModel.Prepare(projectName);
                    TestScheduler.Start();

                    await ViewModel.Initialize();
                    ViewModel.PickWorkspace.Execute();

                    TestScheduler.Start();
                    Observer.Messages.Last().Value.Value.Should().Be(result);
                }
            }

            private void setupChangingWorkspaceScenario()
            {
                List<IThreadSafeWorkspace> workspaces = new List<IThreadSafeWorkspace>();
                List<IThreadSafeProject> projects = new List<IThreadSafeProject>();

                for (long workspaceId = 0; workspaceId < 2; workspaceId++)
                {
                    var workspace = Substitute.For<IThreadSafeWorkspace>();
                    workspace.Id.Returns(workspaceId);
                    workspace.Name.Returns(Guid.NewGuid().ToString());
                    workspaces.Add(workspace);

                    InteractorFactory
                        .GetWorkspaceById(workspaceId)
                        .Execute()
                        .Returns(Observable.Return(workspace));

                    for (long id = 0; id < 3; id++)
                    {
                        var project = Substitute.For<IThreadSafeProject>();
                        project.Id.Returns(10 * workspaceId + id);
                        project.Name.Returns($"Project-{workspaceId}-{id}");
                        project.WorkspaceId.Returns(workspaceId);
                        projects.Add(project);
                    }

                    var sameNameProject = Substitute.For<IThreadSafeProject>();
                    sameNameProject.Id.Returns(10 + workspaceId);
                    sameNameProject.Name.Returns("Project");
                    sameNameProject.WorkspaceId.Returns(workspaceId);
                    projects.Add(sameNameProject);
                }

                var defaultWorkspace = workspaces[0];

                InteractorFactory
                    .GetDefaultWorkspace()
                    .Execute()
                    .Returns(Observable.Return(defaultWorkspace));

                InteractorFactory
                    .AreCustomColorsEnabledForWorkspace(Arg.Any<long>())
                    .Execute()
                    .Returns(Observable.Return(false));

                DataSource
                    .Projects
                    .GetAll(Arg.Any<ProjectPredicate>())
                    .Returns(callInfo =>
                        Observable
                            .Return(projects)
                            .Select(p => p
                                .Where<IThreadSafeProject>(callInfo.Arg<ProjectPredicate>())));

            }
        }

        public sealed class TheConstructor : EditProjectViewModelTest
        {
            [Theory, LogIfTooSlow]
            [ConstructorData]
            public void ThrowsIfAnyOfTheArgumentsIsNull(
                bool useDataSource,
                bool useDialogService,
                bool useRxActionFactory,
                bool useInteractorFactory,
                bool useNavigationService,
                bool useSchedulerProvider,
                bool useStopwatchProvider)
            {
                var dataSource = useDataSource ? DataSource : null;
                var dialogService = useDialogService ? DialogService : null;
                var rxActionFactory = useRxActionFactory ? RxActionFactory : null;
                var interactorFactory = useInteractorFactory ? InteractorFactory : null;
                var navigationService = useNavigationService ? NavigationService : null;
                var stopwatchProvider = useStopwatchProvider ? StopwatchProvider : null;
                var schedulerProvider = useSchedulerProvider ? SchedulerProvider : null;

                Action tryingToConstructWithEmptyParameters =
                    () => new EditProjectViewModel(
                        dataSource,
                        dialogService,
                        rxActionFactory,
                        interactorFactory,
                        schedulerProvider,
                        stopwatchProvider,
                        navigationService
                    );

                tryingToConstructWithEmptyParameters
                    .Should().Throw<ArgumentNullException>();
            }
        }

        public sealed class TheNameIsAlreadyTakenProperty : EditProjectWithSpecificNameViewModelTest
        {
            [Fact, LogIfTooSlow]
            public async Task IsTrueWhenAProjectWithSameNameAlreadyExistsInSameWorkspace()
            {
                SetupDataSource(isFromSameWorkspace: true);
                ViewModel.NameIsAlreadyTaken.Subscribe(Observer);
                ViewModel.Prepare(ProjectName);

                await ViewModel.Initialize();
                TestScheduler.Start();

                Observer.Messages.Last().Value.Value.Should().BeTrue();
            }

            [Fact, LogIfTooSlow]
            public async Task IsFalseWhenAProjectWithTheSameNameAlreadyExistsOnlyInAnotherWorkspace()
            {
                SetupDataSource(isFromSameWorkspace: false);
                ViewModel.NameIsAlreadyTaken.Subscribe(Observer);
                ViewModel.Prepare(ProjectName);

                await ViewModel.Initialize();
                TestScheduler.Start();

                Observer.Messages.Last().Value.Value.Should().BeFalse();
            }

            [Fact, LogIfTooSlow]
            public async Task IsCorrectAfterWorkspaceChangeWhenNameDoesNotExistInAny()
            {
                await TestChangeAfterWorkspaceChange(
                    "NotUsedProject",
                    ViewModel.NameIsAlreadyTaken,
                    result: false
                );
            }

            [Fact, LogIfTooSlow]
            public async Task IsCorrectAfterWorkspaceChangeWhenNameExistOnlyInDestinationWorkspace()
            {
                await TestChangeAfterWorkspaceChange(
                    "Project-1-1",
                    ViewModel.NameIsAlreadyTaken,
                    result: true
                );
            }

            [Fact, LogIfTooSlow]
            public async Task IsCorrectAfterWorkspaceChangeWhenNameExistOnlyInDefaultWorkspace()
            {
                await TestChangeAfterWorkspaceChange(
                    "Project-0-2",
                    ViewModel.NameIsAlreadyTaken,
                    result: false);
            }

            [Fact, LogIfTooSlow]
            public async Task IsCorrectAfterWorkspaceChangeWhenNameExistInBothWorkspaces()
            {
                await TestChangeAfterWorkspaceChange(
                    "Project",
                    ViewModel.NameIsAlreadyTaken,
                    result: true);
            }
        }

        public sealed class TheSaveEnabledProperty : EditProjectWithSpecificNameViewModelTest
        {
            public TheSaveEnabledProperty()
            {
                ViewModel.Save.Enabled.Subscribe(Observer);
                TestScheduler.Start();
            }

            [Fact, LogIfTooSlow]
            public void IsFalseWhenTheNameIsEmpty()
            {
                TestScheduler.Start();

                ViewModel.UpdateName.Execute("");

                Observer.Messages.Last().Value.Value.Should().Be(false);
            }

            [Fact, LogIfTooSlow]
            public void IsFalseWhenTheNameIsJustWhiteSpace()
            {
                TestScheduler.Start();
               
                ViewModel.UpdateName.Execute("            ");

                Observer.Messages.Last().Value.Value.Should().Be(false);
            }

            [Fact, LogIfTooSlow]
            public void IsFalseWhenTheNameIsLongerThanTheThresholdInBytes()
            {
                TestScheduler.Start();

                ViewModel.UpdateName.Execute("This is a ridiculously big project name made solely with the purpose of testing whether or not Toggl apps UI has validation logic that prevents such a large name to be persisted or, even worse, pushed to the api, an event that might end up in crashes and whatnot");

                Observer.Messages.Last().Value.Value.Should().Be(false);
            }

            [Fact, LogIfTooSlow]
            public async Task IsFalseWhenProjectWithSameNameAlreadyExistsInSameWorkspace()
            {
                SetupDataSource(isFromSameWorkspace: true);
                TestScheduler.Start();
                ViewModel.Prepare(ProjectName);

                await ViewModel.Initialize();

                Observer.Messages.Last().Value.Value.Should().Be(false);
            }

            [Fact, LogIfTooSlow]
            public async Task IsTrueWhenProjectWithSameNameAlreadyExistsOnlyInAnotherWorkspace()
            {
                SetupDataSource(isFromSameWorkspace: false);
                TestScheduler.Start();
                ViewModel.Prepare(ProjectName);

                await ViewModel.Initialize();

                Observer.Messages.Last().Value.Value.Should().Be(true);
            }

            [Fact, LogIfTooSlow]
            public async Task IsCorrectAfterWorkspaceChangeWhenNameDoesNotExistInAny()
            {
                TestScheduler.Start();

                await TestChangeAfterWorkspaceChange(
                    "NotUsedProject",
                    ViewModel.Save.Enabled,
                    result: true
                );
            }

            [Fact, LogIfTooSlow]
            public async Task IsCorrectAfterWorkspaceChangeWhenNameExistOnlyInDestinationWorkspace()
            {
                TestScheduler.Start();

                await TestChangeAfterWorkspaceChange(
                    "Project-1-1",
                    ViewModel.Save.Enabled,
                    result: false
                );
            }

            [Fact, LogIfTooSlow]
            public async Task IsCorrectAfterWorkspaceChangeWhenNameExistOnlyInDefaultWorkspace()
            {
                TestScheduler.Start();

                await TestChangeAfterWorkspaceChange(
                    "Project-0-2",
                    ViewModel.Save.Enabled,
                    result: true
                );
            }

            [Fact, LogIfTooSlow]
            public async Task IsCorrectAfterWorkspaceChangeWhenNameExistInBothWorkspaces()
            {
                TestScheduler.Start();

                await TestChangeAfterWorkspaceChange(
                    "Project",
                    ViewModel.Save.Enabled,
                    result: false
                );
            }
        }

        public sealed class TheInitializeMethod : EditProjectViewModelTest
        {
            private readonly ITestableObserver<string> workspaceObserver;

            public TheInitializeMethod()
            {
                workspaceObserver = TestScheduler.CreateObserver<string>();
                ViewModel.WorkspaceName.Subscribe(workspaceObserver);
            }

            [Fact, LogIfTooSlow]
            public async Task SetsTheWorkspaceId()
            {
                Workspace.Id.Returns(DefaultWorkspaceId);
                setupDefaultWorkspace();

                await ViewModel.Initialize();
                TestScheduler.Start();
                ViewModel.Save.Execute();

                await InteractorFactory
                    .Received()
                    .CreateProject(Arg.Is<CreateProjectDTO>(dto => dto.WorkspaceId == DefaultWorkspaceId))
                    .Execute();
            }

            [Fact, LogIfTooSlow]
            public async Task SetsTheWorkspaceName()
            {
                Workspace.Name.Returns(DefaultWorkspaceName);
                setupDefaultWorkspace();
                await ViewModel.Initialize();
                TestScheduler.Start();

                workspaceObserver.Messages.Last().Value.Value.Should().Be(DefaultWorkspaceName);
            }

            [Fact, LogIfTooSlow]
            public async Task SetsToTheFirstEligibleProjectIfDefaultIsNotEligible()
            {
                var defaultWorkspace = Substitute.For<IThreadSafeWorkspace>();
                defaultWorkspace.Name.Returns(DefaultWorkspaceName);
                defaultWorkspace.Admin.Returns(false);
                defaultWorkspace.OnlyAdminsMayCreateProjects.Returns(true);

                var eligibleWorkspace = Substitute.For<IThreadSafeWorkspace>();
                eligibleWorkspace.Name.Returns("Eligible workspace for project creation");
                eligibleWorkspace.Admin.Returns(true);

                InteractorFactory.GetDefaultWorkspace().Execute()
                    .Returns(Observable.Return(defaultWorkspace));
                InteractorFactory.GetAllWorkspaces().Execute()
                    .Returns(Observable.Return(new[] { defaultWorkspace, eligibleWorkspace }));

                await ViewModel.Initialize();
                TestScheduler.Start();

                workspaceObserver.Messages.Last().Value.Value.Should().Be(eligibleWorkspace.Name);
            }

            [Fact, LogIfTooSlow]
            public async Task SetToDefaultWorkspaceIfAllWorkspacesAreEligible()
            {
                var defaultWorkspace = Substitute.For<IThreadSafeWorkspace>();
                defaultWorkspace.Name.Returns(DefaultWorkspaceName);
                defaultWorkspace.Admin.Returns(true);

                var eligibleWorkspace = Substitute.For<IThreadSafeWorkspace>();
                eligibleWorkspace.Name.Returns("Eligible workspace for project creation");
                eligibleWorkspace.Admin.Returns(true);

                var eligibleWorkspace2 = Substitute.For<IThreadSafeWorkspace>();
                eligibleWorkspace.Name.Returns("Another Eligible Workspace");
                eligibleWorkspace.Admin.Returns(true);

                InteractorFactory.GetDefaultWorkspace().Execute()
                    .Returns(Observable.Return(defaultWorkspace));
                InteractorFactory.GetAllWorkspaces().Execute()
                    .Returns(Observable.Return(new[] { eligibleWorkspace2, defaultWorkspace, eligibleWorkspace }));

                await ViewModel.Initialize();
                TestScheduler.Start();

                workspaceObserver.Messages.Last().Value.Value.Should().Be(defaultWorkspace.Name);
            }

            private void setupDefaultWorkspace()
            {
                InteractorFactory
                    .GetDefaultWorkspace()
                    .Execute()
                    .Returns(Observable.Return(Workspace));

                ViewModel.Prepare("Some name");
            }
        }

        public sealed class TheCloseCommand : EditProjectViewModelTest
        {
            [Fact, LogIfTooSlow]
            public void ClosesTheViewModel()
            {
                ViewModel.Close.Execute();
                TestScheduler.Start();

                NavigationService.Received()
                    .Close(Arg.Is(ViewModel), Arg.Any<long?>());
            }

            [Fact, LogIfTooSlow]
            public void ReturnsNull()
            {
                ViewModel.Prepare("Some name");

                ViewModel.Close.Execute();
                TestScheduler.Start();

                NavigationService.Received()
                    .Close(Arg.Is(ViewModel), Arg.Is<long?>(result => result == null));
            }

            [Fact, LogIfTooSlow]
            public void DoesNotTrySavingTheChanges()
            {
                ViewModel.Prepare("Some name");

                ViewModel.Close.Execute();
                TestScheduler.Start();

                InteractorFactory.CreateProject(Arg.Any<CreateProjectDTO>()).DidNotReceive().Execute();
            }
        }

        public sealed class TheSaveCommand : EditProjectViewModelTest
        {
            private const long proWorkspaceId = 11;
            private const long projectId = 12;

            private readonly IThreadSafeProject project = Substitute.For<IThreadSafeProject>();

            public TheSaveCommand()
            {
                InteractorFactory
                    .AreCustomColorsEnabledForWorkspace(DefaultWorkspaceId)
                    .Execute()
                    .Returns(Observable.Return(false));

                InteractorFactory
                    .AreCustomColorsEnabledForWorkspace(proWorkspaceId)
                    .Execute()
                    .Returns(Observable.Return(true));

                InteractorFactory
                    .GetDefaultWorkspace()
                    .Execute()
                    .Returns(Observable.Return(Workspace));

                InteractorFactory
                    .GetWorkspaceById(Arg.Any<long>())
                    .Execute()
                    .Returns(Observable.Return(Workspace));

                InteractorFactory
                    .CreateProject(Arg.Any<CreateProjectDTO>())
                    .Execute()
                    .Returns(Observable.Return(project));

                project.Id.Returns(projectId);
                Workspace.Id.Returns(proWorkspaceId);
            }

            [Fact, LogIfTooSlow]
            public void ClosesTheViewModel()
            {
                ViewModel.Prepare("Some name");

                TestScheduler.Start();
                ViewModel.Save.Execute();

                NavigationService.Received()
                    .Close(Arg.Is(ViewModel), Arg.Any<long?>());
            }

            [Fact, LogIfTooSlow]
            public void ReturnsTheIdOfTheCreatedProject()
            {
                ViewModel.Prepare("Some name");

                TestScheduler.Start();
                ViewModel.Save.Execute();

                NavigationService.Received()
                    .Close(Arg.Is(ViewModel), Arg.Is(projectId));
            }

            [Fact, LogIfTooSlow]
            public void DoesNotCallCreateIfTheProjectNameIsInvalid()
            {
                ViewModel.Prepare("Some name");
                ViewModel.UpdateName.Execute("");

                TestScheduler.Start();
                ViewModel.Save.Execute();

                InteractorFactory
                    .DidNotReceive()
                    .CreateProject(Arg.Any<CreateProjectDTO>())
                    .Execute();
            }

            [Fact, LogIfTooSlow]
            public void DoesNotCloseTheViewModelIfTheProjectNameIsInvalid()
            {
                ViewModel.Prepare("Some name");
                ViewModel.UpdateName.Execute("");

                TestScheduler.Start();
                ViewModel.Save.Execute();

                NavigationService.DidNotReceive()
                    .Close(ViewModel, projectId);
            }

            [Theory, LogIfTooSlow]
            [InlineData("   abcde", "abcde")]
            [InlineData("abcde     ", "abcde")]
            [InlineData("  abcde ", "abcde")]
            [InlineData("abcde  fgh", "abcde  fgh")]
            [InlineData("      abcd\nefgh     ", "abcd\nefgh")]
            public async Task TrimsNameFromTheStartAndTheEndBeforeSaving(string name, string trimmed)
            {
                ViewModel.Prepare(name);
                await ViewModel.Initialize();

                TestScheduler.Start();
                ViewModel.Save.Execute();

                await InteractorFactory
                    .Received()
                    .CreateProject(Arg.Is<CreateProjectDTO>(dto => dto.Name == trimmed))
                    .Execute();
            }

            public sealed class WhenCreatingProjectInAnotherWorkspace : EditProjectViewModelTest
            {
                private const long defaultWorkspaceId = 101;
                private const long selectedWorkspaceId = 102;

                private void prepare()
                {
                    var defaultWorkspace = Substitute.For<IThreadSafeWorkspace>();
                    defaultWorkspace.Id.Returns(defaultWorkspaceId);
                    var selectedWorkspace = Substitute.For<IThreadSafeWorkspace>();
                    selectedWorkspace.Id.Returns(selectedWorkspaceId);
                    InteractorFactory
                        .GetDefaultWorkspace()
                        .Execute()
                        .Returns(Observable.Return(defaultWorkspace));
                    NavigationService
                       .Navigate<SelectWorkspaceViewModel, long, long>(Arg.Any<long>())
                       .Returns(Task.FromResult(selectedWorkspaceId));
                    ViewModel.Prepare("Some project");
                    ViewModel.Initialize().Wait();
                    ViewModel.PickWorkspace.Execute();
                }

                [Fact, LogIfTooSlow]
                public void AsksUserForConfirmationIfWorkspaceHasChanged()
                {
                    prepare();

                    TestScheduler.Start();
                    ViewModel.Save.Execute();

                    DialogService.Received().Confirm(
                        Arg.Is(Resources.WorkspaceChangedAlertTitle),
                        Arg.Is(Resources.WorkspaceChangedAlertMessage),
                        Arg.Is(Resources.Ok),
                        Arg.Is(Resources.Cancel)
                    );
                }

                [Fact, LogIfTooSlow]
                public void DoesNothingIfUserCancels()
                {
                    prepare();
                    DialogService
                        .Confirm(
                            Arg.Is(Resources.WorkspaceChangedAlertTitle),
                            Arg.Is(Resources.WorkspaceChangedAlertMessage),
                            Arg.Is(Resources.Ok),
                            Arg.Is(Resources.Cancel))
                        .Returns(Observable.Return(false));

                    TestScheduler.Start();
                    ViewModel.Save.Execute();

                    InteractorFactory.CreateProject(Arg.Any<CreateProjectDTO>()).DidNotReceive().Execute();
                    NavigationService.DidNotReceive().Close(Arg.Is(ViewModel), Arg.Any<long>());
                }

                [Fact, LogIfTooSlow]
                public void CreatesProjectInTheSelectedWorkspaceIfUserConfirms()
                {
                    prepare();
                    DialogService
                        .Confirm(
                            Arg.Is(Resources.WorkspaceChangedAlertTitle),
                            Arg.Is(Resources.WorkspaceChangedAlertMessage),
                            Arg.Is(Resources.Ok),
                            Arg.Is(Resources.Cancel))
                        .Returns(Observable.Return(true));

                    TestScheduler.Start();
                    ViewModel.Save.Execute();

                    InteractorFactory
                        .Received()
                        .CreateProject(Arg.Is<CreateProjectDTO>(dto => dto.WorkspaceId == selectedWorkspaceId))
                        .Execute();
                }

                [Fact, LogIfTooSlow]
                public void ClosesTheViewModelIfUserConfirms()
                {
                    prepare();
                    DialogService
                        .Confirm(
                            Arg.Is(Resources.WorkspaceChangedAlertTitle),
                            Arg.Is(Resources.WorkspaceChangedAlertMessage),
                            Arg.Is(Resources.Ok),
                            Arg.Is(Resources.Cancel))
                        .Returns(Observable.Return(true));

                    TestScheduler.Start();
                    ViewModel.Save.Execute();

                    NavigationService.Received().Close(Arg.Is(ViewModel), Arg.Any<long>());
                }
            }
        }

        public sealed class ThePickColorCommand : EditProjectViewModelTest
        {
            [Fact, LogIfTooSlow]
            public void CallsTheSelectColorViewModel()
            {
                ViewModel.Prepare("Some name");

                ViewModel.PickColor.Execute();
                TestScheduler.Start();

                NavigationService.Received()
                    .Navigate<SelectColorViewModel, ColorParameters, MvxColor>(Arg.Any<ColorParameters>());
            }

            [Fact, LogIfTooSlow]
            public void SetsTheReturnedColorAsTheColorProperty()
            {
                var observer = TestScheduler.CreateObserver<MvxColor>();
                var expectedColor = MvxColors.AliceBlue;
                using (var disposable = ViewModel.Color.Subscribe(observer))
                {
                    NavigationService
                            .Navigate<SelectColorViewModel, ColorParameters, MvxColor>(Arg.Any<ColorParameters>())
                            .Returns(Task.FromResult(MvxColors.AliceBlue));
                    ViewModel.Prepare("Some name");

                    ViewModel.PickColor.Execute();
                    TestScheduler.Start();

                    observer.Messages.Last().Value.Value.Should().Be(expectedColor);
                }
            }
        }

        public sealed class ThePickWorkspaceCommand : EditProjectViewModelTest
        {
            private const long workspaceId = 10;
            private const long defaultWorkspaceId = 11;
            private const string workspaceName = "My custom workspace";
            private readonly ITestableObserver<string> observer;
            private readonly MockWorkspace workspace = new MockWorkspace();
            private readonly MockWorkspace defaultWorkspace = new MockWorkspace();

            private IDisposable disposable;

            public ThePickWorkspaceCommand()
            {
                workspace.Id = workspaceId;
                workspace.Name = workspaceName;
                defaultWorkspace.Id = defaultWorkspaceId;

                observer = TestScheduler.CreateObserver<string>();

                InteractorFactory
                    .GetDefaultWorkspace()
                    .Execute()
                    .Returns(Observable.Return(defaultWorkspace));

                InteractorFactory
                    .GetWorkspaceById(workspaceId)
                    .Execute()
                    .Returns(Observable.Return(workspace));
            }

            [Fact, LogIfTooSlow]
            public void CallsTheSelectWorkspaceViewModel()
            {
                ViewModel.PickWorkspace.Execute();
                TestScheduler.Start();

                NavigationService.Received()
                    .Navigate<SelectWorkspaceViewModel, long, long>(Arg.Any<long>());
            }

            [Fact, LogIfTooSlow]
            public void SetsTheReturnedWorkspaceNameAsTheWorkspaceNameProperty()
            {
                disposable = ViewModel.WorkspaceName.Subscribe(observer);
                NavigationService
                    .Navigate<SelectWorkspaceViewModel, long, long>(Arg.Any<long>())
                    .Returns(Task.FromResult(workspaceId));

                ViewModel.PickWorkspace.Execute();
                TestScheduler.Start();

                observer.Messages.Last().Value.Value.Should().Be(workspaceName);
            }

            [Fact, LogIfTooSlow]
            public void ResetsTheClientNameWhenTheWorkspaceChanges()
            {
                disposable = ViewModel.ClientName.Subscribe(observer);
                NavigationService
                    .Navigate<SelectWorkspaceViewModel, long, long>(Arg.Any<long>())
                    .Returns(Task.FromResult(workspaceId));

                ViewModel.PickWorkspace.Execute();
                TestScheduler.Start();

                observer.Messages.Last().Value.Value.Should().BeNullOrEmpty();
            }

            [Fact, LogIfTooSlow]
            public void PicksADefaultColorIfTheSelectedColorIsCustomAndTheWorkspaceIsNotPro()
            {
                var colorObserver = TestScheduler.CreateObserver<MvxColor>();
                disposable = ViewModel.Color.Subscribe(colorObserver);
                NavigationService
                    .Navigate<SelectColorViewModel, ColorParameters, MvxColor>(Arg.Any<ColorParameters>())
                    .Returns(Task.FromResult(MvxColors.Azure));
                NavigationService
                    .Navigate<SelectWorkspaceViewModel, long, long>(Arg.Any<long>())
                    .Returns(Task.FromResult(workspaceId));
                InteractorFactory.AreCustomColorsEnabledForWorkspace(workspaceId).Execute()
                    .Returns(Observable.Return(false));

                Observable.Concat(
                    Observable.Defer(() => ViewModel.PickColor.Execute()),
                    Observable.Defer(() => ViewModel.PickWorkspace.Execute())
                );
                TestScheduler.Start();

                colorObserver.Messages.Last().Value.Value.Should().NotBe(MvxColors.Azure);
            }
        }

        public sealed class ThePickClientCommand : EditProjectViewModelTest
        {
            private readonly IDisposable disposable;
            private readonly ITestableObserver<string> observer;

            public ThePickClientCommand()
            {
                observer = TestScheduler.CreateObserver<string>();
                disposable = ViewModel.ClientName.Subscribe(observer);
            }

            [Fact, LogIfTooSlow]
            public void CallsTheSelectClientViewModel()
            {
                ViewModel.Prepare("Some name");

                ViewModel.PickClient.Execute();
                TestScheduler.Start();

                NavigationService.Received()
                    .Navigate<SelectClientViewModel, SelectClientParameters, long?>(Arg.Any<SelectClientParameters>());
            }

            [Fact, LogIfTooSlow]
            public async Task PassesTheCurrentWorkspaceToTheViewModel()
            {
                InteractorFactory
                    .GetDefaultWorkspace()
                    .Execute()
                    .Returns(Observable.Return(Workspace));
                Workspace.Id.Returns(DefaultWorkspaceId);
                ViewModel.Prepare("Some name");
                await ViewModel.Initialize();

                ViewModel.PickClient.Execute();
                TestScheduler.Start();

                await NavigationService.Received()
                    .Navigate<SelectClientViewModel, SelectClientParameters, long?>(
                        Arg.Is<SelectClientParameters>(parameter => parameter.WorkspaceId == DefaultWorkspaceId)
                    );
            }

            [Fact, LogIfTooSlow]
            public void SetsTheReturnedClientAsTheClientNameProperty()
            {
                const string expectedName = "Some client";
                long? expectedId = 10;
                var client = Substitute.For<IThreadSafeClient>();
                client.Id.Returns(expectedId.Value);
                client.Name.Returns(expectedName);
                NavigationService
                    .Navigate<SelectClientViewModel, SelectClientParameters, long?>(Arg.Any<SelectClientParameters>())
                    .Returns(Task.FromResult(expectedId));
                InteractorFactory
                    .GetDefaultWorkspace()
                    .Execute()
                    .Returns(Observable.Return(Workspace));
                InteractorFactory.GetClientById(expectedId.Value)
                    .Execute()
                    .Returns(Observable.Return(client));
                Workspace.Id.Returns(DefaultWorkspaceId);
                ViewModel.Prepare("Some name");

                ViewModel.PickClient.Execute();
                TestScheduler.Start();

                observer.Messages.Last().Value.Value.Should().Be(expectedName);
            }

            [Fact, LogIfTooSlow]
            public void ClearsTheCurrentClientIfZeroIsReturned()
            {
                const string expectedName = "Some client";
                long? expectedId = 10;
                var client = new MockClient { Id = expectedId.Value, Name = expectedName };
                NavigationService
                    .Navigate<SelectClientViewModel, SelectClientParameters, long?>(Arg.Any<SelectClientParameters>())
                    .Returns(Task.FromResult(expectedId), Task.FromResult<long?>(0));
                InteractorFactory
                    .GetDefaultWorkspace()
                    .Execute()
                    .Returns(Observable.Return(Workspace));
                InteractorFactory.GetClientById(expectedId.Value)
                    .Execute()
                    .Returns(Observable.Return(client));
                Workspace.Id.Returns(DefaultWorkspaceId);
                ViewModel.Prepare("Some name");

                Observable.Concat(
                    Observable.Defer(() => ViewModel.PickClient.Execute()),
                    Observable.Defer(() => ViewModel.PickClient.Execute())
                );
                TestScheduler.Start();

                observer.Messages.Last().Value.Value.Should().BeNullOrEmpty();
            }
        }
    }
}

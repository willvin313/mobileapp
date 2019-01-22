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
                bool before, 
                bool after)
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

                    await ViewModel.PickWorkspace.Execute();
                    TestScheduler.AdvanceBy(100);

                    Observer.Messages.AssertEqual(
                        ReactiveTest.OnNext(0, before),
                        ReactiveTest.OnNext(0, after)
                    );
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
                TestScheduler.Start();

                await ViewModel.Initialize();
                TestScheduler.AdvanceBy(100);

                Observer.Messages.Last().Value.Value.Should().BeTrue();
            }

            [Fact, LogIfTooSlow]
            public async Task IsFalseWhenAProjectWithTheSameNameAlreadyExistsOnlyInAnotherWorkspace()
            {
                SetupDataSource(isFromSameWorkspace: false);
                ViewModel.NameIsAlreadyTaken.Subscribe(Observer);
                ViewModel.Prepare(ProjectName);
                TestScheduler.Start();

                await ViewModel.Initialize();
                TestScheduler.AdvanceBy(100);

                Observer.Messages.Last().Value.Value.Should().BeFalse();
            }

            [Fact, LogIfTooSlow]
            public async Task IsCorrectAfterWorkspaceChangeWhenNameDoesNotExistInAny()
            {
                await TestChangeAfterWorkspaceChange("NotUsedProject",
                                                     ViewModel.NameIsAlreadyTaken,
                                                     before: false,
                                                     after: false);
            }

            [Fact, LogIfTooSlow]
            public async Task IsCorrectAfterWorkspaceChangeWhenNameExistOnlyInDestinationWorkspace()
            {
                await TestChangeAfterWorkspaceChange("Project-1-1",
                                                     ViewModel.NameIsAlreadyTaken,
                                                     before: false,
                                                     after: true);
            }

            [Fact, LogIfTooSlow]
            public async Task IsCorrectAfterWorkspaceChangeWhenNameExistOnlyInDefaultWorkspace()
            {
                await TestChangeAfterWorkspaceChange("Project-0-2",
                                                     ViewModel.NameIsAlreadyTaken,
                                                     before: true,
                                                     after: false);
            }

            [Fact, LogIfTooSlow]
            public async Task IsCorrectAfterWorkspaceChangeWhenNameExistInBothWorkspaces()
            {
                await TestChangeAfterWorkspaceChange("Project",
                                                     ViewModel.NameIsAlreadyTaken,
                                                     before: true,
                                                     after: true);
            }
        }

        public sealed class TheSaveEnabledProperty : EditProjectWithSpecificNameViewModelTest
        {
            public TheSaveEnabledProperty()
            {
                ViewModel.NameIsAlreadyTaken.Subscribe(Observer);
                TestScheduler.Start();
            }

            [Fact, LogIfTooSlow]
            public void IsFalseWhenTheNameIsEmpty()
            {
                ViewModel.UpdateName.Execute("");

                Observer.Messages.AssertEqual(
                    ReactiveTest.OnNext(0, false)
                );
            }

            [Fact, LogIfTooSlow]
            public void IsFalseWhenTheNameIsJustWhiteSpace()
            {
                ViewModel.UpdateName.Execute("            ");

                Observer.Messages.AssertEqual(
                    ReactiveTest.OnNext(0, false)
                );
            }

            [Fact, LogIfTooSlow]
            public void IsFalseWhenTheNameIsLongerThanTheThresholdInBytes()
            {
                ViewModel.UpdateName.Execute("This is a ridiculously big project name made solely with the purpose of testing whether or not Toggl apps UI has validation logic that prevents such a large name to be persisted or, even worse, pushed to the api, an event that might end up in crashes and whatnot");

                Observer.Messages.AssertEqual(
                    ReactiveTest.OnNext(0, false)
                );
            }

            [Fact, LogIfTooSlow]
            public async Task IsFalseWhenProjectWithSameNameAlreadyExistsInSameWorkspace()
            {
                SetupDataSource(isFromSameWorkspace: true);

                ViewModel.Prepare(ProjectName);

                await ViewModel.Initialize();

                Observer.Messages.AssertEqual(
                    ReactiveTest.OnNext(0, false)
                );
            }

            [Fact, LogIfTooSlow]
            public async Task IsTrueWhenProjectWithSameNameAlreadyExistsOnlyInAnotherWorkspace()
            {
                SetupDataSource(isFromSameWorkspace: false);

                ViewModel.Prepare(ProjectName);

                await ViewModel.Initialize();

                Observer.Messages.AssertEqual(
                    ReactiveTest.OnNext(0, true)
                );
            }

            [Fact, LogIfTooSlow]
            public async Task IsCorrectAfterWorkspaceChangeWhenNameDoesNotExistInAny()
            {
                await TestChangeAfterWorkspaceChange("NotUsedProject",
                                                     ViewModel.Save.Enabled,
                                                     before: true,
                                                     after: true);
            }

            [Fact, LogIfTooSlow]
            public async Task IsCorrectAfterWorkspaceChangeWhenNameExistOnlyInDestinationWorkspace()
            {
                await TestChangeAfterWorkspaceChange("Project-1-1",
                                                     ViewModel.Save.Enabled,
                                                     before: true,
                                                     after: false);
            }

            [Fact, LogIfTooSlow]
            public async Task IsCorrectAfterWorkspaceChangeWhenNameExistOnlyInDefaultWorkspace()
            {
                await TestChangeAfterWorkspaceChange("Project-0-2",
                                                     ViewModel.Save.Enabled,
                                                     before: false,
                                                     after: true);
            }

            [Fact, LogIfTooSlow]
            public async Task IsCorrectAfterWorkspaceChangeWhenNameExistInBothWorkspaces()
            {
                await TestChangeAfterWorkspaceChange("Project",
                                                     ViewModel.Save.Enabled,
                                                     before: false,
                                                     after: false);
            }
        }

        public sealed class TheInitializeMethod : EditProjectViewModelTest
        {
            private readonly ITestableObserver<string> workspaceObserver;

            public TheInitializeMethod()
            {
                workspaceObserver = TestScheduler.CreateObserver<string>();
                ViewModel.WorkspaceName.Subscribe(workspaceObserver);
                TestScheduler.Start();
            }

            [Fact, LogIfTooSlow]
            public async Task SetsTheWorkspaceId()
            {
                setupDefaultWorkspace();

                await ViewModel.Initialize();
                await ViewModel.Save.Execute();

                await InteractorFactory
                    .Received()
                    .CreateProject(Arg.Is<CreateProjectDTO>(dto => dto.WorkspaceId == DefaultWorkspaceId))
                    .Execute();
            }

            [Fact, LogIfTooSlow]
            public async Task SetsTheWorkspaceName()
            {
                setupDefaultWorkspace();
                await ViewModel.Initialize();
                TestScheduler.AdvanceBy(1);

                workspaceObserver.Messages.AssertEqual(
                    ReactiveTest.OnNext(0, DefaultWorkspaceName)
                );
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
                TestScheduler.AdvanceBy(1);

                workspaceObserver.Messages.AssertEqual(
                    ReactiveTest.OnNext(0, eligibleWorkspace.Name)
                );
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

                workspaceObserver.Messages.AssertEqual(
                    ReactiveTest.OnNext(0, defaultWorkspace.Name)
                );
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
            public async Task ClosesTheViewModel()
            {
                await ViewModel.Close.Execute();

                await NavigationService.Received()
                    .Close(Arg.Is(ViewModel), Arg.Any<long?>());
            }

            [Fact, LogIfTooSlow]
            public async Task ReturnsNull()
            {
                ViewModel.Prepare("Some name");

                await ViewModel.Close.Execute();

                await NavigationService.Received()
                    .Close(Arg.Is(ViewModel), Arg.Is<long?>(result => result == null));
            }

            [Fact, LogIfTooSlow]
            public async Task DoesNotTrySavingTheChanges()
            {
                ViewModel.Prepare("Some name");

                await ViewModel.Close.Execute();

                await InteractorFactory.CreateProject(Arg.Any<CreateProjectDTO>()).DidNotReceive().Execute();
            }
        }

        public sealed class TheDoneCommand : EditProjectViewModelTest
        {
            private const long proWorkspaceId = 11;
            private const long projectId = 12;

            private readonly IThreadSafeProject project = Substitute.For<IThreadSafeProject>();

            public TheDoneCommand()
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

                //ViewModel
            }

            [Fact, LogIfTooSlow]
            public async Task ClosesTheViewModel()
            {
                ViewModel.Prepare("Some name");

                await ViewModel.Save.Execute();

                await NavigationService.Received()
                    .Close(Arg.Is(ViewModel), Arg.Any<long?>());
            }

            [Fact, LogIfTooSlow]
            public async Task ReturnsTheIdOfTheCreatedProject()
            {
                ViewModel.Prepare("Some name");

                await ViewModel.Save.Execute();

                await NavigationService.Received()
                    .Close(Arg.Is(ViewModel), Arg.Is(projectId));
            }

            [Fact, LogIfTooSlow]
            public async Task DoesNotCallCreateIfTheProjectNameIsInvalid()
            {
                ViewModel.Prepare("Some name");
                await ViewModel.UpdateName.Execute("");

                await ViewModel.Save.Execute();

                await InteractorFactory
                    .DidNotReceive()
                    .CreateProject(Arg.Any<CreateProjectDTO>())
                    .Execute();
            }

            [Fact, LogIfTooSlow]
            public async Task DoesNotCloseTheViewModelIfTheProjectNameIsInvalid()
            {
                ViewModel.Prepare("Some name");
                await ViewModel.UpdateName.Execute("");

                await ViewModel.Save.Execute();

                await NavigationService.DidNotReceive()
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

                await ViewModel.Save.Execute();

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
                    ViewModel.PickWorkspace.Execute().Wait();
                }

                [Fact, LogIfTooSlow]
                public async Task AsksUserForConfirmationIfWorkspaceHasChanged()
                {
                    prepare();

                    await ViewModel.Save.Execute();

                    await DialogService.Received().Confirm(
                        Arg.Is(Resources.WorkspaceChangedAlertTitle),
                        Arg.Is(Resources.WorkspaceChangedAlertMessage),
                        Arg.Is(Resources.Ok),
                        Arg.Is(Resources.Cancel)
                    );
                }

                [Fact, LogIfTooSlow]
                public async Task DoesNothingIfUserCancels()
                {
                    prepare();
                    DialogService
                        .Confirm(
                            Arg.Is(Resources.WorkspaceChangedAlertTitle),
                            Arg.Is(Resources.WorkspaceChangedAlertMessage),
                            Arg.Is(Resources.Ok),
                            Arg.Is(Resources.Cancel))
                        .Returns(Observable.Return(false));

                    await ViewModel.Save.Execute();

                    await InteractorFactory.CreateProject(Arg.Any<CreateProjectDTO>()).DidNotReceive().Execute();
                    await NavigationService.DidNotReceive().Close(Arg.Is(ViewModel), Arg.Any<long>());
                }

                [Fact, LogIfTooSlow]
                public async Task CreatesProjectInTheSelectedWorkspaceIfUserConfirms()
                {
                    prepare();
                    DialogService
                        .Confirm(
                            Arg.Is(Resources.WorkspaceChangedAlertTitle),
                            Arg.Is(Resources.WorkspaceChangedAlertMessage),
                            Arg.Is(Resources.Ok),
                            Arg.Is(Resources.Cancel))
                        .Returns(Observable.Return(true));

                    await ViewModel.Save.Execute();

                    await InteractorFactory
                        .Received()
                        .CreateProject(Arg.Is<CreateProjectDTO>(dto => dto.WorkspaceId == selectedWorkspaceId))
                        .Execute();
                }

                [Fact, LogIfTooSlow]
                public async Task ClosesTheViewModelIfUserConfirms()
                {
                    prepare();
                    DialogService
                        .Confirm(
                            Arg.Is(Resources.WorkspaceChangedAlertTitle),
                            Arg.Is(Resources.WorkspaceChangedAlertMessage),
                            Arg.Is(Resources.Ok),
                            Arg.Is(Resources.Cancel))
                        .Returns(Observable.Return(true));

                    await ViewModel.Save.Execute();

                    await NavigationService.Received().Close(Arg.Is(ViewModel), Arg.Any<long>());
                }
            }
        }

        public sealed class ThePickColorCommand : EditProjectViewModelTest
        {
            [Fact, LogIfTooSlow]
            public async Task CallsTheSelectColorViewModel()
            {
                ViewModel.Prepare("Some name");

                await ViewModel.PickColor.Execute();

                await NavigationService.Received()
                    .Navigate<SelectColorViewModel, ColorParameters, MvxColor>(Arg.Any<ColorParameters>());
            }

            [Fact, LogIfTooSlow]
            public async Task SetsTheReturnedColorAsTheColorProperty()
            {
                var observer = TestScheduler.CreateObserver<MvxColor>();
                var expectedColor = MvxColors.AliceBlue;
                using (var disposable = ViewModel.Color.Subscribe(observer))
                {
                    NavigationService
                            .Navigate<SelectColorViewModel, ColorParameters, MvxColor>(Arg.Any<ColorParameters>())
                            .Returns(Task.FromResult(MvxColors.AliceBlue));
                    ViewModel.Prepare("Some name");

                    await ViewModel.PickColor.Execute();

                    observer.Messages.AssertEqual(
                        ReactiveTest.OnNext(0, expectedColor)
                    );
                }
            }
        }

        public sealed class ThePickWorkspaceCommand : EditProjectViewModelTest
        {
            private const long workspaceId = 10;
            private const long defaultWorkspaceId = 11;
            private const string workspaceName = "My custom workspace";
            private readonly IThreadSafeWorkspace workspace = Substitute.For<IThreadSafeWorkspace>();
            private readonly IThreadSafeWorkspace defaultWorkspace = Substitute.For<IThreadSafeWorkspace>();

            public ThePickWorkspaceCommand()
            {
                workspace.Id.Returns(workspaceId);
                workspace.Name.Returns(workspaceName);
                defaultWorkspace.Id.Returns(defaultWorkspaceId);

                InteractorFactory
                    .GetDefaultWorkspace()
                    .Execute()
                    .Returns(Observable.Return(defaultWorkspace));

                InteractorFactory
                    .GetWorkspaceById(workspaceId)
                    .Execute()
                    .Returns(Observable.Return(workspace));

                ViewModel.Prepare();
            }

            [Fact, LogIfTooSlow]
            public async Task CallsTheSelectWorkspaceViewModel()
            {
                await ViewModel.PickWorkspace.Execute();

                await NavigationService.Received()
                    .Navigate<SelectWorkspaceViewModel, long, long>(Arg.Any<long>());
            }

            [Fact, LogIfTooSlow]
            public async Task SetsTheReturnedWorkspaceNameAsTheWorkspaceNameProperty()
            {
                NavigationService
                    .Navigate<SelectWorkspaceViewModel, long, long>(Arg.Any<long>())
                    .Returns(Task.FromResult(workspaceId));

                await ViewModel.PickWorkspace.Execute();

                ViewModel.WorkspaceName.Should().Be(workspaceName);
            }

            [Fact, LogIfTooSlow]
            public async Task ResetsTheClientNameWhenTheWorkspaceChanges()
            {
                NavigationService
                    .Navigate<SelectWorkspaceViewModel, long, long>(Arg.Any<long>())
                    .Returns(Task.FromResult(workspaceId));

                await ViewModel.PickWorkspace.Execute();

                //ViewModel.ClientName.Should().BeNullOrEmpty();
            }

            [Fact, LogIfTooSlow]
            public async Task PicksADefaultColorIfTheSelectedColorIsCustomAndTheWorkspaceIsNotPro()
            {
                NavigationService
                    .Navigate<SelectColorViewModel, ColorParameters, MvxColor>(Arg.Any<ColorParameters>())
                    .Returns(Task.FromResult(MvxColors.Azure));
                NavigationService
                    .Navigate<SelectWorkspaceViewModel, long, long>(Arg.Any<long>())
                    .Returns(Task.FromResult(workspaceId));
                InteractorFactory.AreCustomColorsEnabledForWorkspace(workspaceId).Execute()
                    .Returns(Observable.Return(false));
                await ViewModel.PickColor.Execute();

                await ViewModel.PickWorkspace.Execute();

                ViewModel.Color.Should().NotBe(MvxColors.Azure);
            }
        }

        public sealed class ThePickClientCommand : EditProjectViewModelTest
        {
            [Fact, LogIfTooSlow]
            public async Task CallsTheSelectClientViewModel()
            {
                ViewModel.Prepare("Some name");

                await ViewModel.PickClient.Execute();

                await NavigationService.Received()
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

                await ViewModel.PickClient.Execute();

                await NavigationService.Received()
                    .Navigate<SelectClientViewModel, SelectClientParameters, long?>(
                        Arg.Is<SelectClientParameters>(parameter => parameter.WorkspaceId == DefaultWorkspaceId)
                    );
            }

            [Fact, LogIfTooSlow]
            public async Task SetsTheReturnedClientAsTheClientNameProperty()
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

                await ViewModel.PickClient.Execute();

                ViewModel.ClientName.Should().Be(expectedName);
            }

            [Fact, LogIfTooSlow]
            public async Task ClearsTheCurrentClientIfZeroIsReturned()
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
                await ViewModel.PickClient.Execute();
                NavigationService
                    .Navigate<SelectClientViewModel, SelectClientParameters, long?>(Arg.Any<SelectClientParameters>())
                    .Returns(Task.FromResult<long?>(0));

                await ViewModel.PickClient.Execute();

                //ViewModel.ClientName.Should().BeNullOrEmpty();
            }
        }
    }
}

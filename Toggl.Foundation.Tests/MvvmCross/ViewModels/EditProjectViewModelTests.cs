using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
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
using Toggl.Foundation.Tests.TestExtensions;
using Toggl.Multivac.Extensions;
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

        public abstract class WorkspaceChangeAwareTests : EditProjectViewModelTest
        {
            private const long otherWorkspaceId = DefaultWorkspaceId + 1;
            private const long projectId = 12345;

            protected string ProjectName { get; } = "A random project";

            protected void SetupDataSourceToReturnExistingProjectsAndDefaultWorkspace(bool dataSourceProjectIsInSameWorkspace)
            {
                var project = Substitute.For<IThreadSafeProject>();
                project.Id.Returns(projectId);
                project.Name.Returns(ProjectName);
                project.WorkspaceId.Returns(dataSourceProjectIsInSameWorkspace ? DefaultWorkspaceId : otherWorkspaceId);

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

                DataSource.Projects
                          .GetAll(Arg.Any<ProjectPredicate>())
                          .Returns(callInfo => Observable.Return(new[] { project })
                                                         .Select(projects => projects.Where<IThreadSafeProject>(callInfo.Arg<ProjectPredicate>())));
            }

            protected void SetupDataSourceToReturnMultipleWorkspaces()
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

                    for (long projectId = 0; projectId < 3; projectId++)
                    {
                        var project = Substitute.For<IThreadSafeProject>();
                        project.Id.Returns(10 * workspaceId + projectId);
                        project.Name.Returns($"Project-{workspaceId}-{projectId}");
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

                DataSource.Projects
                    .GetAll(Arg.Any<ProjectPredicate>())
                    .Returns(callInfo =>
                        Observable.Return(projects)
                            .Select(p => p.Where<IThreadSafeProject>(callInfo.Arg<ProjectPredicate>())));

                NavigationService
                    .Navigate<SelectWorkspaceViewModel, long, long>(Arg.Any<long>())
                    .Returns(Task.FromResult(1L));

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
                bool useSchedulerProvider,
                bool useNavigationService,
                bool useStopwatchProvider)
            {
                var dataSource = useDataSource ? DataSource : null;
                var dialogService = useDialogService ? DialogService : null;
                var rxActionFactory = useRxActionFactory ? RxActionFactory : null;
                var interactorFactory = useInteractorFactory ? InteractorFactory : null;
                var navigationService = useNavigationService ? NavigationService : null;
                var schedulerProvider = useSchedulerProvider ? SchedulerProvider : null;
                var stopwatchProvider = useStopwatchProvider ? StopwatchProvider : null;

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

        public sealed class TheNameIsTakenProperty : WorkspaceChangeAwareTests
        {
            private ITestableObserver<bool> isNameTakenObserver;

            public TheNameIsTakenProperty()
            {
                isNameTakenObserver = TestScheduler.CreateObserver<bool>();
            }

            [Theory, LogIfTooSlow]
            [InlineData(true)]
            [InlineData(false)]
            public void ReflectsWhetherOrNotAProjectWithTheSameNameExistsInTheSameWorkspace(bool configureForSameWorkspace)
            {
                SetupDataSourceToReturnExistingProjectsAndDefaultWorkspace(dataSourceProjectIsInSameWorkspace: configureForSameWorkspace);
                var viewModel = CreateViewModel();
                viewModel.NameIsAlreadyTaken.Subscribe(isNameTakenObserver);

                viewModel.Name.Accept(ProjectName);
                TestScheduler.Start();

                isNameTakenObserver.LastEmittedValue().Should().Be(configureForSameWorkspace);
            }

            [Theory, LogIfTooSlow]
            [InlineData("NotUsedProject", false)]
            [InlineData("Project-1-1", true)]
            [InlineData("Project-0-2", false)]
            [InlineData("Project", true)]
            public void TakesIntoAccountWorkspaceChanges(string projectName, bool nameIsTakenInWorkspace)
            {
                SetupDataSourceToReturnMultipleWorkspaces();
                var viewModel = CreateViewModel();
                viewModel.NameIsAlreadyTaken.Subscribe(isNameTakenObserver);

                viewModel.Name.Accept(projectName);
                TestScheduler.Start();
                viewModel.PickWorkspace.Execute();
                TestScheduler.Start();

                isNameTakenObserver.LastEmittedValue().Should().Be(nameIsTakenInWorkspace);
            }
        }

        public sealed class TheSaveEnabledProperty : WorkspaceChangeAwareTests
        {
            private ITestableObserver<bool> saveEnabledObserver;

            protected override void AdditionalViewModelSetup()
            {
                saveEnabledObserver = TestScheduler.CreateObserver<bool>();
                ViewModel.Name.Subscribe();
                ViewModel.Save.Enabled.Subscribe(saveEnabledObserver);
            }

            [Fact, LogIfTooSlow]
            public void IsFalseWhenTheNameIsEmpty()
            {
                TestScheduler.Start();
                ViewModel.Name.Accept("");
                TestScheduler.Start();

                saveEnabledObserver.LastEmittedValue().Should().BeFalse();
            }

            [Fact, LogIfTooSlow]
            public void IsFalseWhenTheNameIsJustWhiteSpace()
            {
                TestScheduler.Start();
                ViewModel.Name.Accept("            ");
                TestScheduler.Start();

                saveEnabledObserver.LastEmittedValue().Should().BeFalse();
            }

            [Fact, LogIfTooSlow]
            public void IsFalseWhenTheNameIsLongerThanTheThresholdInBytes()
            {
                TestScheduler.Start();
                ViewModel.Name.Accept("This is a ridiculously big project name made solely with the purpose of testing whether or not Toggl apps UI has validation logic that prevents such a large name to be persisted or, even worse, pushed to the api, an event that might end up in crashes and whatnot");
                TestScheduler.Start();

                saveEnabledObserver.LastEmittedValue().Should().BeFalse();
            }

            [Theory, LogIfTooSlow]
            [InlineData(true)]
            [InlineData(false)]
            public void ReflectsWhetherOrNotAProjectWithTheSameNameExistsInTheSameWorkspace(bool configureForSameWorkspace)
            {
                saveEnabledObserver = TestScheduler.CreateObserver<bool>();
                SetupDataSourceToReturnExistingProjectsAndDefaultWorkspace(dataSourceProjectIsInSameWorkspace: configureForSameWorkspace);
                var viewModel = CreateViewModel();
                viewModel.Save.Enabled.Subscribe(saveEnabledObserver);
                TestScheduler.Start();

                viewModel.Name.Accept(ProjectName);
                TestScheduler.Start();

                saveEnabledObserver.LastEmittedValue().Should().Be(!configureForSameWorkspace);
            }

            [Theory, LogIfTooSlow]
            [InlineData("NotUsedProject", true)]
            [InlineData("Project-1-1", false)]
            [InlineData("Project-0-2", true)]
            [InlineData("Project", false)]
            public void TakesIntoAccountWorkspaceChanges(string projectName, bool saveShouldBeEnabled)
            {
                saveEnabledObserver = TestScheduler.CreateObserver<bool>();
                SetupDataSourceToReturnMultipleWorkspaces();
                var viewModel = CreateViewModel();
                viewModel.Save.Enabled.Subscribe(saveEnabledObserver);
                TestScheduler.Start();

                viewModel.Name.Accept(projectName);
                TestScheduler.Start();
                viewModel.PickWorkspace.Execute();
                TestScheduler.Start();

                saveEnabledObserver.LastEmittedValue().Should().Be(saveShouldBeEnabled);
            }
        }

        public sealed class TheWorkspace : EditProjectViewModelTest
        {
            [Fact, LogIfTooSlow]
            public async Task SetsTheWorkspace()
            {
                InteractorFactory
                    .GetDefaultWorkspace()
                    .Execute()
                    .Returns(Observable.Return(Workspace));

                Workspace.Id.Returns(DefaultWorkspaceId);
                Workspace.Name.Returns(DefaultWorkspaceName);

                var viewModel = CreateViewModel();
                viewModel.Prepare("Some name");

                viewModel.Save.Execute();
                TestScheduler.Start();

                await InteractorFactory
                    .Received()
                    .CreateProject(Arg.Is<CreateProjectDTO>(dto => dto.WorkspaceId == DefaultWorkspaceId))
                    .Execute();
            }

            [Fact, LogIfTooSlow]
            public void IsSetToTheFirstEligibleForProjectCreationIfDefaultIsNotEligible()
            {
                var observer = TestScheduler.CreateObserver<string>();
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

                var viewModel = CreateViewModel();
                viewModel.WorkspaceName.Subscribe(observer);
                TestScheduler.Start();

                observer.LastEmittedValue().Should().Be(eligibleWorkspace.Name);
            }

            [Fact, LogIfTooSlow]
            public void IsSetToTheDefaultWorkspaceIfAllWorkspacesAreEligibleForProjectCreation()
            {
                var observer = TestScheduler.CreateObserver<string>();
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

                var viewModel = CreateViewModel();
                TestScheduler.Start();
                viewModel.WorkspaceName.Subscribe(observer);
                TestScheduler.Start();

                observer.LastEmittedValue().Should().Be(defaultWorkspace.Name);
            }
        }

        public sealed class TheCloseAction : EditProjectViewModelTest
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

        public sealed class TheDoneAction : EditProjectViewModelTest
        {
            private const long proWorkspaceId = 11;
            private const long projectId = 12;

            private readonly IThreadSafeProject project = Substitute.For<IThreadSafeProject>();

            public TheDoneAction()
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
            public async Task ReturnsTheIdOfTheCreatedProject()
            {
                ViewModel.Prepare("Some name");
                TestScheduler.Start();

                ViewModel.Save.Execute();
                TestScheduler.Start();

                await NavigationService.Received()
                    .Close(Arg.Is(ViewModel), Arg.Is(projectId));
            }

            [Fact, LogIfTooSlow]
            public void DoesNotCallCreateIfTheProjectNameIsInvalid()
            {
                ViewModel.Prepare("Some name");
                TestScheduler.Start();
                ViewModel.Name.Accept("");
                TestScheduler.Start();

                ViewModel.Save.Execute();
                TestScheduler.Start();

                InteractorFactory
                    .DidNotReceive()
                    .CreateProject(Arg.Any<CreateProjectDTO>())
                    .Execute();
            }

            [Fact, LogIfTooSlow]
            public void DoesNotCloseTheViewModelIfTheProjectNameIsInvalid()
            {
                ViewModel.Prepare("Some name");
                TestScheduler.Start();
                ViewModel.Name.Accept("");
                TestScheduler.Start();

                ViewModel.Save.Execute();
                TestScheduler.Start();

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

                ViewModel.Save.Execute();

                TestScheduler.Start();
                await InteractorFactory
                    .Received()
                    .CreateProject(Arg.Is<CreateProjectDTO>(dto => dto.Name == trimmed))
                    .Execute();
            }

            public sealed class WhenCreatingProjectInAnotherWorkspace : EditProjectViewModelTest
            {
                private const long defaultWorkspaceId = 101;
                private const long selectedWorkspaceId = 102;

                protected override void AdditionalSetup()
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
                }

                protected override void AdditionalViewModelSetup()
                {
                    TestScheduler.Start();
                    ViewModel.Prepare("Some project");
                    TestScheduler.Start();
                    ViewModel.WorkspaceName.Subscribe();
                    TestScheduler.Start();
                    ViewModel.PickWorkspace.Execute();
                    TestScheduler.Start();
                }

                [Fact, LogIfTooSlow]
                public void AsksUserForConfirmationIfWorkspaceHasChanged()
                {
                    ViewModel.Save.Execute();
                    TestScheduler.Start();

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
                    DialogService
                        .Confirm(
                            Arg.Is(Resources.WorkspaceChangedAlertTitle),
                            Arg.Is(Resources.WorkspaceChangedAlertMessage),
                            Arg.Is(Resources.Ok),
                            Arg.Is(Resources.Cancel))
                        .Returns(Observable.Return(false));

                    ViewModel.Save.Execute();
                    TestScheduler.Start();

                    InteractorFactory.CreateProject(Arg.Any<CreateProjectDTO>()).DidNotReceive().Execute();
                    NavigationService.DidNotReceive().Close(Arg.Is(ViewModel), Arg.Any<long>());
                }
            }
        }

        public sealed class ThePickColorAction : EditProjectViewModelTest
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
                var colorObserver = TestScheduler.CreateObserver<MvxColor>();
                var expectedColor = MvxColors.AliceBlue;
                NavigationService
                    .Navigate<SelectColorViewModel, ColorParameters, MvxColor>(Arg.Any<ColorParameters>())
                    .Returns(Task.FromResult(expectedColor));
                ViewModel.Color.Subscribe(colorObserver);

                ViewModel.PickColor.Execute();
                TestScheduler.Start();

                colorObserver.LastEmittedValue().ARGB.Should().Be(expectedColor.ARGB);
            }
        }

        public sealed class ThePickWorkspaceAction : EditProjectViewModelTest
        {
            private const long workspaceId = 10;
            private const long defaultWorkspaceId = 11;
            private const string workspaceName = "My custom workspace";
            private readonly IThreadSafeWorkspace workspace = Substitute.For<IThreadSafeWorkspace>();
            private readonly IThreadSafeWorkspace defaultWorkspace = Substitute.For<IThreadSafeWorkspace>();

            public ThePickWorkspaceAction()
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
                NavigationService
                    .Navigate<SelectWorkspaceViewModel, long, long>(Arg.Any<long>())
                    .Returns(Task.FromResult(workspaceId));
                TestScheduler.Start();
                var workspaceObserver = TestScheduler.CreateObserver<string>();
                ViewModel.WorkspaceName.Subscribe(workspaceObserver);

                ViewModel.PickWorkspace.Execute();
                TestScheduler.Start();

                workspaceObserver.LastEmittedValue().Should().Be(workspaceName);
            }

            [Fact, LogIfTooSlow]
            public void ResetsTheClientNameWhenTheWorkspaceChanges()
            {
                NavigationService
                    .Navigate<SelectWorkspaceViewModel, long, long>(Arg.Any<long>())
                    .Returns(Task.FromResult(workspaceId));
                var clientObserver = TestScheduler.CreateObserver<string>();
                ViewModel.ClientName.Subscribe(clientObserver);

                ViewModel.PickWorkspace.Execute();
                TestScheduler.Start();

                clientObserver.LastEmittedValue().Should().BeNullOrEmpty();
            }

            [Fact, LogIfTooSlow]
            public void PicksADefaultColorIfTheSelectedColorIsCustomAndTheWorkspaceIsNotPro()
            {
                NavigationService
                    .Navigate<SelectColorViewModel, ColorParameters, MvxColor>(Arg.Any<ColorParameters>())
                    .Returns(Task.FromResult(MvxColors.Azure));
                NavigationService
                    .Navigate<SelectWorkspaceViewModel, long, long>(Arg.Any<long>())
                    .Returns(Task.FromResult(workspaceId));
                InteractorFactory.AreCustomColorsEnabledForWorkspace(workspaceId).Execute()
                    .Returns(Observable.Return(false));
                ViewModel.PickColor.Execute();
                TestScheduler.Start();

                ViewModel.PickWorkspace.Execute();
                TestScheduler.Start();

                ViewModel.Color.Should().NotBe(MvxColors.Azure);
            }
        }

        public sealed class ThePickClientAction : EditProjectViewModelTest
        {
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
            public void PassesTheCurrentWorkspaceToTheViewModel()
            {
                InteractorFactory
                    .GetDefaultWorkspace()
                    .Execute()
                    .Returns(Observable.Return(Workspace));
                Workspace.Id.Returns(DefaultWorkspaceId);
                var viewModel = CreateViewModel();
                viewModel.Prepare("Some name");
                TestScheduler.Start();

                viewModel.PickClient.Execute();
                TestScheduler.Start();

                NavigationService.Received()
                    .Navigate<SelectClientViewModel, SelectClientParameters, long?>(
                        Arg.Is<SelectClientParameters>(parameter => parameter.WorkspaceId == DefaultWorkspaceId)
                    );
            }

            [Fact, LogIfTooSlow]
            public void SetsTheReturnedClientAsTheClientNameProperty()
            {
                var clientObserver = TestScheduler.CreateObserver<string>();
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
                ViewModel.ClientName.Subscribe(clientObserver);

                ViewModel.PickClient.Execute();
                TestScheduler.Start();

                clientObserver.LastEmittedValue().Should().Be(expectedName);
            }

            [Fact, LogIfTooSlow]
            public void ClearsTheCurrentClientIfZeroIsReturned()
            {
                var clientObserver = TestScheduler.CreateObserver<string>();
                const string expectedName = "Some client";
                long? expectedId = 10;
                var client = Substitute.For<IThreadSafeClient>();
                client.Id.Returns(expectedId.Value);
                client.Name.Returns(expectedName);
                NavigationService
                    .Navigate<SelectClientViewModel, SelectClientParameters, long?>(Arg.Any<SelectClientParameters>())
                    .Returns(Task.FromResult(expectedId), Task.FromResult<long?>(0));
                InteractorFactory.GetDefaultWorkspace().Execute().Returns(Observable.Return(Workspace));
                InteractorFactory.GetClientById(expectedId.Value).Execute().Returns(Observable.Return(client));
                Workspace.Id.Returns(DefaultWorkspaceId);
                ViewModel.Prepare("Some name");
                ViewModel.PickClient.Execute();
                TestScheduler.Start();
                ViewModel.ClientName.Subscribe(clientObserver);

                ViewModel.PickClient.Execute();
                TestScheduler.Start();

                clientObserver.LastEmittedValue().Should().BeNullOrEmpty();
            }
        }
    }
}

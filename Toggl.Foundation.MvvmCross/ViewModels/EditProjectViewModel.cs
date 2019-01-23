using System;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using MvvmCross.Navigation;
using MvvmCross.UI;
using MvvmCross.ViewModels;
using Toggl.Foundation.DataSources;
using Toggl.Foundation.Diagnostics;
using Toggl.Foundation.DTOs;
using Toggl.Foundation.Interactors;
using Toggl.Foundation.Models.Interfaces;
using Toggl.Foundation.MvvmCross.Extensions;
using Toggl.Foundation.MvvmCross.Helper;
using Toggl.Foundation.MvvmCross.Parameters;
using Toggl.Foundation.MvvmCross.Services;
using Toggl.Foundation.Services;
using Toggl.Multivac;
using Toggl.Multivac.Extensions;
using static Toggl.Foundation.Helper.Constants;
using static Toggl.Multivac.Extensions.StringExtensions;

namespace Toggl.Foundation.MvvmCross.ViewModels
{
    [Preserve(AllMembers = true)]
    public sealed class EditProjectViewModel : MvxViewModel<string, long?>
    {
        private static readonly Random random = new Random();

        private readonly ITogglDataSource dataSource;
        private readonly IDialogService dialogService;
        private readonly IInteractorFactory interactorFactory;
        private readonly IStopwatchProvider stopwatchProvider;
        private readonly IMvxNavigationService navigationService;
        private readonly CompositeDisposable disposeBag = new CompositeDisposable();
        private readonly BehaviorSubject<ProjectInformation> project =
            new BehaviorSubject<ProjectInformation>(ProjectInformation.Empty);

        private long initialWorkspaceId;
        private bool areCustomColorsEnabled;
        private IStopwatch navigationFromStartTimeEntryViewModelStopwatch;

        public string PageTitle { get; } = Resources.NewProject;
        public string DoneButtonText { get; } = Resources.Create;

        public IObservable<string> Name { get; }
        public IObservable<bool> IsPrivate { get; }
        public IObservable<MvxColor> Color { get; }
        public IObservable<string> ClientName { get; }
        public IObservable<string> TrimmedName { get; }
        public IObservable<string> WorkspaceName { get; }
        public IObservable<bool> NameIsAlreadyTaken { get; }

        public UIAction Save { get; }
        public UIAction Close { get; }
        public UIAction PickColor { get; }
        public UIAction PickClient { get; }
        public UIAction PickWorkspace { get; }
        public UIAction TogglePrivateProject { get; }
        public InputAction<string> UpdateName { get; }

        public EditProjectViewModel(
            ITogglDataSource dataSource,
            IDialogService dialogService,
            IRxActionFactory rxActionFactory,
            IInteractorFactory interactorFactory,
            ISchedulerProvider schedulerProvider,
            IStopwatchProvider stopwatchProvider,
            IMvxNavigationService navigationService)
        {
            Ensure.Argument.IsNotNull(dataSource, nameof(dataSource));
            Ensure.Argument.IsNotNull(dialogService, nameof(dialogService));
            Ensure.Argument.IsNotNull(rxActionFactory, nameof(rxActionFactory));
            Ensure.Argument.IsNotNull(interactorFactory, nameof(interactorFactory));
            Ensure.Argument.IsNotNull(navigationService, nameof(navigationService));
            Ensure.Argument.IsNotNull(schedulerProvider, nameof(schedulerProvider));
            Ensure.Argument.IsNotNull(stopwatchProvider, nameof(stopwatchProvider));

            this.dataSource = dataSource;
            this.dialogService = dialogService;
            this.navigationService = navigationService;
            this.stopwatchProvider = stopwatchProvider;
            this.interactorFactory = interactorFactory;

            Name = project
                .Select(p => p.Name)
                .DistinctUntilChanged()
                .AsDriver(schedulerProvider);

            IsPrivate = project
                .Select(p => p.IsPrivate)
                .DistinctUntilChanged()
                .AsDriver(schedulerProvider);

            Color = project
                .Select(p => p.Color)
                .DistinctUntilChanged()
                .AsDriver(schedulerProvider);

            ClientName = project
                .Select(p => p.ClientName)
                .DistinctUntilChanged()
                .AsDriver(schedulerProvider);

            TrimmedName = Name
                .Select(n => n.Trim())
                .DistinctUntilChanged()
                .AsDriver(schedulerProvider);

            WorkspaceName = project
                .Select(p => p.WorkspaceName)
                .DistinctUntilChanged();

            var workspaceId = project
                .Select(project => project.WorkspaceId)
                .DistinctUntilChanged();

            var projectsInWorkspace = workspaceId
                .SelectMany(id => dataSource.Projects
                    .GetAll(project => project.WorkspaceId == id)
                    .Select(projects => projects.Select(p => p.Name).ToHashSet()));

            NameIsAlreadyTaken = projectsInWorkspace
                .CombineLatest(TrimmedName, (projectNames, name) => projectNames.Contains(name))
                .DistinctUntilChanged()
                .AsDriver(schedulerProvider);

            var canSave = NameIsAlreadyTaken.Invert()
                .CombineLatest(Name.Select(nameIsValid), CommonFunctions.And);

            Close = rxActionFactory.FromAsync(close);
            Save = rxActionFactory.FromAsync(save, canSave);
            PickColor = rxActionFactory.FromAsync(pickColor);
            PickClient = rxActionFactory.FromAsync(pickClient);
            PickWorkspace = rxActionFactory.FromAsync(pickWorkspace);
            UpdateName = rxActionFactory.FromAction<string>(updateName);
            TogglePrivateProject = rxActionFactory.FromAction(togglePrivateProject);

            workspaceId
                .SkipWhileValueIsDefault()
                .SelectMany(id => interactorFactory.AreCustomColorsEnabledForWorkspace(id).Execute())
                .WithLatestFrom(Color, shouldPickNewRandomColor)
                .Where(CommonFunctions.Identity)
                .SelectUnit()
                .Subscribe(setRandomColor)
                .DisposedBy(disposeBag);

            bool shouldPickNewRandomColor(bool areCustomColorsEnabled, MvxColor color)
            {
                this.areCustomColorsEnabled = areCustomColorsEnabled;

                return !areCustomColorsEnabled && Array.IndexOf(Helper.Color.DefaultProjectColors, Color) < 0;
            }

            bool nameIsValid(string name)
                => !string.IsNullOrWhiteSpace(name)
                && name.LengthInBytes() <= MaxProjectNameLengthInBytes;

            void setRandomColor()
            {
                var randomColorIndex = random.Next(0, Helper.Color.DefaultProjectColors.Length);
                var color = Helper.Color.DefaultProjectColors[randomColorIndex];
                project.OnNext(p => p.WithColor(color));
            }
        }

        public override void Prepare(string parameter)
        {
            project.OnNext(p => p.WithName(parameter));
        }

        public override async Task Initialize()
        {
            navigationFromStartTimeEntryViewModelStopwatch = stopwatchProvider.Get(MeasuredOperation.OpenCreateProjectViewFromStartTimeEntryView);
            stopwatchProvider.Remove(MeasuredOperation.OpenCreateProjectViewFromStartTimeEntryView);

            var defaultWorkspace = await interactorFactory.GetDefaultWorkspace()
                .TrackException<InvalidOperationException, IThreadSafeWorkspace>("EditProjectViewModel.Initialize")
                .Execute();
            var allWorkspaces = await interactorFactory.GetAllWorkspaces().Execute();
            var workspace = defaultWorkspace.IsEligibleForProjectCreation()
                ? defaultWorkspace
                : allWorkspaces.First(ws => ws.IsEligibleForProjectCreation());

            initialWorkspaceId = workspace.Id;
            areCustomColorsEnabled = await interactorFactory.AreCustomColorsEnabledForWorkspace(workspace.Id).Execute();

            project.OnNext(p => p.WithWorkspace(workspace));
        }

        public override void ViewAppeared()
        {
            base.ViewAppeared();
            navigationFromStartTimeEntryViewModelStopwatch?.Stop();
            navigationFromStartTimeEntryViewModelStopwatch = null;
        }

        private async Task pickColor()
        {
            var color = await navigationService.Navigate<SelectColorViewModel, ColorParameters, MvxColor>(
                ColorParameters.Create(project.Value.Color, areCustomColorsEnabled));

            project.OnNext(p => p.WithColor(color));
        }

        private void togglePrivateProject()
        {
            project.OnNext(p => p.ToggleIsPrivate());
        }

        private async Task save()
        {
            if (initialWorkspaceId != project.Value.WorkspaceId)
            {
                var shouldContinue = await dialogService.Confirm(
                    Resources.WorkspaceChangedAlertTitle,
                    Resources.WorkspaceChangedAlertMessage,
                    Resources.Ok,
                    Resources.Cancel
                );

                if (!shouldContinue) return;
            }

            var dto = project.Value.ToDto();
            dto.Billable = await interactorFactory
                .AreProjectsBillableByDefault(project.Value.WorkspaceId)
                .Execute();

            var createdProject = await interactorFactory
                .CreateProject(dto)
                .Execute();

            await navigationService.Close(this, createdProject.Id);
        }

        private Task close()
            => navigationService.Close(this, null);

        private async Task pickWorkspace()
        {
            var selectedWorkspaceId =
                await navigationService
                    .Navigate<SelectWorkspaceViewModel, long, long>(project.Value.WorkspaceId);

            if (selectedWorkspaceId == project.Value.WorkspaceId) return;

            var workspace = await interactorFactory.GetWorkspaceById(selectedWorkspaceId).Execute();

            project.OnNext(p => p
                .WithClient(null)
                .WithWorkspace(workspace)
            );
        }

        private async Task pickClient()
        {
            var parameter = SelectClientParameters.WithIds(project.Value.WorkspaceId, project.Value.ClientId);
            var selectedClientId =
                await navigationService.Navigate<SelectClientViewModel, SelectClientParameters, long?>(parameter);
            if (selectedClientId == null) return;

            if (selectedClientId.Value == 0)
            {
                project.OnNext(project.Value.WithClient(null));
                return;
            }

            var client = await interactorFactory.GetClientById(selectedClientId.Value).Execute();
            project.OnNext(p => p.WithClient(client));
        }

        private void updateName(string name)
        {
            project.OnNext(p => p.WithName(name));
        }

        private class ProjectInformation
        {
            public long? ClientId { get; }
            public bool IsPrivate { get; }
            public long WorkspaceId { get; }
            public string Name { get; }
            public string ClientName { get; }
            public string WorkspaceName { get; }
            public MvxColor Color { get; }

            public static ProjectInformation Empty { get; } = new ProjectInformation(
                null, false, 0, "", "", "", MvxColors.Transparent
            );

            private ProjectInformation(
                long? clientId,
                bool isPrivate,
                long workspaceId,
                string name,
                string clientName,
                string workspaceName,
                MvxColor color)
            {
                Name = name;
                Color = color;
                ClientId = clientId;
                IsPrivate = isPrivate;
                ClientName = clientName;
                WorkspaceId = workspaceId;
                WorkspaceName = workspaceName;
            }

            public ProjectInformation WithName(string name)
                => new ProjectInformation(ClientId, IsPrivate, WorkspaceId, name, ClientName, WorkspaceName, Color);

            public ProjectInformation WithColor(MvxColor color)
                => new ProjectInformation(ClientId, IsPrivate, WorkspaceId, Name, ClientName, WorkspaceName, color);

            public ProjectInformation WithClient(IThreadSafeClient client)
                => new ProjectInformation(client?.Id, IsPrivate, WorkspaceId, Name, client?.Name ?? "", WorkspaceName, Color);

            public ProjectInformation WithWorkspace(IThreadSafeWorkspace workspace)
                => new ProjectInformation(ClientId, IsPrivate, workspace.Id, Name, ClientName, workspace.Name, Color);

            public ProjectInformation ToggleIsPrivate()
                => new ProjectInformation(ClientId, !IsPrivate, WorkspaceId, Name, ClientName, WorkspaceName, Color);

            public CreateProjectDTO ToDto() => new CreateProjectDTO
            {
                Name = Name.Trim(),
                Color = Color.ToHexString(),
                IsPrivate = IsPrivate,
                ClientId = ClientId,
                WorkspaceId = WorkspaceId
            };
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Threading.Tasks;
using MvvmCross.Navigation;
using MvvmCross.UI;
using MvvmCross.ViewModels;
using Toggl.Foundation.DataSources;
using Toggl.Foundation.Diagnostics;
using Toggl.Foundation.DTOs;
using Toggl.Foundation.Helper;
using Toggl.Foundation.Interactors;
using Toggl.Foundation.Models.Interfaces;
using Toggl.Foundation.MvvmCross.Extensions;
using Toggl.Foundation.MvvmCross.Helper;
using Toggl.Foundation.MvvmCross.Parameters;
using Toggl.Foundation.MvvmCross.Services;
using Toggl.Foundation.Services;
using Toggl.Multivac;
using Toggl.Multivac.Extensions;
using Toggl.Multivac.Extensions.Reactive;
using static Toggl.Foundation.Helper.Color;

namespace Toggl.Foundation.MvvmCross.ViewModels
{
    [Preserve(AllMembers = true)]
    public sealed class EditProjectViewModel : MvxViewModel<string, long?>
    {
        private const long noClientId = 0;

        private readonly Random random = new Random();
        private readonly IDialogService dialogService;
        private readonly IInteractorFactory interactorFactory;
        private readonly IStopwatchProvider stopwatchProvider;
        private readonly IMvxNavigationService navigationService;

        private long initialWorkspaceId;
        private IStopwatch navigationFromStartTimeEntryViewModelStopwatch;
        private readonly IObservable<IThreadSafeClient> currentClient;
        private readonly IObservable<IThreadSafeWorkspace> currentWorkspace;

        public string Title { get; } = Resources.NewProject;
        public string DoneButtonText { get; } = Resources.Create;

        public BehaviorRelay<string> Name { get; }
        public BehaviorRelay<bool> IsPrivate { get; }
        public IObservable<MvxColor> Color { get; }
        public IObservable<string> ClientName { get; }
        public IObservable<string> WorkspaceName { get; }
        public IObservable<bool> NameIsAlreadyTaken { get; }

        public UIAction Save { get; }
        public UIAction Close { get; }
        public OutputAction<MvxColor> PickColor { get; }
        public OutputAction<IThreadSafeClient> PickClient { get; }
        public OutputAction<IThreadSafeWorkspace> PickWorkspace { get; }

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

            this.dialogService = dialogService;
            this.navigationService = navigationService;
            this.stopwatchProvider = stopwatchProvider;
            this.interactorFactory = interactorFactory;

            Name = new BehaviorRelay<string>("");
            IsPrivate = new BehaviorRelay<bool>(false, CommonFunctions.Invert);

            Close = rxActionFactory.FromAsync(close);
            PickColor = rxActionFactory.FromObservable<MvxColor>(pickColor);
            PickClient = rxActionFactory.FromObservable<IThreadSafeClient>(pickClient);
            PickWorkspace = rxActionFactory.FromObservable<IThreadSafeWorkspace>(pickWorkspace);

            var initialWorkspaceObservable = interactorFactory
                .GetDefaultWorkspace()
                .TrackException<InvalidOperationException, IThreadSafeWorkspace>("EditProjectViewModel.Initialize")
                .Execute()
                .SelectMany(defaultWorkspaceOrWorkspaceEligibleForProjectCreation)
                .Do(initialWorkspace => initialWorkspaceId = initialWorkspace.Id);

            currentWorkspace = initialWorkspaceObservable
                .Merge(PickWorkspace.Elements)
                .ShareReplay(1);

            currentClient = currentWorkspace
                .SelectValue((IThreadSafeClient)null)
                .Merge(PickClient.Elements)
                .ShareReplay(1);

            WorkspaceName = currentWorkspace
                .Select(w => w.Name)
                .DistinctUntilChanged()
                .AsDriver(schedulerProvider);

            ClientName = currentClient
                .Select(client => client?.Name ?? "")
                .DistinctUntilChanged()
                .AsDriver(schedulerProvider);

            Color = PickColor.Elements
                .StartWith(getRandomColor())
                .Merge(currentWorkspace
                    .SelectMany(customColorIsEnabled)
                    .SelectMany(customColorsAreAvailable => customColorsAreAvailable
                        ? Observable.Empty<MvxColor>()
                        : Color.FirstAsync().Select(randomColorIfNotDefault)))
                .DistinctUntilChanged()
                .AsDriver(schedulerProvider);

            var nameIsAlreadyTaken = currentWorkspace
                .SelectMany(workspace => dataSource.Projects.GetAll(project => project.WorkspaceId == workspace.Id))
                .Select(existingProjectsDictionary)
                .CombineLatest(currentClient, Name, checkNameIsTaken)
                .DistinctUntilChanged();

            NameIsAlreadyTaken = nameIsAlreadyTaken
                .AsDriver(schedulerProvider);

            var saveEnabledObservable = nameIsAlreadyTaken
                .CombineLatest(Name, checkNameValidity);

            Save = rxActionFactory.FromObservable(done, saveEnabledObservable);

            IObservable<IThreadSafeWorkspace> defaultWorkspaceOrWorkspaceEligibleForProjectCreation(IThreadSafeWorkspace defaultWorkspace)
                => defaultWorkspace.IsEligibleForProjectCreation()
                    ? Observable.Return(defaultWorkspace)
                    : interactorFactory.GetAllWorkspaces().Execute()
                        .Select(allWorkspaces => allWorkspaces.First(ws => ws.IsEligibleForProjectCreation()));

            IObservable<bool> customColorIsEnabled(IThreadSafeWorkspace workspace)
                => interactorFactory
                    .AreCustomColorsEnabledForWorkspace(workspace.Id)
                    .Execute();

            MvxColor getRandomColor()
            {
                var randomColorIndex = random.Next(0, Helper.Color.DefaultProjectColors.Length);
                return Helper.Color.DefaultProjectColors[randomColorIndex];
            }

            MvxColor randomColorIfNotDefault(MvxColor lastColor)
            {
                var hex = lastColor.ToHexString();
                if (DefaultProjectColors.Any(defaultColor => defaultColor == hex))
                    return lastColor;

                return getRandomColor();
            }

            Dictionary<long, HashSet<string>> existingProjectsDictionary(IEnumerable<IThreadSafeProject> projectsInWorkspace)
                => projectsInWorkspace.Aggregate(new Dictionary<long, HashSet<string>>(), (dict, project) =>
                {
                    var key = project.ClientId ?? noClientId;
                    if (dict.ContainsKey(key))
                    {
                        dict[key].Add(project.Name);
                        return dict;
                    }

                    dict[key] = new HashSet<string> { project.Name };
                    return dict;
                });

            bool checkNameIsTaken(Dictionary<long, HashSet<string>> projectNameDictionary, IThreadSafeClient client, string name)
            {
                var key = client?.Id ?? noClientId;
                if (projectNameDictionary.TryGetValue(key, out var projectNames))
                    return projectNames.Contains(name.Trim());

                return false;
            }

            bool checkNameValidity(bool nameIsTaken, string name)
                => !nameIsTaken
                    && !string.IsNullOrWhiteSpace(name)
                    && name.LengthInBytes() <= Constants.MaxProjectNameLengthInBytes;
        }

        public override void Prepare(string parameter)
        {
            Name.Accept(parameter);

            navigationFromStartTimeEntryViewModelStopwatch = stopwatchProvider.Get(MeasuredOperation.OpenCreateProjectViewFromStartTimeEntryView);
            stopwatchProvider.Remove(MeasuredOperation.OpenCreateProjectViewFromStartTimeEntryView);
        }

        public override void ViewAppeared()
        {
            base.ViewAppeared();
            navigationFromStartTimeEntryViewModelStopwatch?.Stop();
            navigationFromStartTimeEntryViewModelStopwatch = null;
        }

        private Task close()
            => navigationService.Close(this, null);

        private IObservable<IThreadSafeWorkspace> pickWorkspace()
        {
            return currentWorkspace.FirstAsync().SelectMany(workspaceFromViewModel);

            IObservable<IThreadSafeWorkspace> workspaceFromViewModel(IThreadSafeWorkspace currentWorkspace)
                => navigationService
                    .Navigate<SelectWorkspaceViewModel, long, long>(currentWorkspace.Id)
                    .ToObservable()
                    .SelectMany(selectedWorkspaceId => workspaceFromId(selectedWorkspaceId, currentWorkspace));

            IObservable<IThreadSafeWorkspace> workspaceFromId(long selectedWorkspaceId, IThreadSafeWorkspace currentWorkspace)
                => selectedWorkspaceId == currentWorkspace.Id
                    ? Observable.Empty<IThreadSafeWorkspace>()
                    : interactorFactory
                        .GetWorkspaceById(selectedWorkspaceId)
                        .Execute();
        }

        private IObservable<IThreadSafeClient> pickClient()
        {
            return currentWorkspace.FirstAsync()
                .SelectMany(currentWorkspace => currentClient.FirstAsync()
                    .SelectMany(currentClient => clientFromViewModel(currentClient, currentWorkspace)));

            IObservable<IThreadSafeClient> clientFromViewModel(IThreadSafeClient currentClient, IThreadSafeWorkspace currentWorkspace)
                => navigationService
                    .Navigate<SelectClientViewModel, SelectClientParameters, long?>(
                        SelectClientParameters.WithIds(currentWorkspace.Id, currentClient?.Id))
                    .ToObservable()
                    .SelectMany(clientFromId);

            IObservable<IThreadSafeClient> clientFromId(long? selectedClientId)
            {
                if (selectedClientId == null)
                    return Observable.Empty<IThreadSafeClient>();

                if (selectedClientId.Value == 0)
                    return Observable.Return<IThreadSafeClient>(null);

                return interactorFactory.GetClientById(selectedClientId.Value).Execute();
            }
        }

        private IObservable<MvxColor> pickColor()
        {
            return currentWorkspace.FirstAsync()
                .SelectMany(currentWorkspace => interactorFactory
                    .AreCustomColorsEnabledForWorkspace(currentWorkspace.Id).Execute()
                    .SelectMany(areCustomColorsEnabled => Color.FirstAsync()
                        .SelectMany(currentColor =>
                            colorFromViewmodel(currentColor, areCustomColorsEnabled))));

            IObservable<MvxColor> colorFromViewmodel(MvxColor currentColor, bool areCustomColorsEnabled)
                => navigationService
                    .Navigate<SelectColorViewModel, ColorParameters, MvxColor>(
                        ColorParameters.Create(currentColor, areCustomColorsEnabled))
                    .ToObservable();
        }

        private IObservable<Unit> done()
        {
            return currentWorkspace.FirstAsync()
                .SelectMany(workspace => checkIfCanContinue(workspace)
                    .SelectMany(shouldContinue => !shouldContinue
                        ? Observable.Empty<Unit>()
                        : getDto(workspace)
                            .SelectMany(dto => interactorFactory.CreateProject(dto).Execute())
                            .SelectMany(createdProject =>
                                navigationService.Close(this, createdProject.Id).ToObservable())
                            .SelectUnit()
                )
            );

            IObservable<bool> checkIfCanContinue(IThreadSafeWorkspace workspace)
            {
                if (initialWorkspaceId == workspace.Id)
                    return Observable.Return(true);

                return dialogService.Confirm(
                    Resources.WorkspaceChangedAlertTitle,
                    Resources.WorkspaceChangedAlertMessage,
                    Resources.Ok,
                    Resources.Cancel
                );
            }

            IObservable<CreateProjectDTO> getDto(IThreadSafeWorkspace workspace)
                => Observable.CombineLatest(
                    Name.FirstAsync(),
                    Color.FirstAsync(),
                    currentClient.FirstAsync(),
                    interactorFactory.AreProjectsBillableByDefault(workspace.Id).Execute(),
                    IsPrivate.FirstAsync(),
                    (name, color, client, billable, isPrivate) => new CreateProjectDTO
                    {
                        Name = name.Trim(),
                        Color = color.ToHexString(),
                        IsPrivate = isPrivate,
                        ClientId = client?.Id,
                        Billable = billable,
                        WorkspaceId = workspace.Id
                    }
                );
        }
    }
}

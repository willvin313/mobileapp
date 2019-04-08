﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using MvvmCross.Navigation;
using MvvmCross.ViewModels;
using Toggl.Foundation.Autocomplete.Suggestions;
using Toggl.Foundation.DataSources;
using Toggl.Foundation.Diagnostics;
using Toggl.Foundation.Extensions;
using Toggl.Foundation.Interactors;
using Toggl.Foundation.UI.Collections;
using Toggl.Foundation.UI.Extensions;
using Toggl.Foundation.UI.Parameters;
using Toggl.Foundation.UI.Services;
using Toggl.Foundation.Services;
using Toggl.Multivac;
using Toggl.Multivac.Extensions;
using static Toggl.Foundation.Helper.Constants;

namespace Toggl.Foundation.UI.ViewModels
{
    [Preserve(AllMembers = true)]
    public sealed class SelectProjectViewModel
        : MvxViewModel<SelectProjectParameter, SelectProjectParameter>
    {
        private readonly ITogglDataSource dataSource;
        private readonly IDialogService dialogService;
        private readonly IInteractorFactory interactorFactory;
        private readonly IMvxNavigationService navigationService;
        private readonly ISchedulerProvider schedulerProvider;
        private readonly IStopwatchProvider stopwatchProvider;

        private long? taskId;
        private long? projectId;
        private long workspaceId;
        private IStopwatch navigationFromEditTimeEntryViewModelStopwatch;

        private bool projectCreationSuggestionsAreEnabled;

        public bool UseGrouping { get; private set; }

        private BehaviorSubject<IList<SectionModel<string, AutocompleteSuggestion>>> suggestionsSubject
            = new BehaviorSubject<IList<SectionModel<string, AutocompleteSuggestion>>>(new SectionModel<string, AutocompleteSuggestion>[0]);
        public IObservable<IList<SectionModel<string, AutocompleteSuggestion>>> Suggestions => suggestionsSubject.AsObservable();

        public ISubject<string> FilterText { get; } = new BehaviorSubject<string>(string.Empty);

        public IObservable<bool> IsEmpty { get; }

        public IObservable<string> PlaceholderText { get; }

        public UIAction Close { get; }

        public InputAction<ProjectSuggestion> ToggleTaskSuggestions { get; }

        public InputAction<AutocompleteSuggestion> SelectProject { get; }

        public SelectProjectViewModel(
            ITogglDataSource dataSource,
            IRxActionFactory rxActionFactory,
            IInteractorFactory interactorFactory,
            IMvxNavigationService navigationService,
            IDialogService dialogService,
            ISchedulerProvider schedulerProvider,
            IStopwatchProvider stopwatchProvider)
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
            this.interactorFactory = interactorFactory;
            this.navigationService = navigationService;
            this.schedulerProvider = schedulerProvider;
            this.stopwatchProvider = stopwatchProvider;

            Close = rxActionFactory.FromAsync(close);
            ToggleTaskSuggestions = rxActionFactory.FromAction<ProjectSuggestion>(toggleTaskSuggestions);
            SelectProject = rxActionFactory.FromAsync<AutocompleteSuggestion>(selectProject);

            IsEmpty = dataSource.Projects.GetAll().Select(projects => projects.None());
            PlaceholderText = IsEmpty.Select(isEmpty => isEmpty ? Resources.EnterProject : Resources.AddFilterProjects);
        }

        private bool shouldSuggestCreation(string text)
        {
            if (!projectCreationSuggestionsAreEnabled)
                return false;

            text = text.Trim();

            if (string.IsNullOrEmpty(text))
                return false;

            var isOfAllowedLength = text.LengthInBytes() <= MaxProjectNameLengthInBytes;
            if (!isOfAllowedLength)
                return false;

            return true;
        }

        public override void Prepare(SelectProjectParameter parameter)
        {
            taskId = parameter.TaskId;
            projectId = parameter.ProjectId;
            workspaceId = parameter.WorkspaceId;
        }

        public override async Task Initialize()
        {
            await base.Initialize();
            navigationFromEditTimeEntryViewModelStopwatch = stopwatchProvider.Get(MeasuredOperation.OpenSelectProjectFromEditView);
            stopwatchProvider.Remove(MeasuredOperation.OpenSelectProjectFromEditView);

            var workspaces = await interactorFactory.GetAllWorkspaces().Execute();

            projectCreationSuggestionsAreEnabled = workspaces.Any(ws => ws.IsEligibleForProjectCreation());
            UseGrouping = workspaces.Count() > 1;

            FilterText.Subscribe(async text =>
            {
                var suggestions = interactorFactory.GetProjectsAutocompleteSuggestions(text.SplitToQueryWords()).Execute().SelectMany(x => x).ToEnumerable()
                    .Cast<ProjectSuggestion>()
                    .Select(setSelectedProject);

                var collectionSections = suggestions
                    .GroupBy(project => project.WorkspaceId)
                    .Select(grouping => grouping.OrderBy(projectSuggestion => projectSuggestion.ProjectName))
                    .OrderBy(grouping => grouping.First().WorkspaceName)
                    .Select(grouping => collectionSection(grouping, prependNoProject: string.IsNullOrEmpty(text)))
                    .ToList();

                if (shouldSuggestCreation(text))
                {
                    var createEntitySuggestion = new CreateEntitySuggestion(Resources.CreateProject, text);
                    var section = new SectionModel<string, AutocompleteSuggestion>(null, new[] { createEntitySuggestion });
                    collectionSections.Insert(0, section);
                }

                if (collectionSections.None())
                {
                    var workspace = await interactorFactory.GetWorkspaceById(workspaceId).Execute();
                    var noProjectSuggestion = ProjectSuggestion.NoProject(workspace.Id, workspace.Name);
                    collectionSections.Add(
                        new SectionModel<string, AutocompleteSuggestion>(null, new[] { noProjectSuggestion })
                    );
                }

                suggestionsSubject.OnNext(collectionSections);
            });
        }

        public override void ViewAppeared()
        {
            base.ViewAppeared();
            navigationFromEditTimeEntryViewModelStopwatch?.Stop();
            navigationFromEditTimeEntryViewModelStopwatch = null;
        }

        private SectionModel<string, AutocompleteSuggestion> collectionSection(IEnumerable<ProjectSuggestion> suggestions, bool prependNoProject)
        {
            var workspaceName = suggestions.First().WorkspaceName;
            var sectionItems = suggestions.ToList();

            if (prependNoProject)
            {
                var workspaceIdForNoProject = suggestions.First().WorkspaceId;
                var noProjectSuggestion = ProjectSuggestion.NoProject(workspaceIdForNoProject, workspaceName);
                sectionItems.Insert(0, noProjectSuggestion);
            }

            return new SectionModel<string, AutocompleteSuggestion>(workspaceName, sectionItems);
        }

        private ProjectSuggestion setSelectedProject(ProjectSuggestion suggestion)
        {
            suggestion.Selected = suggestion.ProjectId == projectId;
            return suggestion; 
        }

        private async Task createProject(string name)
        {
            var createdProjectId = await navigationService.Navigate<EditProjectViewModel, string, long?>(name);
            if (createdProjectId == null) return;

            var project = await interactorFactory.GetProjectById(createdProjectId.Value).Execute();
            var parameter = SelectProjectParameter.WithIds(project.Id, null, project.WorkspaceId);
            await navigationService.Close(this, parameter);
        }

        private Task close()
            => navigationService.Close(
                this,
                SelectProjectParameter.WithIds(projectId, taskId, workspaceId));

        private async Task selectProject(AutocompleteSuggestion suggestion)
        {
            if (suggestion is CreateEntitySuggestion createEntitySuggestion)
            {
                await createProject(createEntitySuggestion.EntityName);
                return;
            }

            if (suggestion.WorkspaceId == workspaceId || suggestion.WorkspaceId == 0)
            {
                setProject(suggestion);
                return;
            }

            var shouldSetProject = await dialogService.Confirm(
                Resources.DifferentWorkspaceAlertTitle,
                Resources.DifferentWorkspaceAlertMessage,
                Resources.Ok,
                Resources.Cancel
            );

            if (!shouldSetProject) return;

            setProject(suggestion);
        }

        private void setProject(AutocompleteSuggestion suggestion)
        {
            workspaceId = suggestion.WorkspaceId;
            switch (suggestion)
            {
                case ProjectSuggestion projectSuggestion:
                    projectId = projectSuggestion
                        .ProjectId == 0 ? null : (long?)projectSuggestion.ProjectId;
                    taskId = null;
                    break;

                case TaskSuggestion taskSuggestion:
                    projectId = taskSuggestion.ProjectId;
                    taskId = taskSuggestion.TaskId;
                    break;

                default:
                    throw new ArgumentException($"{nameof(suggestion)} must be either of type {nameof(ProjectSuggestion)} or {nameof(TaskSuggestion)}.");
            }

            navigationService.Close(
                this,
                SelectProjectParameter.WithIds(projectId, taskId, workspaceId));
        }

        private void toggleTaskSuggestions(ProjectSuggestion projectSuggestion)
        {
            if (projectSuggestion.TasksVisible)
                removeTasksFor(projectSuggestion);
            else
                insertTasksFor(projectSuggestion);

            projectSuggestion.TasksVisible = !projectSuggestion.TasksVisible;
        }

        private void insertTasksFor(ProjectSuggestion projectSuggestion)
        {
            var indexOfTargetSection = suggestionsSubject.Value.IndexOf(section => section.Header == projectSuggestion.WorkspaceName);
            if (indexOfTargetSection < 0) return;
            var targetSection = suggestionsSubject.Value.ElementAt(indexOfTargetSection);

            var indexOfSuggestion = targetSection.Items.IndexOf(project => project == projectSuggestion);
            if (indexOfSuggestion < 0) return;
            var newItemsInSection = targetSection.Items.InsertRange(indexOfSuggestion + 1, projectSuggestion.Tasks.OrderBy(task => task.Name));

            var newSection = new SectionModel<string, AutocompleteSuggestion>(targetSection.Header, newItemsInSection);
            var newSuggestions = suggestionsSubject.Value.ToList();
            newSuggestions[indexOfTargetSection] = newSection;

            suggestionsSubject.OnNext(newSuggestions);
        }

        private void removeTasksFor(ProjectSuggestion projectSuggestion)
        {
            var indexOfTargetSection = suggestionsSubject.Value.IndexOf(section => section.Items.Contains(projectSuggestion));
            if (indexOfTargetSection < 0) return;

            var targetSection = suggestionsSubject.Value.ElementAt(indexOfTargetSection);
            var newItemsInSection = targetSection.Items.ToList();
            foreach (var task in projectSuggestion.Tasks)
                newItemsInSection.Remove(task);

            var newSection = new SectionModel<string, AutocompleteSuggestion>(targetSection.Header, newItemsInSection);
            var newSuggestions = suggestionsSubject.Value.ToList();
            newSuggestions[indexOfTargetSection] = newSection;

            suggestionsSubject.OnNext(newSuggestions);
        }
    }
}

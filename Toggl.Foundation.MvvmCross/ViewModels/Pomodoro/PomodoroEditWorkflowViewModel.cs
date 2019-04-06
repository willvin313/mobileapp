using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using Toggl.Foundation.Models.Pomodoro;
using Toggl.Foundation.Services;
using Toggl.Multivac;
using Toggl.Multivac.Extensions;
using Toggl.PrimeRadiant.Settings;
using MvvmCross.Navigation;
using MvvmCross.ViewModels;
using System.Reactive.Linq;
using Toggl.Foundation.MvvmCross.Extensions;
using Math = System.Math;
using Toggl.Foundation.Extensions;

namespace Toggl.Foundation.MvvmCross.ViewModels
{
    [Preserve(AllMembers = true)]
    public sealed class PomodoroEditWorkflowViewModel : MvxViewModel<PomodoroWorkflow, PomodoroWorkflow>
    {
        private readonly IPomodoroStorage pomodoroStorage;
        private readonly IMvxNavigationService navigationService;
        private readonly ISchedulerProvider schedulerProvider;
        private readonly IRxActionFactory rxActionFactory;

        private int selectedItemIndex;

        public PomodoroWorkflow Workflow { get; private set; }

        private BehaviorSubject<IReadOnlyList<PomodoroWorkflowItem>> workflowItemsSubject;
        public IObservable<IReadOnlyList<PomodoroWorkflowItem>> WorkflowItems { get; private set; }

        private BehaviorSubject<int> selectedWorkflowItemIndexSubject
            = new BehaviorSubject<int>(0);
        public IObservable<int> SelectedWorkflowItemIndex { get; private set; }

        private BehaviorSubject<PomodoroWorkflowItem?> selectedWorkflowItemSubject
            = new BehaviorSubject<PomodoroWorkflowItem?>(null);
        public IObservable<PomodoroWorkflowItem?> SelectedWorkflowItem { get; private set; }

        public UIAction Close { get; }
        public UIAction AddWorkflowItem { get; }
        public UIAction DeleteWorkflowItem { get; }
        public InputAction<int> SelectItemByIndex { get; private set; }
        public InputAction<int> UpdateDuration { get; private set; }
        public InputAction<PomodoroWorkflowItemType> UpdateCurrentWorkflowItemType { get; private set; }

        public PomodoroEditWorkflowViewModel(
            IMvxNavigationService navigationService,
            IPomodoroStorage pomodoroStorage,
            ISchedulerProvider schedulerProvider,
            IRxActionFactory rxActionFactory)
        {
            Ensure.Argument.IsNotNull(navigationService, nameof(navigationService));
            Ensure.Argument.IsNotNull(pomodoroStorage, nameof(pomodoroStorage));
            Ensure.Argument.IsNotNull(schedulerProvider, nameof(schedulerProvider));
            Ensure.Argument.IsNotNull(rxActionFactory, nameof(rxActionFactory));

            this.navigationService = navigationService;
            this.pomodoroStorage = pomodoroStorage;
            this.schedulerProvider = schedulerProvider;
            this.rxActionFactory = rxActionFactory;

            Close = rxActionFactory.FromAsync(close);

            SelectItemByIndex = rxActionFactory.FromAction<int>(selectWorkflowItemIndex);
            UpdateDuration = rxActionFactory.FromAction<int>(updateDuration);
            AddWorkflowItem = rxActionFactory.FromAction(addWorkflowItem);
            DeleteWorkflowItem = rxActionFactory.FromAction(deleteWorkflowItem);
            UpdateCurrentWorkflowItemType = rxActionFactory.FromAction<PomodoroWorkflowItemType>(updateWorkflowItemType);
        }

        public override void Prepare(PomodoroWorkflow parameter)
        {
            Workflow = parameter.Clone();
        }

        public override async Task Initialize()
        {
            await base.Initialize();

            workflowItemsSubject = new BehaviorSubject<IReadOnlyList<PomodoroWorkflowItem>>(Workflow.Items);
            WorkflowItems = workflowItemsSubject
                .AsDriver(new List<PomodoroWorkflowItem>(), schedulerProvider);

            SelectedWorkflowItemIndex = selectedWorkflowItemIndexSubject
                .DistinctUntilChanged()
                .AsDriver(schedulerProvider);

            SelectedWorkflowItem = selectedWorkflowItemSubject
                .AsDriver(schedulerProvider);
        }

        private void selectWorkflowItemIndex(int index)
        {
            selectedItemIndex = index;
            var selectedItem = Workflow.Items[index];
            selectedWorkflowItemSubject.OnNext(selectedItem);
        }

        private void addWorkflowItem()
        {
            var updatedItems = Workflow.Items.ToList();
            updatedItems.Add(new PomodoroWorkflowItem(PomodoroWorkflowItemType.Work, 25));

            Workflow = Workflow.CloneWithUpdatedItems(updatedItems);

            selectedItemIndex = updatedItems.LastIndex();

            workflowItemsSubject.OnNext(updatedItems);
            selectedWorkflowItemIndexSubject.OnNext(selectedItemIndex);
        }

        private void deleteWorkflowItem()
        {
            if (Workflow.Items.Count <= 1)
                return;

            var updatedItems = Workflow.Items.ExceptElementAt(selectedItemIndex).ToList();

            Workflow = Workflow.CloneWithUpdatedItems(updatedItems);

            selectedItemIndex = Math.Max(0, selectedItemIndex - 1);

            selectedWorkflowItemIndexSubject.OnNext(selectedItemIndex);
            workflowItemsSubject.OnNext(updatedItems);
        }

        private void updateDuration(int duration)
        {
            var updatedItems = Workflow.Items.Select((item, i) =>
            {
                if (i != selectedItemIndex)
                    return item;

                return new PomodoroWorkflowItem(item.Type, duration);

            }).ToList();

            Workflow = Workflow.CloneWithUpdatedItems(updatedItems);

            workflowItemsSubject.OnNext(updatedItems);
        }

        private void updateWorkflowItemType(PomodoroWorkflowItemType type)
        {
            var updatedItems = Workflow.Items.Select((item, i) =>
            {
                if (i != selectedItemIndex)
                    return item;

                return new PomodoroWorkflowItem(type, item.Minutes);

            }).ToList();

            Workflow = Workflow.CloneWithUpdatedItems(updatedItems);

            workflowItemsSubject.OnNext(updatedItems);
            selectedWorkflowItemSubject.ReemitLastValue();
        }

        private Task close()
            => navigationService.Close(this);
    }
}

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

namespace Toggl.Foundation.MvvmCross.ViewModels
{
    [Preserve(AllMembers = true)]
    public sealed class PomodoroEditWorkflowViewModel : MvxViewModel<PomodoroWorkflow, PomodoroWorkflow>
    {
        private readonly IPomodoroStorage pomodoroStorage;
        private readonly IMvxNavigationService navigationService;
        private readonly ISchedulerProvider schedulerProvider;
        private readonly IRxActionFactory rxActionFactory;

        public UIAction Close { get; }

        private BehaviorSubject<IReadOnlyList<PomodoroWorkflowItem>> workflowItemsSubject;

        public IObservable<IReadOnlyList<PomodoroWorkflowItem>> WorkflowItems { get; private set; }

        private PomodoroWorkflow workflow;

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
        }

        public override void Prepare(PomodoroWorkflow parameter)
        {
            workflow = parameter.Clone();
        }

        public override async Task Initialize()
        {
            await base.Initialize();

            workflowItemsSubject = new BehaviorSubject<IReadOnlyList<PomodoroWorkflowItem>>(workflow.Items);
            WorkflowItems = workflowItemsSubject
                .AsDriver(new List<PomodoroWorkflowItem>(), schedulerProvider);
        }

        private Task close()
            => navigationService.Close(this);
    }
}

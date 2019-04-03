using System;
using System.Linq;
using System.Reactive;
using System.Reactive.Subjects;
using Android.OS;
using Android.Support.V7.Widget;
using Android.Views;
using Toggl.Foundation.Models.Pomodoro;
using Toggl.Foundation.MvvmCross.ViewModels;
using Toggl.Giskard.Adapters;
using Toggl.Giskard.Adapters.DiffingStrategies;
using Toggl.Giskard.Extensions.Reactive;
using Toggl.Giskard.ViewHolders;
using Toggl.Multivac.Extensions;

namespace Toggl.Giskard.Fragments
{
    public sealed partial class PomodoroListingFragment : ReactiveFragment<PomodoroListingViewModel>
    {
        private SimpleAdapter<PomodoroWorkflow> adapter;

        private Subject<PomodoroWorkflow> workflowStartedSubject = new Subject<PomodoroWorkflow>();

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            var view = inflater.Inflate(Resource.Layout.PomodoroListingFragment, container, false);

            InitializeViews(view);

            setupRecyclerView();
            setupBindings();

            return view;
        }

        private void setupRecyclerView()
        {
            adapter = new SimpleAdapter<PomodoroWorkflow>(Resource.Layout.PomodoroListingWorkflowCell, createViewHolder);
            workflowsListView.SetLayoutManager(new LinearLayoutManager(Context));
            workflowsListView.SetAdapter(adapter);
        }

        private BaseRecyclerViewHolder<PomodoroWorkflow> createViewHolder(View view)
            => new PomodoroListingWorkflowViewHolder(view, workflowStartedSubject);

        private void setupBindings()
        {
            ViewModel.Workflows
                .Subscribe(adapter.Rx().ReadOnlyItems())
                .DisposedBy(DisposeBag);
        }
    }

}

using System;
using System.Linq;
using Android.OS;
using Android.Views;
using Toggl.Foundation.MvvmCross.ViewModels;

namespace Toggl.Giskard.Fragments
{
    public sealed partial class PomodoroListingFragment : ReactiveFragment<PomodoroListingViewModel>
    {
        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            var view = inflater.Inflate(Resource.Layout.PomodoroListingFragment, container, false);

            InitializeViews(view);

            return view;
        }
    }
}

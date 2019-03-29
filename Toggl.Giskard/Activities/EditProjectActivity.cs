using System;
using System.Linq;
using System.Reactive.Linq;
using Android.App;
using Android.Content.PM;
using Android.Graphics;
using Android.OS;
using Android.Support.V4.Graphics;
using Android.Support.V7.Widget;
using Android.Views;
using MvvmCross.Platforms.Android.Presenters.Attributes;
using MvvmCross.Plugin.Color.Platforms.Android;
using Toggl.Foundation.MvvmCross.ViewModels;
using Toggl.Giskard.Extensions;
using Toggl.Giskard.Extensions.Reactive;
using Toggl.Giskard.Fragments;
using Toggl.Multivac.Extensions;
using FoundationResources = Toggl.Foundation.Resources;

namespace Toggl.Giskard.Activities
{
    [MvxActivityPresentation]
    [Activity(Theme = "@style/AppTheme.BlueStatusBar",
              WindowSoftInputMode = SoftInput.AdjustResize,
              ScreenOrientation = ScreenOrientation.Portrait,
              ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize)]
    public sealed partial class EditProjectActivity : ReactiveActivity<EditProjectViewModel>
    {
        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);
            SetContentView(Resource.Layout.EditProjectActivity);
            InitializeViews();
            OverridePendingTransition(Resource.Animation.abc_slide_in_bottom, Resource.Animation.abc_fade_out);
            setupToolbar();
            errorText.Visibility = ViewStates.Gone;

            // Name
            projectNameTextView.Rx()
                .Text()
                .Subscribe(ViewModel.Name.Accept)
                .DisposedBy(DisposeBag);

            ViewModel.Name
                .Subscribe(projectNameTextView.Rx().TextObserver(ignoreUnchanged: true))
                .DisposedBy(DisposeBag);

            // Color
            colorCircle.Rx()
                .BindAction(ViewModel.PickColor)
                .DisposedBy(DisposeBag);

            colorArrow.Rx()
                .BindAction(ViewModel.PickColor)
                .DisposedBy(DisposeBag);

            ViewModel.Color
                .Select(color => color.ToNativeColor())
                .Subscribe(colorCircle.SetCircleColor)
                .DisposedBy(DisposeBag);

            // Error
            ViewModel.Error
                .Subscribe(errorText.Rx().TextObserver())
                .DisposedBy(DisposeBag);

            ViewModel.Error
                .Select(e => !string.IsNullOrEmpty(e))
                .Subscribe(errorText.Rx().IsVisible())
                .DisposedBy(DisposeBag);

            var errorOffset = 8.DpToPixels(this);
            var noErrorOffset = 14.DpToPixels(this);

            ViewModel.Error
                .Select(e => string.IsNullOrEmpty(e) ? noErrorOffset : errorOffset)
                .Subscribe(projectNameTextView.LayoutParameters.Rx().MarginTop())
                .DisposedBy(DisposeBag);
            // Workspace
            changeWorkspaceView.Rx()
                .BindAction(ViewModel.PickWorkspace)
                .DisposedBy(DisposeBag);

            ViewModel.WorkspaceName
                .Subscribe(workspaceNameLabel.Rx().TextObserver())
                .DisposedBy(DisposeBag);

            // Client
            changeClientView.Rx()
                .BindAction(ViewModel.PickClient)
                .DisposedBy(DisposeBag);

            ViewModel.ClientName
                .Select(clientNameWithEmptyText)
                .Subscribe(clientNameTextView.Rx().TextObserver())
                .DisposedBy(DisposeBag);

            var noClientColor = Color.ParseColor("#CECECE");
            ViewModel.ClientName
                .Select(clientTextColor)
                .Subscribe(clientNameTextView.SetTextColor)
                .DisposedBy(DisposeBag);

            // Is Private
            toggleIsPrivateView.Rx().Tap()
                .Select(_ => isPrivateSwitch.Checked)
                .Subscribe(ViewModel.IsPrivate.Accept)
                .DisposedBy(DisposeBag);

            ViewModel.IsPrivate
                .Subscribe(isPrivateSwitch.Rx().CheckedObserver())
                .DisposedBy(DisposeBag);

            // Save
            createProjectButton.Rx()
                .BindAction(ViewModel.Save)
                .DisposedBy(DisposeBag);

            var enabledColor = Color.White;
            var disabledColor = new Color(ColorUtils.SetAlphaComponent(Color.White, 127));
            ViewModel.Save.Enabled
                .Select(createProjectTextColor)
                .Subscribe(createProjectButton.SetTextColor)
                .DisposedBy(DisposeBag);

            string clientNameWithEmptyText(string clientName)
                => string.IsNullOrEmpty(clientName) ? FoundationResources.AddClient : clientName;

            Color clientTextColor(string clientName)
                => string.IsNullOrEmpty(clientName) ? noClientColor : Color.Black;

            Color createProjectTextColor(bool enabled)
                => enabled ? enabledColor : disabledColor;
        }


        public override void Finish()
        {
            base.Finish();
            OverridePendingTransition(Resource.Animation.abc_fade_in, Resource.Animation.abc_slide_out_bottom);
        }

        public override bool OnKeyDown(Keycode keyCode, KeyEvent e)
        {
            if (keyCode == Keycode.Back)
            {
                var fragment = SupportFragmentManager.Fragments.FirstOrDefault();
                if (fragment is SelectWorkspaceFragment selectWorkspaceFragment)
                {
                    selectWorkspaceFragment.ViewModel.Close.Execute();
                    return true;
                }

                ViewModel.Close.Execute();
                return true;
            }

            return base.OnKeyDown(keyCode, e);
        }

        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            if (item.ItemId == Android.Resource.Id.Home)
            {
                navigateBack();
                return true;
            }
            return base.OnOptionsItemSelected(item);
        }

        private void setupToolbar()
        {
            var toolbar = FindViewById<Toolbar>(Resource.Id.Toolbar);
            toolbar.Title = ViewModel.Title;

            SetSupportActionBar(toolbar);

            SupportActionBar.SetDisplayHomeAsUpEnabled(true);
            SupportActionBar.SetDisplayShowHomeEnabled(true);
        }

        private void navigateBack()
        {
            ViewModel.Close.Execute();
        }
    }
}

using System;
using System.Linq;
using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Support.V7.Widget;
using Android.Views;
using MvvmCross.Platforms.Android.Presenters.Attributes;
using Toggl.Multivac.Extensions;
using Toggl.Foundation.MvvmCross.ViewModels;
using Toggl.Giskard.Extensions.Reactive;
using Toggl.Giskard.Fragments;
using System.Reactive.Linq;
using MvvmCross.Plugin.Color.Platforms.Android;
using Android.Graphics;
using Android.Support.V4.Graphics;

namespace Toggl.Giskard.Activities
{
    [MvxActivityPresentation]
    [Activity(Theme = "@style/AppTheme.BlueStatusBar",
              WindowSoftInputMode = SoftInput.AdjustResize,
              ScreenOrientation = ScreenOrientation.Portrait,
              ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize)]
    public sealed partial class EditProjectActivity : ReactiveActivity<EditProjectViewModel>
    {
        private const int marginWhenNameIsTaken = 8;
        private const int marginWhenNameIsNotTaken = 14;

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);
            SetContentView(Resource.Layout.EditProjectActivity);
            OverridePendingTransition(Resource.Animation.abc_slide_in_bottom, Resource.Animation.abc_fade_out);
            setupToolbar();

            editProjectColorCircle.Rx()
                .BindAction(ViewModel.PickColor)
                .DisposedBy(DisposeBag);

            editProjectColorArrow.Rx()
                .BindAction(ViewModel.PickColor)
                .DisposedBy(DisposeBag);

            editWorkspace.Rx()
                .BindAction(ViewModel.PickWorkspace)
                .DisposedBy(DisposeBag);

            createProjectButton.Rx()
                .BindAction(ViewModel.Save)
                .DisposedBy(DisposeBag);

            toggleIsPrivateView.Rx()
                .BindAction(ViewModel.TogglePrivateProject)
                .DisposedBy(DisposeBag);

            editClientView.Rx()
                .BindAction(ViewModel.PickClient)
                .DisposedBy(DisposeBag);

            ViewModel.Name
                .Subscribe(editProjectProjectName.Rx().TextObserver())
                .DisposedBy(DisposeBag);

            ViewModel.NameIsAlreadyTaken
                .Select(nameIsTaken => nameIsTaken ? marginWhenNameIsTaken : marginWhenNameIsNotTaken)
                .Subscribe(editProjectProjectName.Rx().MarginTop())
                .DisposedBy(DisposeBag);

            ViewModel.NameIsAlreadyTaken
                .Subscribe(errorText.Rx().IsVisible())
                .DisposedBy(DisposeBag);

            ViewModel.Color
                .Select(color => color.ToNativeColor())
                .Subscribe(editProjectColorCircle.SetCircleColor)
                .DisposedBy(DisposeBag);

            ViewModel.IsPrivate
                .Subscribe(isPrivateSwitch.Rx().Checked())
                .DisposedBy(DisposeBag);

            ViewModel.Save.Enabled
                .Select(saveButtonColor)
                .Subscribe(createProjectButton.Rx().TextColor())
                .DisposedBy(DisposeBag);

            ViewModel.WorkspaceName
                .Subscribe(workspaceNameLabel.Rx().TextObserver())
                .DisposedBy(DisposeBag);

            ViewModel.ClientName
                .Select(clientNameWithDefaultValue)
                .Subscribe(clientNameTextView.Rx().TextObserver())
                .DisposedBy(DisposeBag);

            ViewModel.ClientName
                .Select(clientNameColor)
                .Subscribe(clientNameTextView.Rx().TextColor())
                .DisposedBy(DisposeBag);

            Color saveButtonColor(bool saveIsEnabled)
                => saveIsEnabled ? Color.White : new Color(ColorUtils.SetAlphaComponent(Color.White, 127));

            Color clientNameColor(string clientName)
                => string.IsNullOrEmpty(clientName) ? Color.ParseColor("#CECECE") : Color.Black;

            string clientNameWithDefaultValue(string clientName)
                => string.IsNullOrEmpty(clientName) ? Foundation.Resources.NoClient : clientName;
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
            toolbar.Title = ViewModel.PageTitle;

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

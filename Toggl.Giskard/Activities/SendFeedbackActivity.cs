using System;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Views;
using MvvmCross.Binding.BindingContext;
using MvvmCross.Platforms.Android.Presenters.Attributes;
using Toggl.Foundation.UI.ViewModels;
using Toggl.Giskard.Extensions.Reactive;
using Toggl.Multivac.Extensions;
using Toggl.Ultrawave.Exceptions;

namespace Toggl.Giskard.Activities
{
    [MvxActivityPresentation]
    [Activity(Theme = "@style/AppTheme",
        ScreenOrientation = ScreenOrientation.Portrait,
        WindowSoftInputMode = SoftInput.StateVisible,
        ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize)]
    public partial class SendFeedbackActivity : ReactiveActivity<SendFeedbackViewModel>
    {
        private bool sendEnabled;
        private Subject<Unit> sendFeedbackSubject = new Subject<Unit>();
        private Subject<Unit> closeSubject = new Subject<Unit>();

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);
            SetContentView(Resource.Layout.SendFeedbackActivity);
            OverridePendingTransition(Resource.Animation.abc_slide_in_bottom, Resource.Animation.abc_fade_out);

            InitializeViews();
            SetSupportActionBar(toolbar);
            SupportActionBar.Title = GetString(Resource.String.SendFeedbackTitle);
            SupportActionBar.SetDisplayHomeAsUpEnabled(true);
            SupportActionBar.SetDisplayShowHomeEnabled(true);

            feedbackEditText.Rx().Text()
                .Subscribe(ViewModel.FeedbackText)
                .DisposedBy(DisposeBag);

            errorCard.Rx()
                .BindAction(ViewModel.DismissError)
                .DisposedBy(DisposeBag);

            var sendButtonEnabled = ViewModel.SendEnabled.CombineLatest(ViewModel.IsLoading,
                (sendIsEnabled, isLoading) => sendIsEnabled && !isLoading);
           sendButtonEnabled
                .Subscribe(onSendEnabled)
                .DisposedBy(DisposeBag);

            ViewModel.Error
                .Select(selectErrorMessage)
                .Subscribe(errorInfoText.Rx().TextObserver())
                .DisposedBy(DisposeBag);

            sendFeedbackSubject
                .Subscribe(ViewModel.Send.Inputs)
                .DisposedBy(DisposeBag);

            closeSubject
                .Subscribe(ViewModel.Close.Inputs)
                .DisposedBy(DisposeBag);

            ViewModel.Error
                .Select(error => error != null)
                .Subscribe(errorCard.Rx().IsVisible())
                .DisposedBy(DisposeBag);

            ViewModel.IsLoading
                .Subscribe(progressBar.Rx().IsVisible())
                .DisposedBy(DisposeBag);

            ViewModel.IsLoading
                .Invert()
                .Subscribe(feedbackEditText.Rx().Enabled())
                .DisposedBy(DisposeBag);
        }

        public override bool OnCreateOptionsMenu(IMenu menu)
        {
            MenuInflater.Inflate(Resource.Menu.SendFeedbackMenu, menu);
            return true;
        }

        public override bool OnPrepareOptionsMenu(IMenu menu)
        {
            var sendMenuItem = menu.FindItem(Resource.Id.SendMenuItem);
            sendMenuItem.SetEnabled(sendEnabled);
            return true;
        }

        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            switch (item.ItemId)
            {
                case Resource.Id.SendMenuItem:
                    sendFeedbackSubject.OnNext(Unit.Default);
                    return true;

                case Android.Resource.Id.Home:
                    closeSubject.OnNext(Unit.Default);
                    return true;

                default:
                    return base.OnOptionsItemSelected(item);
            }
        }

        public override void OnBackPressed()
        {
            closeSubject.OnNext(Unit.Default);
        }

        private void onSendEnabled(bool enabled)
        {
            sendEnabled = enabled;
            InvalidateOptionsMenu();
        }

        private string selectErrorMessage(Exception exception)
            => exception is OfflineException
                ? GetString(Resource.String.GenericInternetConnectionErrorMessage)
                : GetString(Resource.String.GenericErrorMessage);
    }
}

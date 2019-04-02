using System;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Android;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Provider;
using Android.Support.V4.App;
using Android.Support.V4.Content;
using MvvmCross;
using MvvmCross.Platforms.Android;
using Toggl.Foundation.MvvmCross.Services;
using Toggl.Giskard.Helper;
using Uri = Android.Net.Uri;

namespace Toggl.Giskard.Services
{
    [Preserve(AllMembers = true)]
    public sealed class PermissionsServiceAndroid : IPermissionsService
    {
        private const int calendarAuthCode = 500;

        private Subject<bool> calendarAuthorizationSubject;

        public IObservable<bool> CalendarPermissionGranted
            => Observable.Start(() => checkPermissions(Manifest.Permission.ReadCalendar));

        public IObservable<bool> NotificationPermissionGranted
            => Observable.Return(true);

        public IObservable<bool> RequestCalendarAuthorization(bool force = false)
            => Observable.Defer(() =>
            {
                if (checkPermissions(Manifest.Permission.ReadCalendar))
                    return Observable.Return(true);

                if (calendarAuthorizationSubject != null)
                    return calendarAuthorizationSubject.AsObservable();

                var topActivity = Mvx.Resolve<IMvxAndroidCurrentTopActivity>().Activity;
                if (!(topActivity is IPermissionAskingActivity permissionAskingActivity))
                    return Observable.Return(false);

                permissionAskingActivity.OnPermissionChangedCallback = onPermissionChanged;
                calendarAuthorizationSubject = new Subject<bool>();
                ActivityCompat.RequestPermissions(topActivity, new[] { Manifest.Permission.ReadCalendar, Manifest.Permission.WriteCalendar }, calendarAuthCode);

                return calendarAuthorizationSubject.AsObservable();
            });

        private void onPermissionChanged(int requestCode, string[] permissions, Permission[] grantResults)
        {
            if (requestCode != calendarAuthCode)
            {
                calendarAuthorizationSubject?.OnNext(false);
                calendarAuthorizationSubject?.OnCompleted();
                calendarAuthorizationSubject = null;
            }

            var permissionWasGranted = grantResults.Any() && grantResults.First() == Permission.Granted;
            calendarAuthorizationSubject?.OnNext(permissionWasGranted);
            calendarAuthorizationSubject?.OnCompleted();
            calendarAuthorizationSubject = null;
        }

        public IObservable<bool> RequestNotificationAuthorization(bool force = false)
            => Observable.Return(true);

        public void OpenAppSettings()
        {
            var settingsIntent = new Intent();
            settingsIntent.SetAction(Settings.ActionApplicationDetailsSettings);
            settingsIntent.AddCategory(Intent.CategoryDefault);
            settingsIntent.SetData(Uri.Parse("package:com.toggl.giskard"));
            settingsIntent.AddFlags(ActivityFlags.NewTask);
            settingsIntent.AddFlags(ActivityFlags.NoHistory);
            settingsIntent.AddFlags(ActivityFlags.ExcludeFromRecents);
            
            Application.Context.StartActivity(settingsIntent);
        }

        private bool checkPermissions(params string[] permissionsToCheck)
        {
            foreach (var permission in permissionsToCheck)
            {
                if (MarshmallowApis.AreAvailable)
                {
                    if (ContextCompat.CheckSelfPermission(Application.Context, permission) != Permission.Granted)
                        return false;
                }
                else
                {
                    if (PermissionChecker.CheckSelfPermission(Application.Context, permission) != PermissionChecker.PermissionGranted)
                        return false;
                }
            }

            return true;
        }

        internal interface IPermissionAskingActivity
        {
            Action<int, string[], Permission[]> OnPermissionChangedCallback { get; set; }
        }
    }
}

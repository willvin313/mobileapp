using System;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Threading.Tasks;
using EventKit;
using Foundation;
using Toggl.Foundation.UI.Services;
using UIKit;
using UserNotifications;

namespace Toggl.Daneel.Services
{
    [Preserve(AllMembers = true)]
    public sealed class PermissionsServiceIos : IPermissionsService
    {
        private readonly UNAuthorizationOptions options = UNAuthorizationOptions.Alert | UNAuthorizationOptions.Badge | UNAuthorizationOptions.Sound;

        public IObservable<bool> CalendarPermissionGranted
            => Observable.Return(
                EKEventStore.GetAuthorizationStatus(EKEntityType.Event) == EKAuthorizationStatus.Authorized
            );

        public IObservable<bool> NotificationPermissionGranted
            => UNUserNotificationCenter.Current
                .GetNotificationSettingsAsync()
                .ToObservable()
                .Select(settings => settings.AuthorizationStatus == UNAuthorizationStatus.Authorized);

        public IObservable<bool> RequestCalendarAuthorization(bool force = false)
            => requestPermission(
                CalendarPermissionGranted,
                () => new EKEventStore().RequestAccessAsync(EKEntityType.Event),
                force
            );

        public IObservable<bool> RequestNotificationAuthorization(bool force = false)
            => requestPermission(
                NotificationPermissionGranted,
                () => UNUserNotificationCenter.Current.RequestAuthorizationAsync(options),
                force
            );

        public void OpenAppSettings()
        {
            UIApplication.SharedApplication.OpenUrl(
                NSUrl.FromString(UIApplication.OpenSettingsUrlString)
            );
        }

        private IObservable<bool> requestPermission(
            IObservable<bool> permissionChecker, 
            Func<Task<Tuple<bool, NSError>>> permissionRequestFunction,
            bool force)
            => Observable.DeferAsync(async cancellationToken =>
            {
                var permissionGranted = await permissionChecker;

                if (permissionGranted)
                    return Observable.Return(true);

                if (force)
                {
                    //Fact: If the user changes any permissions through the settings, this app gets restarted
                    //and in that case we don't care about the value returned from this method.
                    //We care about the returned value in the case, when user opens settings
                    //and comes back to this app without altering any permissions. In that case
                    //returning the current permission status is the correct behaviour.
                    OpenAppSettings();
                    return Observable.Return(false);
                }

                return Observable
                    .FromAsync(permissionRequestFunction)
                    .Select(tuple => tuple.Item1);
            });

    }
}

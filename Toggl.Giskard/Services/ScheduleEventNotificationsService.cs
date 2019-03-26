
using System;
using Android.App;
using Android.Content;
using Android.Support.V4.App;
using MvvmCross;
using MvvmCross.Platforms.Android.Core;
using Toggl.Foundation.Interactors;

namespace Toggl.Giskard.Services
{
    [Service(
        Exported = true,
        Permission = "android.permission.BIND_JOB_SERVICE", 
        Name = "com.toggl.giskard.ScheduleEventNotificationsService")]
    public class ScheduleEventNotificationsService : JobIntentService
    {
        private IDisposable disposable;

        public const int JobId = 42;

        public static void EnqueueWork(Context context, Intent intent)
        {
            var componentName = new ComponentName(context, Java.Lang.Class.FromType(typeof(ScheduleEventNotificationsService)));
            EnqueueWork(context, componentName, JobId, intent);
        }

        protected override void OnHandleWork(Intent intent)
        {
            var setup = MvxAndroidSetupSingleton.EnsureSingletonAvailable(ApplicationContext);
            setup.EnsureInitialized();

            if (!Mvx.TryResolve<IInteractorFactory>(out var interactorFactory))
                return;

            disposable?.Dispose();
            disposable = null;

            disposable = interactorFactory
                .ScheduleEventNotificationsForNextWeek()
                .Execute()
                .Subscribe();
        }

        public override void OnDestroy()
        {
            base.OnDestroy();

            disposable?.Dispose();
            disposable = null;
        }
    }
}

using Android.App;
using Android.Content;
using Toggl.Giskard.Services;

namespace Toggl.Giskard.BroadcastReceivers
{
    [BroadcastReceiver(
        Exported = true,
        Permission = "android.permission.BIND_JOB_SERVICE",
        Name = "com.toggl.giskard.BootCompletedBroadcastReceiver")]
    [IntentFilter(new[] { Intent.ActionBootCompleted, Intent.CategoryDefault })]
    public class BootCompletedBroadcastReceiver : BroadcastReceiver
    {
        public override void OnReceive(Context context, Intent intent)
        {
            ScheduleEventNotificationsService.EnqueueWork(context, intent);
        }
    }
}

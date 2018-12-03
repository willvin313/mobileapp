using System;
using Android.App;
using Android.Content;
using Android.OS;
using Toggl.Foundation;
using Toggl.Ultrawave;
using Toggl.Ultrawave.Network;

namespace Toggl.Giskard
{
    public sealed class AndroidPlatformInfo : IPlatformInfo
    {
        public string HelpUrl { get; } = "https://support.toggl.com/toggl-timer-for-android/";

        public string PhoneModel { get; } = $"{Build.Manufacturer} {Build.Model}";

        public string OperatingSystem { get; } = getOperatingSystem();

        public Platform Platform { get; } = Platform.Giskard;

        public string BuildNumber { get; }

        public string StoreUrl { get; } = "https://play.google.com/store/apps/details?id=com.toggl.giskard";

        public Version Version { get; }

        public UserAgent UserAgent { get; }

        public ApiEnvironment ApiEnvironment { get; }

        public AndroidPlatformInfo(ApiEnvironment apiEnvironment, Context appContext)
        {
            var packageInfo = appContext.PackageManager.GetPackageInfo(appContext.PackageName, 0);
            var clientName = Platform.ToString();
            var version = packageInfo.VersionName;
            var versionCode = packageInfo.VersionCode;

            ApiEnvironment = apiEnvironment;
            Version = Version.Parse(version);
            BuildNumber = versionCode.ToString();
            UserAgent = new UserAgent(clientName, version);
        }

        private static string getOperatingSystem()
        {
            var releaseVersion = Build.VERSION.Release;
            var platformRelease = new Build.VERSION_CODES().Class.GetFields()[(int)Build.VERSION.SdkInt].Name;
            return $"{releaseVersion} {platformRelease}";
        }
    }
}

using System;
using System.Collections.Generic;
using Android.App;
using Android.OS;
using Firebase.Analytics;
using Microsoft.AppCenter.Crashes;
using Toggl.Foundation.Analytics;
using AppCenterAnalytics = Microsoft.AppCenter.Analytics.Analytics;

namespace Toggl.Giskard.Services
{
    public sealed class AnalyticsServiceAndroid : BaseAnalyticsService
    {
        private const int maxAppCenterStringLength = 64;

        private FirebaseAnalytics firebaseAnalytics { get; }

        public AnalyticsServiceAndroid()
        {
            #if USE_ANALYTICS
            firebaseAnalytics = FirebaseAnalytics.GetInstance(Application.Context);
            #endif
        }

        public override void Track(string eventName, Dictionary<string, string> parameters)
        {
            #if USE_ANALYTICS
            var bundle = bundleFromParameters(parameters);
            firebaseAnalytics.LogEvent(eventName, bundle);
            AppCenterAnalytics.TrackEvent(eventName, trimLongParameters(parameters));
            #endif
        }

        protected override void TrackException(Exception exception)
        {
            Crashes.TrackError(exception);
        }

        public override void Track(Exception exception, string message)
        {
            var dict = new Dictionary<string, string>
            {
                [nameof(message)] = message,
            };

            Crashes.TrackError(exception, dict);
        }

        private Bundle bundleFromParameters(Dictionary<string, string> parameters)
        {
            var bundle = new Bundle();

            foreach(var entry in parameters)
            {
                bundle.PutString(entry.Key, entry.Value);
            }

            return bundle;
        }

        private Dictionary<string, string> trimLongParameters(Dictionary<string, string> parameters)
        {
            var validParameters = new Dictionary<string, string>();
            foreach (var (key, value) in parameters)
            {
                validParameters.Add(trimForAppCenter(key), trimForAppCenter(value));
            }

            return validParameters;
        }

        private string trimForAppCenter(string text)
            => text.Length > maxAppCenterStringLength ? text.Substring(0, maxAppCenterStringLength) : text;
    }
}

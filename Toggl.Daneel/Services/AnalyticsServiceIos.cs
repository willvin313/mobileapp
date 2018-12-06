using System;
using System.Collections.Generic;
using System.Linq;
using Foundation;
using Microsoft.AppCenter.Crashes;
using Toggl.Foundation.Analytics;
using FirebaseAnalytics = Firebase.Analytics.Analytics;
using AppCenterAnalytics = Microsoft.AppCenter.Analytics.Analytics;

namespace Toggl.Daneel.Services
{
    public sealed class AnalyticsServiceIos : BaseAnalyticsService
    {
        private const int maxAppCenterStringLength = 64;

        public override void Track(string eventName, Dictionary<string, string> parameters = null)
        {
            parameters = parameters ?? new Dictionary<string, string>();

            AppCenterAnalytics.TrackEvent(eventName, trimLongParameters(parameters));

            var convertedDictionary = convertDictionary(parameters);
            FirebaseAnalytics.LogEvent(new NSString(eventName), convertedDictionary);
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

        private NSDictionary<NSString, NSObject> convertDictionary(Dictionary<string, string> parameters)
        {
            if (parameters.Count == 0)
                return new NSDictionary<NSString, NSObject>();

            return NSDictionary<NSString, NSObject>.FromObjectsAndKeys(
                parameters.Values.ToArray(),
                parameters.Keys.ToArray()
            );
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

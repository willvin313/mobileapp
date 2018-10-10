using Foundation;
using Toggl.Daneel.ExtensionKit;
using Toggl.Ultrawave;
using Toggl.Ultrawave.Network;

namespace SiriExtension
{
    public class APIHelper
    {
        #if USE_PRODUCTION_API
        private const ApiEnvironment environment = ApiEnvironment.Production;
        #else
        private const ApiEnvironment environment = ApiEnvironment.Staging;
        #endif

        public static TogglApi GetTogglAPI()
        {
            var apiToken = SharedStorage.instance.GetApiToken();
            if (apiToken == null)
            {
                return null;
            }

            var version = NSBundle.MainBundle.InfoDictionary["CFBundleShortVersionString"].ToString();
            var userAgent = new UserAgent("Daneel", $"{version}.SiriExtension");
            return new TogglApi(new ApiConfiguration(environment, Credentials.WithApiToken(apiToken), userAgent));
        }
    }
}
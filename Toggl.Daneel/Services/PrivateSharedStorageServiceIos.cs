using System;
using Toggl.Foundation.Services;
using Foundation;
using Toggl.Daneel.ExtensionKit;

namespace Toggl.Daneel.Services
{
    public class PrivateSharedStorageServiceIos : IPrivateSharedStorageService
    {        
        public void SaveApiToken(string apiToken)
        {
            SharedStorage.instance.SetApiToken(apiToken);
        }

        public void SaveUserId(long userId)
        {
            SharedStorage.instance.SetUserId(userId);
        }

        public void SaveLastUpdateDate(DateTimeOffset date)
        {
            SharedStorage.instance.SetLastUpdateDate(date);
        }

        public void ClearAll()
        {
            SharedStorage.instance.DeleteEverything();
        }
    }
}

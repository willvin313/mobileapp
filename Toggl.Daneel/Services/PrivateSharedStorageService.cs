using System;
using Toggl.Foundation.Services;
using Foundation;
using Toggl.Daneel.ExtensionKit;

namespace Toggl.Daneel.Services
{
    public class PrivateSharedStorageService : IPrivateSharedStorageService
    {        
        public void SaveApiToken(string apiToken)
        {
            SharedStorage.instance.SetApiToken(apiToken);
        }

        public void ClearAll()
        {
            SharedStorage.instance.DeleteEverything();
        }

        public void SaveUserId(long userId)
        {
            SharedStorage.instance.SetUserId(userId);
        }
    }
}

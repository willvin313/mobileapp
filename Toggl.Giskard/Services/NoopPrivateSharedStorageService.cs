using System;
using Toggl.Foundation.Services;

namespace Toggl.Giskard.Services
{
    public class NoopPrivateSharedStorageService : IPrivateSharedStorageService
    {
        public void SaveApiToken(string apiToken)
        {
        }

        public void SaveUserId(long userId)
        {
        }

        public void ClearAll()
        {
        }
    }
}

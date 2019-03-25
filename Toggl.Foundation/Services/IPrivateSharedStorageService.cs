using System;

namespace Toggl.Foundation.Services
{
    public interface IPrivateSharedStorageService
    {
        void SaveApiToken(string apiToken);

        void SaveUserId(long userId);

        void SaveLastUpdateDate(DateTimeOffset date);

        void ClearAll();
    }
}

using System;

namespace Toggl.Foundation.UI.Services
{
    public interface IBrowserService
    {
        void OpenUrl(string url);

        void OpenStore();
    }
}

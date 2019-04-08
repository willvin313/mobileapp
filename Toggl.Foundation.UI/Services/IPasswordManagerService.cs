using System;

namespace Toggl.Foundation.UI.Services
{
    public interface IPasswordManagerService
    {
        bool IsAvailable { get; }

        IObservable<PasswordManagerResult> GetLoginInformation();
    }
}

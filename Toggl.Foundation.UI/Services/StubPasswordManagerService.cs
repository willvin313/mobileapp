using System;
namespace Toggl.Foundation.UI.Services
{
    public sealed class StubPasswordManagerService : IPasswordManagerService
    {
        public bool IsAvailable => false;

        public IObservable<PasswordManagerResult> GetLoginInformation()
            => throw new NotImplementedException("You need to implement IPasswordManagerService and register it on the platform specific setup during the InitializeLastChance method before using it.");
    }
}

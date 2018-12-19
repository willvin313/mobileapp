using System;
using System.Reactive;

namespace Toggl.Foundation.Login
{
    public interface IGoogleService
    {
        IObservable<GoogleAccountData> GetGoogleAccountData();

        IObservable<Unit> LogOutIfNeeded();
    }
}

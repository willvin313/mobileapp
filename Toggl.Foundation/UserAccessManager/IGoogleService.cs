using System;
using System.Reactive;

namespace Toggl.Foundation.Login
{
    public interface IGoogleService
    {
        IObservable<string> GetAuthToken();
        IObservable<Unit> LogOutIfNeeded();
    }
}

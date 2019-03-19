using Toggl.Foundation.Interactors;

namespace Toggl.Foundation.Shortcuts
{
    public interface IApplicationShortcutCreator
    {
        void OnLogin(IInteractorFactory interactorFactory);
        void OnLogout();
    }
}

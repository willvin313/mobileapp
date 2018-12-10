using Toggl.Ultrawave;
using Toggl.Ultrawave.Network;

namespace Toggl.Foundation.Login
{
    public interface IApiFactory
    {
        ITogglApi CreateApiWith(Credentials credentials);
    }
}

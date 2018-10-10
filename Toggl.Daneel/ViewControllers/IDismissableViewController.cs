using System.Threading.Tasks;

namespace Toggl.Daneel.ViewControllers
{
    public interface IDismissableViewController
    {
        Task<bool> Dismiss();
    }
}

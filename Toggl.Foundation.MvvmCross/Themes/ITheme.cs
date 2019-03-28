using MvvmCross.UI;

namespace Toggl.Foundation.MvvmCross.Themes
{
    public interface ITheme
    {
        MvxColor CardColor { get; }

        MvxColor BackgroundColor { get; }
    }
}

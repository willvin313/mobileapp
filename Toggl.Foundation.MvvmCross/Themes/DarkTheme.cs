using MvvmCross.UI;
using Toggl.Foundation.MvvmCross.Helper;

namespace Toggl.Foundation.MvvmCross.Themes
{
    public class DarkTheme : ITheme
    {
        public MvxColor CardColor { get; } = Color.LighterGrey;

        public MvxColor BackgroundColor { get; } = Color.Grey;
    }
}

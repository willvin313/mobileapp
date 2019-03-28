using MvvmCross.UI;
using Toggl.Foundation.MvvmCross.Helper;

namespace Toggl.Foundation.MvvmCross.Themes
{
    public class LightTheme : ITheme
    {
        public MvxColor CardColor { get; } = Color.NearlyWhite;

        public MvxColor BackgroundColor { get; } = Color.White;
    }
}

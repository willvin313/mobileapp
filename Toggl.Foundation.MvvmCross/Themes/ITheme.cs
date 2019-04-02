using Toggl.Multivac;

namespace Toggl.Foundation.MvvmCross.Themes
{
    public interface ITheme
    {
        Color Card { get; }

        Color Background { get; }

        Color CellBackground { get; }

        Color Text { get; }

        Color Separator { get; }

        Color BottomBar { get; }

        Color Error { get; }
    }
}

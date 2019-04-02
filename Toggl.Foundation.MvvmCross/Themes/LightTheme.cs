using Toggl.Multivac;

namespace Toggl.Foundation.MvvmCross.Themes
{
    public class LightTheme : ITheme
    {
        public Color Card { get; } = Colors.White;

        public Color Background { get; } = Colors.NearlyWhite;

        public Color CellBackground { get; } = Colors.White;

        public Color Text { get; } = Colors.Black;

        public Color Separator { get; } = Colors.KindaWhite;

        public Color BottomBar { get; } = Colors.White;

        public Color Error { get; } = Colors.Red;
    }
}

using Toggl.Multivac;

namespace Toggl.Foundation.MvvmCross.Themes
{
    public class DarkTheme : ITheme
    {
        public Color Card { get; } = Colors.LighterGrey;

        public Color Background { get; } = Colors.Grey;

        public Color CellBackground { get; } = Colors.DarkerGrey;

        public Color Text { get; } = Colors.White.WithAlpha(204);

        public Color Separator { get; } = Colors.LightestGrey;

        public Color BottomBar { get; } = Colors.DarkerGrey;

        public Color Error { get; } = Colors.SofterRed;
    }
}

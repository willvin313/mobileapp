using System;
using System.Reactive.Linq;
using Toggl.Multivac;
using Toggl.Multivac.Extensions;
using Toggl.PrimeRadiant.Settings;

namespace Toggl.Foundation.MvvmCross.Themes
{
    public interface IThemeProvider
    {
        IObservable<ITheme> CurrentTheme { get; }
    }

    public class ThemeProvider : IThemeProvider
    {
        public IObservable<ITheme> CurrentTheme { get; set; }

        public ThemeProvider(IUserPreferences userPreferences, ISchedulerProvider schedulerProvider)
        {
            CurrentTheme = userPreferences.UseDarkTheme
                .Select(themeFromUserPreference)
                .AsDriver(LightTheme, schedulerProvider);
        }

        private ITheme themeFromUserPreference(bool useDarkTheme)
            => useDarkTheme ? DarkTheme : (ITheme)LightTheme;

        protected virtual DarkTheme DarkTheme { get; set; }
            = new DarkTheme();

        protected virtual LightTheme LightTheme { get; set; }
            = new LightTheme();
    }
}

using MvvmCross;
using Toggl.Foundation.DataSources;

namespace Toggl.Foundation.MvvmCross
{
    public static class DataSourceExtensions
    {
        public static ITogglDataSource RegisterServices(this ITogglDataSource self)
        {
            Mvx.RegisterSingleton(self);
            Mvx.RegisterSingleton(self.Tags);
            Mvx.RegisterSingleton(self.User);
            Mvx.RegisterSingleton(self.Tasks);
            Mvx.RegisterSingleton(self.Clients);
            Mvx.RegisterSingleton(self.Projects);
            Mvx.RegisterSingleton(self.TimeEntries);
            Mvx.RegisterSingleton(self.SyncManager);

            return self;
        }
    }
}

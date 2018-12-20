using Toggl.Ultrawave.Network;

namespace Toggl.Ultrawave.Tests.Integration.Helper
{
    public static class Configuration
    {
        public static UserAgent UserAgent { get; }
            = new UserAgent("MobileIntegrationTests", "93d84a68b46c262f671fdfcbfa33f45a8c99ee94");
    }
}

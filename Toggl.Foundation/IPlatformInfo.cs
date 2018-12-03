using System;
using Toggl.Ultrawave;
using Toggl.Ultrawave.Network;

namespace Toggl.Foundation
{
    public interface IPlatformInfo
    {
        Platform Platform { get; }

        string HelpUrl { get; }

        string PhoneModel { get; }

        string OperatingSystem { get; }

        string BuildNumber { get; }

        string StoreUrl { get; }

        Version Version { get; }

        UserAgent UserAgent { get; }

        ApiEnvironment ApiEnvironment { get; }
    }

    public enum Platform
    {
        Daneel,
        Giskard
    }
}

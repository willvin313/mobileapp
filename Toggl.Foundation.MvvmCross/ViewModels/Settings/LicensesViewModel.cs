using System.Collections.Immutable;
using System.Linq;
using System.Reactive.Linq;
using MvvmCross.ViewModels;
using Toggl.Foundation.Services;
using Toggl.Multivac;

namespace Toggl.Foundation.UI.ViewModels
{
    [Preserve(AllMembers = true)]
    public sealed class LicensesViewModel : MvxViewModel
    {
        public IImmutableList<License> Licenses { get; }

        public LicensesViewModel(ILicenseProvider licenseProvider)
        {
            Ensure.Argument.IsNotNull(licenseProvider, nameof(licenseProvider));

            Licenses = licenseProvider.GetAppLicenses()
                .Select(keyValuePair => new License(keyValuePair.Key, keyValuePair.Value))
                .ToImmutableList();
        }
    }
}

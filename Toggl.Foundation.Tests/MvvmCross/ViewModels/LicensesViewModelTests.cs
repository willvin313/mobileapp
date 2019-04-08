using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using FluentAssertions;
using NSubstitute;
using Toggl.Foundation.UI.ViewModels;
using Toggl.Multivac;
using Xunit;

namespace Toggl.Foundation.Tests.MvvmCross.ViewModels
{
    public sealed class LicensesViewModelTests
    {
        public abstract class LicensesViewModelTest : BaseViewModelTests<LicensesViewModel>
        {
            protected override LicensesViewModel CreateViewModel()
                => new LicensesViewModel(LicenseProvider);
        }

        public sealed class TheConstructor : LicensesViewModelTest
        {
            private readonly Dictionary<string, string> licenses = new Dictionary<string, string>
            {
                { "Something", "Some long license" },
                { "Something else", "Some other license" },
                { "Third one", "Another even longer license" }
            };

            protected override LicensesViewModel CreateViewModel()
            {
                LicenseProvider.GetAppLicenses().Returns(licenses);

                return base.CreateViewModel();
            }

            [Fact, LogIfTooSlow]
            public void ThrowsIfTheArgumentIsNull()
            {
                Action tryingToConstructWithEmptyParameters =
                    () => new LicensesViewModel(null);

                tryingToConstructWithEmptyParameters
                    .Should().Throw<ArgumentNullException>();
            }

            [Fact, LogIfTooSlow]
            public void InitializesTheLicenses()
            {
                var expectedLicenses = licenses
                    .Select(license => new License(license.Key, license.Value))
                    .ToImmutableList();
                
                ViewModel.Licenses.Should().BeEquivalentTo(expectedLicenses);
            }
        }
    }
}

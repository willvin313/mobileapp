﻿using MvvmCross.Navigation;
using MvvmCross.ViewModels;
using Toggl.Foundation.UI.Services;
using Toggl.Foundation.Services;
using Toggl.Multivac;
using Toggl.Multivac.Extensions;

namespace Toggl.Foundation.UI.ViewModels
{
    [Preserve(AllMembers = true)]
    public sealed class TermsOfServiceViewModel : MvxViewModelResult<bool>
    {
        private readonly IRxActionFactory rxActionFactory;

        private const string privacyPolicyUrl = "https://toggl.com/legal/privacy/";
        private const string termsOfServiceUrl = "https://toggl.com/legal/terms/";

        private readonly IBrowserService browserService;

        public UIAction ViewTermsOfService { get; }
        public UIAction ViewPrivacyPolicy { get; }
        public InputAction<bool> Close { get; }

        public TermsOfServiceViewModel(IBrowserService browserService, IRxActionFactory rxActionFactory, IMvxNavigationService navigationService)
        {
            Ensure.Argument.IsNotNull(browserService, nameof(browserService));
            Ensure.Argument.IsNotNull(rxActionFactory, nameof(rxActionFactory));
            Ensure.Argument.IsNotNull(navigationService, nameof(navigationService));

            this.browserService = browserService;
            this.rxActionFactory = rxActionFactory;

            ViewPrivacyPolicy = rxActionFactory.FromAction(() => openUrl(privacyPolicyUrl));
            ViewTermsOfService = rxActionFactory.FromAction(() => openUrl(termsOfServiceUrl));
            Close = rxActionFactory.FromAsync<bool>(result => navigationService.Close(this, result));
        }

        private void openUrl(string url)
        {
            browserService.OpenUrl(url);
        }
    }
}

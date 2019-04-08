﻿using System;
using System.Threading.Tasks;
using MvvmCross.Navigation;
using Toggl.Foundation.UI.ViewModels;
using Toggl.Foundation.Services;
using Toggl.Foundation.Exceptions;
using Toggl.Multivac;
using Toggl.PrimeRadiant.Settings;
using Toggl.Ultrawave.Exceptions;
using Toggl.Foundation.Analytics;

namespace Toggl.Foundation.UI.Services
{
    public sealed class ErrorHandlingService : IErrorHandlingService
    {
        private readonly IMvxNavigationService navigationService;
        private readonly IAccessRestrictionStorage accessRestrictionStorage;

        public ErrorHandlingService(
            IMvxNavigationService navigationService,
            IAccessRestrictionStorage accessRestrictionStorage)
        {
            Ensure.Argument.IsNotNull(navigationService, nameof(navigationService));
            Ensure.Argument.IsNotNull(accessRestrictionStorage, nameof(accessRestrictionStorage));

            this.navigationService = navigationService;
            this.accessRestrictionStorage = accessRestrictionStorage;
        }

        public bool TryHandleDeprecationError(Exception error)
        {
            switch (error)
            {
                case ApiDeprecatedException _:
                    accessRestrictionStorage.SetApiOutdated();
                    navigationService.Navigate<OutdatedAppViewModel>();
                    return true;
                case ClientDeprecatedException _:
                    accessRestrictionStorage.SetClientOutdated();
                    navigationService.Navigate<OutdatedAppViewModel>();
                    return true;
            }

            return false;
        }

        public bool TryHandleUnauthorizedError(Exception error)
        {
            if (error is UnauthorizedException unauthorized)
            {
                var token = unauthorized.ApiToken;
                if (token != null)
                {
                    accessRestrictionStorage.SetUnauthorizedAccess(token);
                    navigationService.Navigate<TokenResetViewModel>();
                }

                return true;
            }

            return false;
        }

        public bool TryHandleNoWorkspaceError(Exception error)
        {
            if (error is NoWorkspaceException)
            {
                accessRestrictionStorage.SetNoWorkspaceStateReached(true);
                return true;
            }

            return false;
        }

        public bool TryHandleNoDefaultWorkspaceError(Exception error)
        {
            if (error is NoDefaultWorkspaceException)
            {
                accessRestrictionStorage.SetNoDefaultWorkspaceStateReached(true);
                return true;
            }

            return false;
        }
    }
}

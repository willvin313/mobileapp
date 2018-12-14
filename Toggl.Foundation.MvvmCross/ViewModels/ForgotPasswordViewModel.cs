using System;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using MvvmCross.Navigation;
using MvvmCross.ViewModels;
using Toggl.Foundation.Login;
using Toggl.Foundation.MvvmCross.Parameters;
using Toggl.Multivac;
using Toggl.Multivac.Extensions;
using Toggl.Ultrawave.Exceptions;

namespace Toggl.Foundation.MvvmCross.ViewModels
{
    [Preserve(AllMembers = true)]
    public sealed class ForgotPasswordViewModel : MvxViewModel<EmailParameter, EmailParameter>
    {
        private readonly int errorCountBeforeShowingContactSupportSuggestion = 2;
        private readonly ITimeService timeService;
        private readonly IUserAccessManager userAccessManager;
        private readonly IMvxNavigationService navigationService;
        private readonly ISchedulerProvider schedulerProvider;
        private readonly TimeSpan delayAfterPassordReset = TimeSpan.FromSeconds(4);

        public BehaviorSubject<Email> Email { get; } = new BehaviorSubject<Email>(Multivac.Email.Empty);
        public UIAction Reset { get; }
        public UIAction Close { get; }
        public UIAction ContactUs { get; }
        public IObservable<bool> SuggestContactSupport { get; }

        public ForgotPasswordViewModel(
            ITimeService timeService,
            IUserAccessManager userAccessManager,
            IMvxNavigationService navigationService,
            ISchedulerProvider schedulerProvider)
        {
            Ensure.Argument.IsNotNull(timeService, nameof(timeService));
            Ensure.Argument.IsNotNull(userAccessManager, nameof(userAccessManager));
            Ensure.Argument.IsNotNull(navigationService, nameof(navigationService));
            Ensure.Argument.IsNotNull(schedulerProvider, nameof(schedulerProvider));

            this.timeService = timeService;
            this.userAccessManager = userAccessManager;
            this.navigationService = navigationService;
            this.schedulerProvider = schedulerProvider;

            Reset = UIAction.FromObservable(reset, Email.Select(email => email.IsValid));

            Close = UIAction.FromAction(returnAndFillEmail, Reset.Executing.Invert());

            ContactUs = UIAction.FromAsync(contactUs);

            SuggestContactSupport = Reset.Errors
                .Skip(errorCountBeforeShowingContactSupportSuggestion)
                .SelectValue(true)
                .StartWith(false)
                .AsDriver(false, schedulerProvider);
        }

        public override void Prepare(EmailParameter parameter)
        {
            Email.OnNext(parameter.Email);
        }

        private IObservable<Unit> reset()
        {
            return userAccessManager
                .ResetPassword(Email.Value)
                .ObserveOn(schedulerProvider.MainScheduler)
                .Do(closeWithDelay)
                .SelectUnit()
                .Catch<Unit, Exception>(e => throw createUIException(e));
        }

        private void closeWithDelay()
        {
            timeService.RunAfterDelay(delayAfterPassordReset, returnAndFillEmail);
        }

        private void returnAndFillEmail()
        {
            navigationService.Close(this, EmailParameter.With(Email.Value));
        }

        private Task contactUs() =>
            navigationService
                .Navigate<BrowserViewModel, BrowserParameters>(
                    BrowserParameters.WithUrlAndTitle(Resources.ContactUsUrl, Resources.ContactUs));

        private Exception createUIException(Exception exception)
        {
            switch (exception)
            {
                case BadRequestException _:
                    return new Exception(Resources.PasswordResetEmailDoesNotExistError);

                case OfflineException _:
                    return new Exception(Resources.PasswordResetOfflineError);

                case ApiException apiException:
                    return new Exception(apiException.LocalizedApiErrorMessage);

                default:
                    return new Exception(Resources.PasswordResetGeneralError);
            }
        }
    }
}

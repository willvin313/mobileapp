using System;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using FsCheck;
using FsCheck.Xunit;
using NSubstitute;
using Toggl.Foundation.MvvmCross.Parameters;
using Toggl.Foundation.MvvmCross.ViewModels;
using Toggl.Foundation.Tests.Extensions;
using Toggl.Foundation.Tests.Generators;
using Toggl.Multivac;
using Toggl.Ultrawave.Exceptions;
using Toggl.Ultrawave.Network;
using Xunit;

namespace Toggl.Foundation.Tests.MvvmCross.ViewModels
{
    public sealed class ForgotPasswordViewModelTests
    {
        public abstract class ForgotPasswordViewModelTest : BaseViewModelTests<ForgotPasswordViewModel>
        {
            protected Email ValidEmail { get; } = Email.From("person@company.com");
            protected Email InvalidEmail { get; } = Email.From("This is not an email");

            protected override ForgotPasswordViewModel CreateViewModel()
                => new ForgotPasswordViewModel(TimeService, UserAccessManager, NavigationService,
                    SchedulerProvider);
        }

        public sealed class TheConstructor : ForgotPasswordViewModelTest
        {
            [Theory, LogIfTooSlow]
            [ConstructorData]
            public void ThrowsIfAnyOfTheArgumentsIsNull(
                bool useTimeService,
                bool useUserAccessManager,
                bool useNavigationService,
                bool useSchedulerProvider)
            {
                var timeService = useTimeService ? TimeService : null;
                var userAccessManager = useUserAccessManager ? UserAccessManager : null;
                var navigationService = useNavigationService ? NavigationService : null;
                var schedulerProvider = useSchedulerProvider ? SchedulerProvider : null;

                Action tryingToConstructWithEmptyParameters =
                    () => new ForgotPasswordViewModel(
                        timeService, userAccessManager, navigationService, schedulerProvider);

                tryingToConstructWithEmptyParameters
                    .Should().Throw<ArgumentNullException>();
            }
        }

        public sealed class ThePrepareMethod : ForgotPasswordViewModelTest
        {
            [Property]
            public void SetsTheEmail(NonEmptyString emailString)
            {
                var email = Email.From(emailString.Get);

                ViewModel.Prepare(EmailParameter.With(email));

                ViewModel.Email.Value.Should().Be(email);
            }
        }

        public sealed class TheResetAction : ForgotPasswordViewModelTest
        {
            [Fact, LogIfTooSlow]
            public async Task ResetsThePassword()
            {
                UserAccessManager
                    .ResetPassword(Arg.Any<Email>())
                    .Returns(Observable.Return("Great success"));
                var observer = TestScheduler.CreateObserver<Unit>();
                ViewModel.ResetPassword.Elements.Subscribe(observer);
                TestScheduler.Start();

                ViewModel.Email.OnNext(ValidEmail);

                ViewModel.ResetPassword.Execute(Unit.Default);

                UserAccessManager.Received().ResetPassword(ValidEmail);
            }

            [Fact, LogIfTooSlow]
            public void ShouldBeEnableIfEmailIsValid()
            {
                var observer = TestScheduler.CreateObserver<bool>();
                ViewModel.ResetPassword.Enabled.Subscribe(observer);
                TestScheduler.Start();

                ViewModel.Email.OnNext(ValidEmail);

                observer.LastValue().Should().BeTrue();
            }

            [Fact, LogIfTooSlow]
            public void ShouldBeEnabledEvenIfEmailIsInvalid()
            {
                var observer = TestScheduler.CreateObserver<bool>();
                ViewModel.ResetPassword.Enabled.Subscribe(observer);
                TestScheduler.Start();

                ViewModel.Email.OnNext(InvalidEmail);

                observer.LastValue().Should().BeTrue();
            }

            [Fact, LogIfTooSlow]
            public void ShouldBeEnabledEvenIfEmailIsEmpty()
            {
                var observer = TestScheduler.CreateObserver<bool>();
                ViewModel.ResetPassword.Enabled.Subscribe(observer);

                observer.LastValue().Should().BeTrue();
            }

            [Fact, LogIfTooSlow]
            public void ShouldEmitsEmailInvalidExceptionWhenEmailIsInvalid()
            {
                var observer = TestScheduler.CreateObserver<Exception>();
                ViewModel.ResetPassword.Errors.Subscribe(observer);
                TestScheduler.Start();

                ViewModel.ResetPassword.Execute();

                observer.LastValue().Message.Should().Be(Resources.PasswordResetInvalidEmailError);
            }

            public sealed class WhenPasswordResetSucceeds : ForgotPasswordViewModelTest
            {
                [Fact, LogIfTooSlow]
                public async Task SetsPasswordResetSuccessfulToTrue()
                {
                    ViewModel.Email.OnNext(ValidEmail);
                    UserAccessManager
                        .ResetPassword(Arg.Any<Email>())
                        .Returns(Observable.Return("Great success"));

                    var observer = TestScheduler.CreateObserver<Unit>();
                    ViewModel.ResetPassword.Elements.Subscribe(observer);

                    ViewModel.ResetPassword.Execute(Unit.Default);
                    TestScheduler.Start();

                    observer.Messages.Should().HaveCount(1);
                }

                [Fact, LogIfTooSlow]
                public async Task CallsTimeServiceToCloseViewModelAfterFourSeconds()
                {
                    ViewModel.Email.OnNext(ValidEmail);
                    UserAccessManager
                        .ResetPassword(Arg.Any<Email>())
                        .Returns(Observable.Return("Great success"));

                    ViewModel.ResetPassword.Execute(Unit.Default);
                    TestScheduler.Start();

                    TimeService.Received().RunAfterDelay(TimeSpan.FromSeconds(4), Arg.Any<Action>());
                }

                [Fact, LogIfTooSlow]
                public async Task ClosesTheViewModelAfterFourSecondDelay()
                {
                    var timeService = new TimeService(TestScheduler);
                    var viewModel = new ForgotPasswordViewModel(
                        timeService, UserAccessManager, NavigationService, SchedulerProvider);
                    viewModel.Email.OnNext(ValidEmail);

                    UserAccessManager
                        .ResetPassword(Arg.Any<Email>())
                        .Returns(Observable.Return("Great success"));

                    viewModel.ResetPassword.Execute(Unit.Default);

                    TestScheduler.Start();
                    TestScheduler.AdvanceBy(TimeSpan.FromSeconds(4).Ticks);

                    NavigationService
                        .Received()
                        .Close(
                            viewModel,
                            Arg.Is<EmailParameter>(
                                parameter => parameter.Email.Equals(ValidEmail)));
                }
            }

            public sealed class WhenPasswordResetFails : ForgotPasswordViewModelTest
            {
                [Fact, LogIfTooSlow]
                public async Task EmitsFalseResultWhenItFails()
                {
                    var exception = new Exception();
                    ViewModel.Email.OnNext(ValidEmail);
                    UserAccessManager
                        .ResetPassword(Arg.Any<Email>())
                        .Returns(Observable.Throw<string>(exception));

                    var observer = TestScheduler.CreateObserver<Exception>();
                    ViewModel.ResetPassword.Errors.Subscribe(observer);

                    ViewModel.ResetPassword.Execute(Unit.Default);
                    TestScheduler.Start();

                    observer.Messages.Should().HaveCount(1);
                }

                [Fact, LogIfTooSlow]
                public async Task SetsNoEmailErrorWhenReceivesBadRequestException()
                {
                    ViewModel.Email.OnNext(ValidEmail);
                    var exception = new BadRequestException(
                        Substitute.For<IRequest>(), Substitute.For<IResponse>());
                    UserAccessManager
                        .ResetPassword(Arg.Any<Email>())
                        .Returns(Observable.Throw<string>(exception));

                    var observer = TestScheduler.CreateObserver<Exception>();
                    ViewModel.ResetPassword.Errors.Subscribe(observer);

                    ViewModel.ResetPassword.Execute(Unit.Default);
                    TestScheduler.Start();

                    observer.LastValue().Message.Should().Be(Resources.PasswordResetEmailDoesNotExistError);
                }

                [Fact, LogIfTooSlow]
                public async Task SetsOfflineErrorWhenReceivesOfflineException()
                {
                    ViewModel.Email.OnNext(ValidEmail);
                    UserAccessManager
                        .ResetPassword(Arg.Any<Email>())
                        .Returns(Observable.Throw<string>(new OfflineException()));

                    var observer = TestScheduler.CreateObserver<Exception>();
                    ViewModel.ResetPassword.Errors.Subscribe(observer);

                    ViewModel.ResetPassword.Execute(Unit.Default);
                    TestScheduler.Start();

                    observer.LastValue().Message.Should().Be(Resources.PasswordResetOfflineError);
                }

                [Fact, LogIfTooSlow]
                public async Task SetsApiErrorWhenReceivesApiException()
                {
                    ViewModel.Email.OnNext(ValidEmail);
                    var response = Substitute.For<IResponse>();
                    var message = "Some error message";
                    response.RawData.Returns(message);
                    var exception = new ApiException(
                        Substitute.For<IRequest>(),
                        response,
                        message);
                    UserAccessManager
                        .ResetPassword(Arg.Any<Email>())
                        .Returns(Observable.Throw<string>(exception));

                    var observer = TestScheduler.CreateObserver<Exception>();
                    ViewModel.ResetPassword.Errors.Subscribe(observer);

                    ViewModel.ResetPassword.Execute(Unit.Default);
                    TestScheduler.Start();

                    observer.LastValue().Message.Should().Be(exception.LocalizedApiErrorMessage);
                }

                [Fact, LogIfTooSlow]
                public async Task SetsGeneralErrorForAnyOtherException()
                {
                    ViewModel.Email.OnNext(ValidEmail);
                    UserAccessManager
                        .ResetPassword(Arg.Any<Email>())
                        .Returns(Observable.Throw<string>(new Exception()));

                    var observer = TestScheduler.CreateObserver<Exception>();
                    ViewModel.ResetPassword.Errors.Subscribe(observer);

                    ViewModel.ResetPassword.Execute(Unit.Default);
                    TestScheduler.Start();

                    observer.LastValue().Message.Should().Be(Resources.PasswordResetGeneralError);
                }
            }
        }

        public sealed class TheSuggestContactSupportProperty : ForgotPasswordViewModelTest
        {
            [Fact, LogIfTooSlow]
            public async Task StartsWithFalse()
            {
                var observer = TestScheduler.CreateObserver<bool>();
                ViewModel.SuggestContactSupport.Subscribe(observer);
                TestScheduler.Start();

                observer.LastValue().Should().BeFalse();
            }

            [Fact, LogIfTooSlow]
            public async Task EmitsTrueAfterACertainNumberOfErrors()
            {
                var threshold = 3;
                ViewModel.Email.OnNext(ValidEmail);
                UserAccessManager
                    .ResetPassword(Arg.Any<Email>())
                    .Returns(Observable.Throw<string>(new Exception()));

                var observer = TestScheduler.CreateObserver<bool>();
                ViewModel.SuggestContactSupport.Subscribe(observer);

                Observable.Concat(
                        Enumerable
                            .Range(1, threshold)
                            .Select(n
                                => Observable.Defer(() => ViewModel.ResetPassword.Execute(Unit.Default)).Catch(Observable.Return(Unit.Default)))
                    )
                    .Subscribe();

                TestScheduler.Start();

                observer.LastValue().Should().BeTrue();
            }
        }

        public sealed class TheCloseAction : ForgotPasswordViewModelTest
        {
            [Fact, LogIfTooSlow]
            public async Task ClosesTheViewModelReturningTheEmail()
            {
                var email = Email.From("peterpan@pan.com");
                ViewModel.Email.OnNext(email);

                await ViewModel.Close.Execute();

                NavigationService
                    .Received()
                    .Close(
                        ViewModel,
                        Arg.Is<EmailParameter>(
                            parameter => parameter.Email.Equals(email)));
            }
        }

        public sealed class TheContactUsAction : ForgotPasswordViewModelTest
        {
            [Fact, LogIfTooSlow]
            public async Task OpensTheBrowserWithTheAppropriateTitle()
            {
                await ViewModel.ContactUs.Execute();

                await NavigationService.Received().Navigate<BrowserViewModel, BrowserParameters>(
                    Arg.Is<BrowserParameters>(parameter => parameter.Title == Resources.ContactUs)
                );
            }

            [Fact, LogIfTooSlow]
            public async Task OpensTheBrowserWithTheCorrectURL()
            {
                await ViewModel.ContactUs.Execute();

                await NavigationService.Received().Navigate<BrowserViewModel, BrowserParameters>(
                    Arg.Is<BrowserParameters>(parameter => parameter.Url == Resources.ContactUsUrl)
                );
            }
        }
    }
}
using System;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Reactive.Testing;
using NSubstitute;
using Toggl.Foundation.DataSources;
using Toggl.Foundation.MvvmCross.ViewModels;
using Toggl.Foundation.Tests.Extensions;
using Toggl.Foundation.Tests.Generators;
using Toggl.Multivac;
using Toggl.Multivac.Models;
using Toggl.PrimeRadiant.Settings;
using Toggl.Ultrawave.Network;
using Xunit;
using Unit = System.Reactive.Unit;

namespace Toggl.Foundation.Tests.MvvmCross.ViewModels
{
    public sealed class SignupViewModelTests
    {
        public abstract class SignupViewModelTest : BaseViewModelTests<SignupViewModel>
        {
            protected Email ValidEmail { get; } = Email.From("susancalvin@psychohistorian.museum");
            protected Email InvalidEmail { get; } = Email.From("foo@");

            protected Password ValidPassword { get; } = Password.From("123456");
            protected Password InvalidPassword { get; } = Password.Empty;

            protected ILocation Location { get; } = Substitute.For<ILocation>();
            protected ILastTimeUsageStorage LastTimeUsageStorage { get; } = Substitute.For<ILastTimeUsageStorage>();

            protected override SignupViewModel CreateViewModel()
                => new SignupViewModel(
                    ApiFactory,
                    UserAccessManager,
                    OnboardingStorage,
                    NavigationService,
                    ErrorHandlingService,
                    LastTimeUsageStorage,
                    TimeService,
                    SchedulerProvider);

            protected override void AdditionalSetup()
            {
                Location.CountryCode.Returns("LV");
                Location.CountryName.Returns("Latvia");

                Api.Location.Get().Returns(Observable.Return(Location));

                ApiFactory.CreateApiWith(Arg.Any<Credentials>()).Returns(Api);
            }
        }

        public sealed class TheConstructor : SignupViewModelTest
        {
            [Xunit.Theory, LogIfTooSlow]
            [ConstructorData]
            public void ThrowsIfAnyOfTheArgumentsIsNull(
                bool useApiFactory,
                bool useUserAccessManager,
                bool useOnboardingStorage,
                bool userNavigationService,
                bool useApiErrorHandlingService,
                bool useLastTimeUsageStorage,
                bool useTimeService,
                bool useSchedulerProvider)
            {
                var apiFactory = useApiFactory ? ApiFactory : null;
                var userAccessManager = useUserAccessManager ? UserAccessManager : null;
                var onboardingStorage = useOnboardingStorage ? OnboardingStorage : null;
                var navigationService = userNavigationService ? NavigationService : null;
                var apiErrorHandlingService = useApiErrorHandlingService ? ErrorHandlingService : null;
                var lastTimeUsageService = useLastTimeUsageStorage ? LastTimeUsageStorage : null;
                var timeService = useTimeService ? TimeService : null;
                var schedulerProvider = useSchedulerProvider ? SchedulerProvider : null;

                Action tryingToConstructWithEmptyParameters =
                    () => new SignupViewModel(
                        apiFactory,
                        userAccessManager,
                        onboardingStorage,
                        navigationService,
                        apiErrorHandlingService,
                        lastTimeUsageService,
                        timeService,
                        schedulerProvider);

                tryingToConstructWithEmptyParameters
                    .Should().Throw<ArgumentNullException>();
            }
        }

        public sealed class TheInitializeMethod : SignupViewModelTest
        {
            [Fact, LogIfTooSlow]
            public async Task GetstheCurrentLocation()
            {
                await ViewModel.Initialize();

                await Api.Location.Received().Get();
            }
        }

        public sealed class TheClearEmailScreenError : SignupViewModelTest
        {
            [Fact, LogIfTooSlow]
            public void DoesNotEmitWhenEmailIsInValid()
            {
                ViewModel.EmailRelay.Accept(InvalidEmail.ToString());
                var observer = TestScheduler.CreateObserver<Unit>();
                ViewModel.ClearEmailScreenError.Subscribe(observer);

                TestScheduler.Start();

                observer.Messages.Should().BeEmpty();
            }

            [Fact, LogIfTooSlow]
            public void EmitsElementWhenEmailIsValid()
            {
                ViewModel.EmailRelay.Accept(ValidEmail.ToString());
                var observer = TestScheduler.CreateObserver<Unit>();
                ViewModel.ClearEmailScreenError.Subscribe(observer);

                TestScheduler.Start();

                observer.Messages.Should().HaveCount(1);
            }

            [Fact, LogIfTooSlow]
            public void EmitsElementWhenEmailTransitionFromInvalidToValid()
            {
                var observer = TestScheduler.CreateObserver<Unit>();
                ViewModel.ClearEmailScreenError.Subscribe(observer);
                ViewModel.EmailRelay.Accept(InvalidEmail.ToString());
                ViewModel.EmailRelay.Accept(ValidEmail.ToString());

                TestScheduler.Start();

                observer.Messages.Should().HaveCount(1);
            }
        }

        public sealed class TheClearPasswordScreenError : SignupViewModelTest
        {
            [Theory]
            [InlineData(false, false, false)]
            [InlineData(false, true, false)]
            [InlineData(true, false, false)]
            [InlineData(true, true, true)]
            public void EmitAppropriateValue(bool emailValid, bool passwordValid, bool shouldEmit)
            {
                ViewModel.EmailRelay.Accept(emailValid ? ValidEmail.ToString() : InvalidEmail.ToString());
                ViewModel.PasswordRelay.Accept(passwordValid ? ValidPassword.ToString() : InvalidPassword.ToString());
                var observer = TestScheduler.CreateObserver<Unit>();
                ViewModel.ClearPasswordScreenError.Subscribe(observer);

                TestScheduler.Start();

                observer.Messages.Should().HaveCount(shouldEmit ? 1 : 0);
            }
        }

        public sealed class TheSignUpWithEmailAction : SignupViewModelTest
        {
            [Fact, LogIfTooSlow]
            public void EmitsErrorWhenEmailIsInvalid()
            {
                ViewModel.EmailRelay.Accept(InvalidEmail.ToString());
                var observer = TestScheduler.CreateObserver<Exception>();
                ViewModel.SignupWithEmail.Errors.Subscribe(observer);

                TestScheduler.Start();
                ViewModel.SignupWithEmail.Execute();

                observer.Messages.Should().HaveCount(1);
                observer.Messages.Last().Value.Value.Message.Should().Be(Resources.EnterValidEmail);
            }

            [Fact, LogIfTooSlow]
            public void EmitsElementWhenEmailIsValid()
            {
                ViewModel.EmailRelay.Accept(ValidEmail.ToString());
                var observer = TestScheduler.CreateObserver<Unit>();
                ViewModel.SignupWithEmail.Elements.Subscribe(observer);

                TestScheduler.Start();
                ViewModel.SignupWithEmail.Execute();

                observer.Messages.Should().HaveCount(1);
            }
        }

        public sealed class TheTogglePasswordVisibilityMethod : SignupViewModelTest
        {
            [Fact, LogIfTooSlow]
            public void SetsTheIsPasswordMaskedToFalseWhenItIsTrue()
            {
                var observer = TestScheduler.CreateObserver<bool>();

                ViewModel.IsPasswordMasked.Subscribe(observer);
                ViewModel.TogglePasswordVisibility.Execute();

                TestScheduler.Start();
                observer.Messages.AssertEqual(
                    ReactiveTest.OnNext(1, true),
                    ReactiveTest.OnNext(2, false)
                );
            }

            [Fact, LogIfTooSlow]
            public void SetsTheIsPasswordMaskedToTrueWhenItIsFalse()
            {
                var observer = TestScheduler.CreateObserver<bool>();

                ViewModel.IsPasswordMasked.Subscribe(observer);
                ViewModel.TogglePasswordVisibility.Execute();

                ViewModel.TogglePasswordVisibility.Execute();

                TestScheduler.Start();
                observer.Messages.AssertEqual(
                    ReactiveTest.OnNext(1, true),
                    ReactiveTest.OnNext(2, false),
                    ReactiveTest.OnNext(3, true)
                );
            }
        }

        public sealed class TheShakeTargetsProperty : SignupViewModelTest
        {
            [Fact, LogIfTooSlow]
            public void ShouldEmitEmailWhenEmailIsInvalidWhenContinueToPasswordScreen()
            {
                ViewModel.EmailRelay.Accept(InvalidEmail.ToString());
                ViewModel.PasswordRelay.Accept(ValidPassword.ToString());
                var observer = TestScheduler.CreateObserver<SignupViewModel.ShakeTarget>();
                ViewModel.Shake.Subscribe(observer);

                ViewModel.SignupWithEmail.Execute();
                TestScheduler.Start();

                observer.LastValue().Should().Be(SignupViewModel.ShakeTarget.Email);
            }

            [Fact, LogIfTooSlow]
            public void ShouldEmitPasswordWhenPasswordIsInvalid()
            {
                ViewModel.EmailRelay.Accept(ValidEmail.ToString());
                ViewModel.PasswordRelay.Accept(InvalidPassword.ToString());
                var observer = TestScheduler.CreateObserver<SignupViewModel.ShakeTarget>();
                ViewModel.Shake.Subscribe(observer);

                ViewModel.GotoCountrySelection.Execute();
                TestScheduler.Start();

                observer.LastValue().Should().Be(SignupViewModel.ShakeTarget.Password);
            }

            [Fact, LogIfTooSlow]
            public void ShouldNotEmitWhenEmailAndPasswordAreValid()
            {
                ViewModel.EmailRelay.Accept(ValidEmail.ToString());
                ViewModel.PasswordRelay.Accept(ValidPassword.ToString());
                var observer = TestScheduler.CreateObserver<SignupViewModel.ShakeTarget>();
                ViewModel.Shake.Subscribe(observer);

                ViewModel.GotoCountrySelection.Execute();
                TestScheduler.Start();

                observer.Messages.Should().BeEmpty();
            }
        }

        public sealed class TheBackAction : SignupViewModelTest
        {
            [Fact, LogIfTooSlow]
            public void ShouldCallNavigationServiceCloseWhenInTheFirstScreen()
            {
                ViewModel.Back.Execute();
                TestScheduler.Start();

                NavigationService.Received().Close(ViewModel);
            }

            [Fact, LogIfTooSlow]
            public void ShouldBeDisabledIfSigningUp()
            {
                ViewModel.Initialize();
                ViewModel.EmailRelay.Accept(ValidEmail.ToString());
                ViewModel.PasswordRelay.Accept(ValidPassword.ToString());
                UserAccessManager.SignUp(Arg.Any<Email>(), Arg.Any<Password>(), Arg.Any<bool>(), Arg.Any<int>())
                    .Returns(Observable.Never<ITogglDataSource>());
                var observer = TestScheduler.CreateObserver<bool>();
                ViewModel.Back.Enabled.Subscribe(observer);

                ViewModel.ToggleTOSAgreement.Execute();
                ViewModel.SignUp.Execute();
                TestScheduler.Start();

                observer.LastValue().Should().BeFalse();
            }


            [Fact, LogIfTooSlow]
            public void ShouldBeEnabledByDefault()
            {
                var observer = TestScheduler.CreateObserver<bool>();
                ViewModel.Back.Enabled.Subscribe(observer);

                TestScheduler.Start();

                observer.LastValue().Should().BeTrue();
            }

            [Fact, LogIfTooSlow]
            public void ShouldClearThePassword()
            {
                ViewModel.EmailRelay.Accept(ValidEmail.ToString());
                var observer = TestScheduler.CreateObserver<string>();
                ViewModel.PasswordRelay.Accept("somePassword");
                ViewModel.PasswordRelay.Subscribe(observer);

                ViewModel.SignupWithEmail.Execute();
                ViewModel.Back.Execute();
                TestScheduler.Start();

                observer.Messages.AssertEqual(
                   ReactiveTest.OnNext(0, "somePassword"),
                   ReactiveTest.OnNext(0, string.Empty)
                );
            }
        }

        public sealed class TheGoToCountrySelectionAction : SignupViewModelTest
        {
            [Fact, LogIfTooSlow]
            public void EmitErrorWhenEmailIsInvalid()
            {
                var observer = TestScheduler.CreateObserver<Exception>();
                ViewModel.GotoCountrySelection.Errors.Subscribe(observer);
                ViewModel.EmailRelay.Accept(InvalidEmail.ToString());

                ViewModel.GotoCountrySelection.Execute();
                TestScheduler.Start();

                observer.LastValue().Message.Should().Be(Resources.EnterValidEmail);
            }

            [Fact, LogIfTooSlow]
            public void EmitErrorWhenPasswordIsTooShort()
            {
                var observer = TestScheduler.CreateObserver<Exception>();
                ViewModel.GotoCountrySelection.Errors.Subscribe(observer);
                ViewModel.EmailRelay.Accept(ValidEmail.ToString());
                ViewModel.PasswordRelay.Accept(InvalidPassword.ToString());

                ViewModel.GotoCountrySelection.Execute();
                TestScheduler.Start();

                observer.LastValue().Message.Should().Be(Resources.PasswordTooShort);
            }

            [Fact, LogIfTooSlow]
            public void ShouldChangeStateToCountrySelection()
            {
                var observer = TestScheduler.CreateObserver<bool>();
                ViewModel.IsCountrySelectionScreenVisible.Subscribe(observer);
                ViewModel.EmailRelay.Accept(ValidEmail.ToString());
                ViewModel.PasswordRelay.Accept(ValidEmail.ToString());

                ViewModel.GotoCountrySelection.Execute();
                TestScheduler.Start();

                observer.LastValue().Should().BeTrue();
            }
        }
    }
}

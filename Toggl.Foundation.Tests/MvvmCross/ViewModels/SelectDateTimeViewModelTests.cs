using System;
using System.Threading.Tasks;
using FluentAssertions;
using FsCheck.Xunit;
using NSubstitute;
using Toggl.Foundation.MvvmCross.Parameters;
using Toggl.Foundation.MvvmCross.ViewModels;
using Toggl.Foundation.Tests.Generators;
using Xunit;

namespace Toggl.Foundation.Tests.MvvmCross.ViewModels
{
    public class SelectDateTimeViewModelTests
    {
        public class SelectDateTimeDialogViewModelTest : BaseViewModelTests<SelectDateTimeViewModel>
        {
            protected override SelectDateTimeViewModel CreateViewModel()
                => new SelectDateTimeViewModel(RxActionFactory, NavigationService);

            protected DateTimePickerParameters GenerateParameterForTime(DateTimeOffset now)
                => DateTimePickerParameters.WithDates(DateTimePickerMode.DateTime, now, now.AddHours(-1), now.AddHours(+1));
        }

        public class TheConstructor : SelectDateTimeDialogViewModelTest
        {
            [Theory, LogIfTooSlow]
            [ConstructorData]
            public void ThrowsIfAnyOfTheArgumentsIsNull(
                bool useRxActionFactory,
                bool useNavigationService)
            {
                var rxActionFactory = useRxActionFactory ? RxActionFactory : null;
                var navigationService = useNavigationService ? NavigationService : null;

                Action tryingToConstructWithEmptyParameter
                    = () => new SelectDateTimeViewModel(rxActionFactory, navigationService);

                tryingToConstructWithEmptyParameter.Should().Throw<ArgumentNullException>();
            }
        }

        public class TheCurrentDateTimeCommand : SelectDateTimeDialogViewModelTest
        {
            [Property]
            public void DoesNotAcceptValuesGreaterThanTheMaxValue(DateTimeOffset now)
            {
                if (DateTimeOffset.MinValue.AddHours(1) <= now ||
                    DateTimeOffset.MaxValue.AddHours(-1) >= now) return;

                var parameter = GenerateParameterForTime(now);
                ViewModel.Prepare(parameter);

                ViewModel.CurrentDateTime.Accept(parameter.MaxDate.AddMinutes(3));

                ViewModel.CurrentDateTime.Value.Should().Be(parameter.MaxDate);
            }

            [Property]
            public void DoesNotAcceptValuesSmallerThanTheMinValue(DateTimeOffset now)
            {
                if (DateTimeOffset.MinValue.AddHours(1) <= now ||
                    DateTimeOffset.MaxValue.AddHours(-1) >= now) return;

                var parameter = GenerateParameterForTime(now);
                ViewModel.Prepare(parameter);

                ViewModel.CurrentDateTime.Accept(parameter.MinDate.AddMinutes(-3));

                ViewModel.CurrentDateTime.Value.Should().Be(parameter.MinDate);
            }
        }

        public class TheCloseCommand : SelectDateTimeDialogViewModelTest
        {
            [Fact, LogIfTooSlow]
            public async Task ClosesTheViewModel()
            {
                ViewModel.CloseCommand.Execute();

                await NavigationService.Received().Close(Arg.Is(ViewModel), Arg.Any<DateTimeOffset>());
            }

            [Property]
            public void ReturnsTheDefaultParameter(DateTimeOffset now)
            {
                if (DateTimeOffset.MinValue.AddHours(1) <= now ||
                    DateTimeOffset.MaxValue.AddHours(-1) >= now) return;

                var parameter = GenerateParameterForTime(now);
                ViewModel.Prepare(parameter);

                ViewModel.CloseCommand.Execute();

                NavigationService.Received().Close(Arg.Is(ViewModel), Arg.Is(now)).Wait();
            }
        }

        public class TheSaveCommand : SelectDateTimeDialogViewModelTest
        {
            [Fact, LogIfTooSlow]
            public async Task ClosesTheViewModel()
            {
                ViewModel.CloseCommand.Execute();

                await NavigationService.Received().Close(Arg.Is(ViewModel), Arg.Any<DateTimeOffset>());
            }

            [Property]
            public void ReturnsAValueThatReflectsTheChangesToDuration(DateTimeOffset now, DateTimeOffset dateTimeOffset)
            {
                if (DateTimeOffset.MinValue.AddHours(1) <= dateTimeOffset ||
                    DateTimeOffset.MaxValue.AddHours(-1) >= dateTimeOffset) return;

                var parameter = GenerateParameterForTime(dateTimeOffset);
                parameter.CurrentDate = now;
                ViewModel.Prepare(parameter);
                ViewModel.CurrentDateTime.Accept(dateTimeOffset);

                ViewModel.SaveCommand.Execute();
                
                NavigationService.Received()
                    .Close(Arg.Is(ViewModel), Arg.Is<DateTimeOffset>(p => p == dateTimeOffset))
                    .Wait();
            }
        }
    }
}

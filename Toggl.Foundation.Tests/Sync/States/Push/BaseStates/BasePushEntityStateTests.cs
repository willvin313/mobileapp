using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using FluentAssertions;
using NSubstitute;
using Toggl.Foundation.Analytics;
using Toggl.Foundation.Sync;
using Toggl.Foundation.Sync.States.Push;
using Toggl.Foundation.Tests.Helpers;
using Toggl.PrimeRadiant;
using Toggl.Ultrawave.Exceptions;
using Xunit;

namespace Toggl.Foundation.Tests.Sync.States.Push.BaseStates
{
    public abstract class BasePushEntityStateTests
    {
        [Fact, LogIfTooSlow]
        public void ReturnsFailTransitionWhenEntityIsNull()
        {
            var state = CreateState();

            var transition = state.Start(null).SingleAsync().Wait();
            var parameter = ((Transition<Exception>)transition).Parameter;

            transition.Result.Should().Be(state.UnknownError);
            parameter.Should().BeOfType<ArgumentNullException>();
        }

        [Theory, LogIfTooSlow]
        [MemberData(nameof(ApiExceptions.ClientExceptionsWhichAreNotReThrownInSyncStates), MemberType = typeof(ApiExceptions))]
        public void ReturnsClientErrorTransitionWhenHttpFailsWithClientErrorException(ClientErrorException exception)
        {
            var state = CreateState();
            var entity = new TestModel(-1, SyncStatus.SyncNeeded);
            PrepareApiCallFunctionToThrow(exception);

            var transition = state.Start(entity).SingleAsync().Wait();
            var parameter = ((Transition<(Exception Reason, IThreadSafeTestModel)>)transition).Parameter;

            transition.Result.Should().Be(state.ClientError);
            parameter.Reason.Should().BeAssignableTo<ClientErrorException>();
        }

        [Theory, LogIfTooSlow]
        [MemberData(nameof(ApiExceptions.ServerExceptions), MemberType = typeof(ApiExceptions))]
        public void ReturnsServerErrorTransitionWhenHttpFailsWithServerErrorException(ServerErrorException exception)
        {
            var state = CreateState();
            var entity = new TestModel(-1, SyncStatus.SyncNeeded);
            PrepareApiCallFunctionToThrow(exception);

            var transition = state.Start(entity).SingleAsync().Wait();
            var parameter = ((Transition<ServerErrorException>)transition).Parameter;

            transition.Result.Should().Be(state.ServerError);
            parameter.Should().BeAssignableTo<ServerErrorException>();
        }

        [Fact, LogIfTooSlow]
        public void ReturnsUnknownErrorTransitionWhenHttpFailsWithNonApiException()
        {
            var state = CreateState();
            var entity = new TestModel(-1, SyncStatus.SyncNeeded);
            PrepareApiCallFunctionToThrow(new TestException());

            var transition = state.Start(entity).SingleAsync().Wait();
            var parameter = ((Transition<Exception>)transition).Parameter;

            transition.Result.Should().Be(state.UnknownError);
            parameter.Should().BeOfType<TestException>();
        }

        [Fact, LogIfTooSlow]
        public void ReturnsFailTransitionWhenDatabaseOperationFails()
        {
            var state = CreateState();
            var entity = new TestModel(-1, SyncStatus.SyncNeeded);
            PrepareDatabaseOperationToThrow(new TestException());

            var transition = state.Start(entity).SingleAsync().Wait();
            var parameter = ((Transition<Exception>)transition).Parameter;

            transition.Result.Should().Be(state.UnknownError);
            parameter.Should().BeOfType<TestException>();
        }

        [Theory, LogIfTooSlow]
        [MemberData(nameof(ApiExceptions.ExceptionsWhichCauseRethrow), MemberType = typeof(ApiExceptions))]
        [MemberData(nameof(ExtraExceptionsToRethrow))]
        public void ThrowsWhenExceptionsWhichShouldBeRethrownAreCaught(Exception exception)
        {
            var state = CreateState();
            PrepareApiCallFunctionToThrow(exception);
            Exception caughtException = null;

            try
            {
                state.Start(Substitute.For<IThreadSafeTestModel>()).Wait();
            }
            catch (Exception e)
            {
                caughtException = e;
            }

            caughtException.Should().NotBeNull();
            caughtException.Should().BeAssignableTo(exception.GetType());
        }

        public static IEnumerable<object[]> ExtraExceptionsToRethrow => new[]
        {
            new object[] { new OfflineException(new Exception()) }
        };

        protected abstract PushSyncOperation Operation { get; }

        protected abstract BasePushEntityState<IThreadSafeTestModel> CreateState();

        protected abstract void PrepareApiCallFunctionToThrow(Exception e);

        protected abstract void PrepareDatabaseOperationToThrow(Exception e);

        public static IEnumerable<object[]> EntityTypes
            => new[]
            {
                new[] { typeof(IThreadSafeWorkspaceTestModel) },
                new[] { typeof(IThreadSafeUserTestModel) },
                new[] { typeof(IThreadSafeWorkspaceFeatureTestModel) },
                new[] { typeof(IThreadSafePreferencesTestModel) },
                new[] { typeof(IThreadSafeTagTestModel) },
                new[] { typeof(IThreadSafeClientTestModel) },
                new[] { typeof(IThreadSafeProjectTestModel) },
                new[] { typeof(IThreadSafeTaskTestModel) },
                new[] { typeof(IThreadSafeTimeEntryTestModel) },
            };

        protected static IAnalyticsEvent<string> TestSyncAnalyticsExtensionsSearchStrategy(Type entityType, IAnalyticsService analyticsService)
        {
            var testAnalyticsService = (ITestAnalyticsService)analyticsService;

            return typeof(IThreadSafeTestModel).IsAssignableFrom(entityType)
                ? testAnalyticsService.TestEvent
                : SyncAnalyticsExtensions.DefaultSyncAnalyticsExtensionsSearchStrategy(entityType, analyticsService);
        }
    }
}

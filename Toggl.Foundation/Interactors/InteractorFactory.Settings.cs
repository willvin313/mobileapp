using System;
using System.Reactive;
using Toggl.Foundation.Interactors.Settings;

namespace Toggl.Foundation.Interactors
{
    public partial class InteractorFactory : IInteractorFactory
    {
        public IInteractor<IObservable<Unit>> SendFeedback(string message)
            => new SendFeedbackInteractor(
                api.Feedback,
                dataSource.User,
                dataSource.Workspaces,
                dataSource.TimeEntries,
                platformInfo,
                userPreferences,
                lastTimeUsageStorage,
                timeService,
                message);
    }
}

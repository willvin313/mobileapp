using System;
using System.Reactive.Disposables;
using System.Threading.Tasks;
using Foundation;
using Toggl.Daneel.Extensions.Models;
using Toggl.Foundation;
using Toggl.Foundation.Analytics;
using Toggl.Foundation.DataSources;
using Toggl.Foundation.Extensions;
using Toggl.Foundation.Interactors;
using Toggl.Foundation.Models.Interfaces;
using Toggl.Foundation.MvvmCross.Extensions;
using Toggl.Multivac;
using Toggl.Multivac.Extensions;
using WatchConnectivity;
using System.Reactive.Linq;
using Toggl.Daneel.Extensions;
using Toggl.Foundation.Suggestions;
using System.Linq;
using System.Reactive;

namespace Toggl.Daneel.Watch
{
    public sealed class WatchSessionHandler : WCSessionDelegate
    {
        private readonly ITimeService timeService;
        private readonly ITogglDataSource dataSource;
        private readonly IInteractorFactory interactorFactory;
        private readonly ISuggestionProviderContainer suggestionProvider;

        private readonly CompositeDisposable disposeBag = new CompositeDisposable();

        public WatchSessionHandler(
            ITimeService timeService, 
            ITogglDataSource dataSource, 
            IInteractorFactory interactorFactory,
            ISuggestionProviderContainer suggestionProvider)
        {
            Ensure.Argument.IsNotNull(timeService, nameof(timeService));
            Ensure.Argument.IsNotNull(dataSource, nameof(dataSource));
            Ensure.Argument.IsNotNull(interactorFactory, nameof(interactorFactory));
            Ensure.Argument.IsNotNull(suggestionProvider, nameof(suggestionProvider));

            this.timeService = timeService;
            this.dataSource = dataSource;
            this.interactorFactory = interactorFactory;
            this.suggestionProvider = suggestionProvider;

            this.dataSource
                .TimeEntries
                .CurrentlyRunningTimeEntry
                .Subscribe(currentRunningTimeEntryChanged)
                .DisposedBy(disposeBag);

            this.dataSource
                .User
                .Get()
                .Subscribe(currentUserChanged)
                .DisposedBy(disposeBag);

            this.dataSource
                .TimeEntries
                .ItemsChanged()
                .Subscribe(async _ => await updateRecentTimeEntries())
                .DisposedBy(disposeBag);

            Observable
                .CombineLatest(
                    dataSource.Workspaces.ItemsChanged(),
                    dataSource.TimeEntries.ItemsChanged())
                .SelectUnit()
                .StartWith(Unit.Default)
                .Subscribe(async _ => await updateSuggestions())
                .DisposedBy(disposeBag);
        }

        [Export("session:activationDidCompleteWithState:error:")]
        public override void ActivationDidComplete(WCSession session, WCSessionActivationState activationState, NSError error)
        {
            Console.WriteLine("Session activation state: {0}", activationState);
        }

        [Export("sessionReachabilityDidChange:")]
        public override void SessionReachabilityDidChange(WCSession session)
        {
            Console.WriteLine("Session reachability changed: {0}", session.Reachable);
        }

        [Export("session:didReceiveMessage:")]
        public override void DidReceiveMessage(WCSession session, NSDictionary<NSString, NSObject> message)
        {
            Console.WriteLine("Did receive message: {0}", message);

            var action = (message["action"] as NSString).ToString();

            switch (action)
            {
                case "StopRunningTimeEntry":
                    stopRunningTimeEntry();
                    break;
                case "StartTimeEntry":
                    var description = (message["Description"] as NSString).ToString();
                    startTimeEntry(description);
                    break;
                default:
                    Console.WriteLine("Unknown action: {0}", action);
                    break;
            }
        }

        [Export("session:didReceiveMessage:replyHandler:")]
        public override void DidReceiveMessage(WCSession session, NSDictionary<NSString, NSObject> message, WCSessionReplyHandler replyHandler)
        {
            Console.WriteLine("Did receive message: {0}", message);

            var response = new NSDictionary<NSString, NSObject>();
            replyHandler(response);
        }

        private void currentRunningTimeEntryChanged(IThreadSafeTimeEntry timeEntry)
        {
            if (WCSession.DefaultSession.ActivationState != WCSessionActivationState.Activated)
                return;

            var timeEntryDict = timeEntry == null ? null : timeEntry.ToNSDictionary();

            var context = WCSession.DefaultSession.ApplicationContext ?? new NSDictionary<NSString, NSObject>();
            var mutableContext = new NSMutableDictionary<NSString, NSObject>(context);

            mutableContext["LoggedIn"] = new NSNumber(true);

            if (timeEntryDict == null)
            {
                mutableContext.Remove("RunningTimeEntry".ToNSString());
            }
            else
            {
                mutableContext["RunningTimeEntry"] = timeEntryDict;
            }

            var updatedContext = new NSDictionary<NSString, NSObject>(mutableContext.Keys, mutableContext.Values);

            NSError error;
            WCSession.DefaultSession.UpdateApplicationContext(updatedContext, out error);
        }

        private void currentUserChanged(IThreadSafeUser user)
        {
            var context = WCSession.DefaultSession.ApplicationContext ?? new NSDictionary<NSString, NSObject>();
            var mutableContext = new NSMutableDictionary<NSString, NSObject>(context);

            if (user == null)
            {
                mutableContext.Remove("LoggedIn".ToNSString());
            }
            else
            {
                mutableContext["LoggedIn"] = new NSNumber(true);
            }

            var updatedContext = new NSDictionary<NSString, NSObject>(mutableContext.Keys, mutableContext.Values);

            NSError error;
            WCSession.DefaultSession.UpdateApplicationContext(updatedContext, out error);
        }

        private async Task stopRunningTimeEntry()
        {
            await interactorFactory.StopTimeEntry(timeService.CurrentDateTime, TimeEntryStopOrigin.AppleWatch).Execute();
        }

        private async Task startTimeEntry(string description)
        {
            var workspaceId = (await dataSource.User.Get()).DefaultWorkspaceId.Value;
            var prototype = description.AsTimeEntryPrototype(timeService.CurrentDateTime, workspaceId);
            await interactorFactory.CreateTimeEntry(prototype).Execute();
        }

        private async Task updateRecentTimeEntries()
        {
            var timeEntries = await dataSource
                .TimeEntries
                .GetAll(te => te.Start > timeService.CurrentDateTime.ToLocalTime().Date);

            var timeEntriesArray = new NSMutableArray();

            timeEntries
                .Select(timeEntry => timeEntry.ToNSDictionary())
                .ForEach(timeEntriesArray.Add);

            var context = WCSession.DefaultSession.ApplicationContext ?? new NSDictionary<NSString, NSObject>();
            var mutableContext = new NSMutableDictionary<NSString, NSObject>(context);

            mutableContext["TodayTimeEntries"] = timeEntriesArray;

            var updatedContext = new NSDictionary<NSString, NSObject>(mutableContext.Keys, mutableContext.Values);

            NSError error;
            WCSession.DefaultSession.UpdateApplicationContext(updatedContext, out error);
        }

        private async Task updateSuggestions()
        {
            var suggestions = await suggestionProvider
                .Providers
                .Select(provider => provider.GetSuggestions())
                .Aggregate(Observable.Merge)
                .ToArray();

            var suggestionsArray = new NSMutableArray();

            suggestions
                .Select(suggestion => suggestion.ToNSDictionary())
                .ForEach(suggestionsArray.Add);

            var context = WCSession.DefaultSession.ApplicationContext ?? new NSDictionary<NSString, NSObject>();
            var mutableContext = new NSMutableDictionary<NSString, NSObject>(context);

            mutableContext["Suggestions"] = suggestionsArray;

            var updatedContext = new NSDictionary<NSString, NSObject>(mutableContext.Keys, mutableContext.Values);

            NSError error;
            WCSession.DefaultSession.UpdateApplicationContext(updatedContext, out error);
        }
    }
}

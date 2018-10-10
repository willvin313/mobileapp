using System;
using Toggl.Ultrawave;
using Toggl.Daneel.Intents;
using Foundation;
using Intents;

namespace SiriExtension
{
    [Register("IntentHandler")]
    public class IntentHandler : INExtension
    {        
        protected IntentHandler(IntPtr handle) : base(handle)
        {
            // Note: this .ctor should not contain any initialization logic.
        }

        public override NSObject GetHandler(INIntent intent)
        {
            switch (intent)
            {
                case StopTimerIntent _:
                    return new StopTimerIntentHandler(APIHelper.GetTogglAPI());
                case StartTimerIntent _:
                    return new StartTimerIntentHandler(APIHelper.GetTogglAPI());
                default:
                    throw new Exception("Unhandled intent type: ${intent}");
            }
        }
    }
}

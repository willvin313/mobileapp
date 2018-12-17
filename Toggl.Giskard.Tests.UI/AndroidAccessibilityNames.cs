using System;
using Xamarin.UITest.Queries;

namespace Toggl.Tests.UI
{
    public static class Onboarding
    {
        public const string FirstOnboardingElement = LoginButton;
        public const string SkipButton = "";
        public const string NextButton = "";
        public const string LoginButton = "Log in";
        public const string SignUpButton = "New to Toggl";
        public const string FirstLabel = "";
        public const string SecondLabel = "";
        public const string ThirdLabel = "";
        public const string PreviousButton = "";
    }

    public static class Login
    {
        //public const string EmailText = "";
        public static readonly Func<AppQuery, AppQuery> EmailText = x => x.Id("LoginEmailTextField");
        public static readonly Func<AppQuery, AppQuery> ErrorLabel = x => x.Id("InfoTextField");
        public static readonly Func<AppQuery, AppQuery> PasswordText = x => x.Id("LoginPasswordTextField");
        public const string ShowPasswordButton = "";
        public const string ForgotPasswordButton = "LoginForgotPassword";
        public const string BackButton = "Back Button";
        public static readonly Func<AppQuery, AppQuery> NextButton = x => x.Id("LoginNextButton");
    }

    public static class Main
    {
        public static readonly Func<AppQuery, AppQuery> StartTimeEntryButton = x => x.Id("MainPlayButton");
        public static readonly Func<AppQuery, AppQuery> StopTimeEntryButton = x => x.Id("MainStopButton");
    }

    public static class StartTimeEntry
    {
        public static readonly Func<AppQuery, AppQuery> DoneButton = x => x.Id("StartTimeEntryDoneButton");
        public static readonly Func<AppQuery, AppQuery> DescriptionText = x => x.Id("StartTimeEntryDescriptionTextField");
    }
}

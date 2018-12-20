using Xamarin.UITest;

namespace Toggl.Tests.UI.Extensions
{
    public static class SignUpExtensions
    {
        public static void WaitForSignUpScreen(this IApp app)
        {
            app.WaitForLoginScreen();

            app.Tap(Login.SwitchToSignUpLabel);
            app.WaitForElement(SignUp.SignUpButton);
        }

        public static void TrySigningUpAndFail(this IApp app)
        {
            app.Tap(SignUp.SignUpButton);
            app.WaitForElement(Login.ErrorLabel);
        }

        public static void SignUpSuccesfully(this IApp app)
        {
            app.Tap(SignUp.SignUpButton);
            app.WaitForElement(SignUp.GdprButton);

            app.Tap(SignUp.GdprButton);
            app.WaitForElement(Main.StartTimeEntryButton);
        }

        public static void RejectTerms(this IApp app)
        {
            #if __IOS__
            app.Tap(SignUp.GdprCancelButton);
            #else
            app.WaitForElement(SignUp.GdprButton);
            app.Back();
            #endif
        }
    }
}

using System;
using Android.Views;
using Android.Widget;

namespace Toggl.Giskard.Activities
{
    public sealed partial class SignupOrLoginChoiceActivity
    {
        private Button signupButton;
        private Button loginButton;

        protected override void InitializeViews()
        {
            signupButton = FindViewById<Button>(Resource.Id.SignUpButton);
            loginButton = FindViewById<Button>(Resource.Id.LoginButton);
        }
    }
}
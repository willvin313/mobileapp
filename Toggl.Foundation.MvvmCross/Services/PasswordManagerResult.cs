using Toggl.Multivac;

namespace Toggl.Foundation.UI.Services
{
    public sealed class PasswordManagerResult
    {
        public Email Email { get; }
        public Password Password { get; }

        public static PasswordManagerResult None { get; }
            = new PasswordManagerResult(Email.Empty, Password.Empty);

        public PasswordManagerResult(Email email, Password password)
        {
            Email = email;
            Password = password;
        }
    }
}

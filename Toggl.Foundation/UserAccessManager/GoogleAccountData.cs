namespace Toggl.Foundation.Login
{
    public struct GoogleAccountData
    {
        public string Name { get; }
        public string Email { get; }
        public string Token { get; }

        public GoogleAccountData(string name, string email, string token)
        {
            Name = name;
            Email = email;
            Token = token;
        }
    }
}

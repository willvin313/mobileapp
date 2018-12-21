using System;
using Toggl.Foundation.MvvmCross.Reactive;
using Toggl.Giskard.Views;

namespace Toggl.Giskard.Extensions.Reactive
{
    public static class TextInputLayoutWithHelperTextExtensions
    {
        public static Action<string> ErrorText(this IReactive<TextInputLayoutWithHelperText> reactive)
          => errorText => reactive.Base.Error = errorText;
    }
}

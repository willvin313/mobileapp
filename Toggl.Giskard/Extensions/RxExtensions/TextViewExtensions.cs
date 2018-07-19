using System;
using System.Reactive.Linq;
using Android.Text;
using Android.Widget;
using Java.Lang;

namespace Toggl.Giskard.Extensions
{
    public static partial class ViewExtensions
    {
        public static IObservable<string> Text(this TextView textView)
            => Observable
                .FromEventPattern<TextChangedEventArgs>(e => textView.TextChanged += e, e => textView.TextChanged -= e)
                .Select(args => ((EditText)args.Sender).Text);

        public static IObservable<ICharSequence> TextFormatted(this TextView textView)
            => Observable
                .FromEventPattern<TextChangedEventArgs>(e => textView.TextChanged += e, e => textView.TextChanged -= e)
                .Select(args => ((EditText)args.Sender).TextFormatted);

        public static Action<string> BindText(this TextView textView)
            => text => textView.Text = text;
    }
}

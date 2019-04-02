using System;
using Foundation;
using MvvmCross.Binding.BindingContext;
using MvvmCross.IoC;
using MvvmCross.ViewModels;
using Toggl.Daneel.Views;

namespace Toggl.Daneel
{
    // This class is never actually executed, but when Xamarin linking is enabled it does ensure types and properties
    // are preserved in the deployed app
    [Preserve(AllMembers = true)]
    public sealed class LinkerPleaseInclude
    {
        public void Include(MvxTaskBasedBindingContext c)
        {
            c.Dispose();
            var c2 = new MvxTaskBasedBindingContext();
            c2.Dispose();
        }

        public void Include(MvxPropertyInjector injector)
        {
            injector = new MvxPropertyInjector();
        }

        public void Include(MvxViewModelViewTypeFinder typeFinder)
        {
            typeFinder = new MvxViewModelViewTypeFinder(null, null);
        }

        public void Include(ConsoleColor color)
        {
            Console.Write("");
            Console.WriteLine("");
            color = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.ForegroundColor = ConsoleColor.White;
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.ForegroundColor = ConsoleColor.DarkGray;
        }

        public void Include(TextViewWithPlaceholder textView)
        {
            textView.TextColor = textView.TextColor;
        }
    }
}

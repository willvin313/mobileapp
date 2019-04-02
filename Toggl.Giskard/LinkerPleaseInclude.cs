using System;
using Android.Support.Constraints;
using Android.Support.V7.Widget;
using Firebase.Provider;
using MvvmCross.Binding.BindingContext;
using MvvmCross.IoC;
using MvvmCross.Navigation;
using MvvmCross.ViewModels;
using Newtonsoft.Json.Converters;
using V4Space = Android.Support.V4.Widget.Space;

namespace Toggl.Giskard
{
    // This class is never actually executed, but when Xamarin linking is enabled it does so to ensure types and properties
    // are preserved in the deployed app
    public class LinkerPleaseInclude
    {
        public void Include(MvxTaskBasedBindingContext c)
        {
            c.Dispose();
            var c2 = new MvxTaskBasedBindingContext();
            c2.Dispose();
        }

        public void Include(MvxPropertyInjector injector)
        {
            injector = new MvxPropertyInjector ();
        }

        public void Include(MvxViewModelViewTypeFinder typeFinder)
        {
            typeFinder = new MvxViewModelViewTypeFinder(null, null);
        }

        public void Include(MvxNavigationService service, IMvxViewModelLoader loader)
        {
            service = new MvxNavigationService(null, loader);
        }

        public void Include(FitWindowsLinearLayout linearLayout)
        {
            linearLayout = new FitWindowsLinearLayout(null);
            linearLayout = new FitWindowsLinearLayout(null, null);
        }

        public void Include(CardView cardView)
        {
            cardView = new CardView(null);
            cardView = new CardView(null, null);
            cardView = new CardView(null, null, 0);
        }
 
        public void Include(FitWindowsFrameLayout layout)
        {
            layout = new FitWindowsFrameLayout(null);
            layout = new FitWindowsFrameLayout(null, null);
        }

        public void Include(AlertDialogLayout layout)
        {
            layout = new AlertDialogLayout(null);
            layout = new AlertDialogLayout(null, null);
        }

        public void Include(DialogTitle title)
        {
            title = new DialogTitle(null);
            title = new DialogTitle(null, null);
            title = new DialogTitle(null, null, 0);
        }
        public void Include(V4Space space)
        {
            space = new V4Space(null);
            space = new V4Space(null, null);
            space = new V4Space(null, null, 0);
        }
        public void Include(ButtonBarLayout layout)
        {
            layout = new ButtonBarLayout(null, null);
        }
      
        public void Include(StringEnumConverter converter)
        {
            converter = new StringEnumConverter(true);
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

        public void Include(FirebaseInitProvider provider)
        {
            provider = new FirebaseInitProvider();
        }

        public void Include(ConstraintLayout constraintLayout, Guideline guideline)
        {
            constraintLayout = new ConstraintLayout(null);
            constraintLayout = new ConstraintLayout(null, null);
            constraintLayout = new ConstraintLayout(null, null, 1);
            guideline = new Guideline(null);
            guideline = new Guideline(null, null);
            guideline = new Guideline(null, null, 1);
            guideline = new Guideline(null, null, 1, 1);
        }
    }
}

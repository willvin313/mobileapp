using System;
using Toggl.Foundation.MvvmCross.Helper;
using UIKit;

namespace Toggl.Daneel.Extensions
{
    public static partial class UIKitRxExtensions
    {
        public static Action<nfloat> BindConstant(this NSLayoutConstraint constraint)
            => constant => constraint.Constant = constant;

        public static Action<nfloat> BindAnimatedConstant(this NSLayoutConstraint constraint) => constant =>
        {
            constraint.Constant = constant;
            AnimationExtensions.Animate(
                Animation.Timings.EnterTiming,
                Animation.Curves.SharpCurve,
                () => ((UIView)constraint.FirstItem).Superview.LayoutSubviews()
            );
        };
    }
}

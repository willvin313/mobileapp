using UIKit;
using System;
using Toggl.Foundation.UI.Helper;
using Toggl.Multivac;

namespace Toggl.Daneel.Extensions
{
    public static class LayoutConstraintExtensions
    {
        public static void AnimateSetConstant(this NSLayoutConstraint self, nfloat constant, UIView containerView)
        {
            Ensure.Argument.IsNotNull(self, nameof(self));
            Ensure.Argument.IsNotNull(constant, nameof(constant));

            self.Constant = constant;

            Ensure.Argument.IsNotNull(containerView, nameof(containerView));

            UIView.Animate(Animation.Timings.EnterTiming, () => containerView.LayoutIfNeeded());
        }
    }
}

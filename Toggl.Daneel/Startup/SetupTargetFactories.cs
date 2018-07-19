using MvvmCross.Binding.Bindings.Target.Construction;
using MvvmCross.Platforms.Ios.Core;
using Toggl.Daneel.Binding;
using UIKit;
using Toggl.Daneel.Views;

namespace Toggl.Daneel
{
    public partial class Setup
    {
        protected override void FillTargetFactories(IMvxTargetBindingFactoryRegistry registry)
        {
            base.FillTargetFactories(registry);

            registry.RegisterCustomBindingFactory<UIBarButtonItem>(
                BarButtonAnimatedEnabledTargetBinding.BindingName,
                view => new BarButtonAnimatedEnabledTargetBinding(view)
            );

            registry.RegisterCustomBindingFactory<UIBarButtonItem>(
                BarButtonCommandTargetBinding.BindingName,
                view => new BarButtonCommandTargetBinding(view)
            );

            registry.RegisterCustomBindingFactory<UIButton>(
                ButtonAnimatedTitleTargetBinding.BindingName,
                view => new ButtonAnimatedTitleTargetBinding(view)
            );

            registry.RegisterCustomBindingFactory<UIButton>(
                ButtonImageTargetBinding.BindingName,
                view => new ButtonImageTargetBinding(view)
            );

            registry.RegisterCustomBindingFactory<UIDatePicker>(
                DatePickerDateTimeOffsetTargetBinding.BindingName,
                view => new DatePickerDateTimeOffsetTargetBinding(view)
            );

            registry.RegisterCustomBindingFactory<UIImageView>(
                ImageViewAnimatedImageTargetBinding.BindingName,
                view => new ImageViewAnimatedImageTargetBinding(view)
            );

            registry.RegisterCustomBindingFactory<NSLayoutConstraint>(
                LayoutConstraintConstantTargetBinding.BindingName,
                view => new LayoutConstraintConstantTargetBinding(view)
            );

            registry.RegisterCustomBindingFactory<LoginTextField>(
                LoginTextFieldFirstResponderTargetBinding.BindingName,
                view => new LoginTextFieldFirstResponderTargetBinding(view)
            );

            registry.RegisterCustomBindingFactory<UINavigationItem>(
                NavigationItemHidesBackButtonTargetBinding.BindingName,
                view => new NavigationItemHidesBackButtonTargetBinding(view)
            );

            registry.RegisterCustomBindingFactory<UIScrollView>(
                ScrollViewAnimatedCurrentPageTargetBinding.BindingName,
                view => new ScrollViewAnimatedCurrentPageTargetBinding(view)
            );

            registry.RegisterCustomBindingFactory<SpiderOnARopeView>(
                SpiderOnARopeViewIsVisibleTargetBinding.BindingName,
                view => new SpiderOnARopeViewIsVisibleTargetBinding(view)
            );

            registry.RegisterCustomBindingFactory<UISwitch>(
                SwitchAnimatedOnTargetBinding.BindingName,
                view => new SwitchAnimatedOnTargetBinding(view)
            );

            registry.RegisterCustomBindingFactory<UITextField>(
                TextFieldFocusTargetBinding.BindingName,
                view => new TextFieldFocusTargetBinding(view)
            );

            registry.RegisterCustomBindingFactory<UITextField>(
                TextFieldPlaceholderTargetBinding.BindingName,
                view => new TextFieldPlaceholderTargetBinding(view)
            );

            registry.RegisterCustomBindingFactory<UITextField>(
                TextFieldSecureTextEntryTargetBinding.BindingName,
                view => new TextFieldSecureTextEntryTargetBinding(view)
            );

            registry.RegisterCustomBindingFactory<UITextView>(
                TextViewTagListTargetBinding.BindingName,
                view => new TextViewTagListTargetBinding(view)
            );

            registry.RegisterCustomBindingFactory<TextViewWithPlaceholder>(
                TextViewWithPlaceholderTextTargetBinding.BindingName,
                view => new TextViewWithPlaceholderTextTargetBinding(view)
            );

            registry.RegisterCustomBindingFactory<UIView>(
                ViewAnimatedBackgroundTargetBinding.BindingName,
                view => new ViewAnimatedBackgroundTargetBinding(view)
            );

            registry.RegisterCustomBindingFactory<UIView>(
                ViewAnimatedVisibilityTargetBinding.BindingName,
                view => new ViewAnimatedVisibilityTargetBinding(view)
            );

            registry.RegisterCustomBindingFactory<UIView>(
                ViewLongPressCommandTargetBinding.BindingName,
                view => new ViewLongPressCommandTargetBinding(view)
            );

            registry.RegisterCustomBindingFactory<UIView>(
                ViewVisibilityWithFadeTargetBinding.BindingName,
                view => new ViewVisibilityWithFadeTargetBinding(view)
            );
        }
    }
}

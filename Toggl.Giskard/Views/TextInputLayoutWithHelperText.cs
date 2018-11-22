using System;
using Android.Content;
using Android.Content.Res;
using Android.Graphics;
using Android.Runtime;
using Android.Support.Design.Widget;
using Android.Support.V4.Content;
using Android.Support.V4.View;
using Android.Support.V4.View.Animation;
using Android.Support.V4.Widget;
using Android.Support.V7.Widget;
using Android.Util;
using Android.Views;
using Android.Views.Accessibility;
using Android.Widget;
using Java.Lang;
using Toggl.Giskard.Extensions;
using Toggl.Giskard.Helper;
using Exception = Java.Lang.Exception;

namespace Toggl.Giskard.Views
{
    [Register("toggl.giskard.views.TextInputLayoutWithHelperText")]
    public class TextInputLayoutWithHelperText : TextInputLayout, IViewPropertyAnimatorListener
    {
        private readonly FastOutSlowInInterpolator interpolator = new FastOutSlowInInterpolator();
        private const long animationDuration = 200L;
        private readonly Color fallbackTextColorAfterMarshmallow = Color.Magenta;

        private bool errorEnabled;
        private bool helperTextEnabled;

        private ICharSequence helperText;
        private ColorStateList helperTextColor;
        private float helperTextFontSize;
        private int textHelperTextAppearance = Resource.Style.TextInputLayoutRegularTextAppearance;

        private TextView helperTextTextView;

        protected TextInputLayoutWithHelperText(IntPtr javaReference, JniHandleOwnership transfer)
            : base(javaReference, transfer)
        {
        }

        public TextInputLayoutWithHelperText(Context context) : base(context)
        {
        }

        public TextInputLayoutWithHelperText(Context context, IAttributeSet attrs) : base(context, attrs)
        {
            var customsAttrs =
                context.ObtainStyledAttributes(attrs, Resource.Styleable.TextInputLayoutWithHelperText, 0, 0);
            try
            {
                helperText = customsAttrs.GetTextFormatted(Resource.Styleable.TextInputLayoutWithHelperText_helperText);
                helperTextColor =
                    customsAttrs.GetColorStateList(Resource.Styleable.TextInputLayoutWithHelperText_helperTextColor);
                helperTextFontSize =
                    customsAttrs.GetDimensionPixelSize(
                        Resource.Styleable.TextInputLayoutWithHelperText_helperTextSize,
                        12.SpToPixels(context));
            }
            finally
            {
                customsAttrs.Recycle();
            }
        }

        public TextInputLayoutWithHelperText(Context context, IAttributeSet attrs, int defStyleAttr) : base(context,
            attrs, defStyleAttr)
        {
        }


        public override void AddView(View child, int index, ViewGroup.LayoutParams @params)
        {
            base.AddView(child, index, @params);
            if (child?.GetType() != typeof(EditText) || string.IsNullOrWhiteSpace(helperText?.ToString()))
                return;
            HelperText = helperText;
        }

        public override bool ErrorEnabled
        {
            set
            {
                if (errorEnabled == value) return;
                errorEnabled = value;

                if (errorEnabled && helperTextEnabled)
                {
                    HelperTextEnabled = false;
                }

                base.ErrorEnabled = value;

                if (!errorEnabled && !string.IsNullOrWhiteSpace(helperText.ToString()))
                {
                    HelperText = helperText;
                }
            }
        }

        public bool HelperTextEnabled
        {
            set
            {
                if (value == helperTextEnabled) return;
                ErrorEnabled = !value && errorEnabled;

                if (value)
                {
                    if (helperTextTextView == null)
                        helperTextTextView = createHelperTextView();

                    helperTextTextView.Visibility = ViewStates.Invisible;
                    ViewCompat.SetAccessibilityLiveRegion(helperTextTextView, ViewCompat.AccessibilityLiveRegionPolite);
                    AddView(helperTextTextView);
                    if (EditText != null)
                    {
                        ViewCompat.SetPaddingRelative(
                            helperTextTextView,
                            ViewCompat.GetPaddingStart(EditText), 0,
                            ViewCompat.GetPaddingEnd(EditText), EditText.PaddingBottom
                        );
                    }
                }
                else if (helperTextTextView != null)
                {
                    RemoveView(helperTextTextView);
                }

                helperTextEnabled = value;
            }
        }

        private TextView createHelperTextView()
        {
            var textView = new AppCompatTextView(Context);

            setTextViewTextColorFallbackColorWhenMissingStyleAttrs(textView);

            if (helperTextColor != null)
            {
                textView.SetTextColor(helperTextColor);
            }

            if (helperTextFontSize > 0)
            {
                textView.SetTextSize(ComplexUnitType.Px, helperTextFontSize);
            }

            return textView;
        }

        private void setTextViewTextColorFallbackColorWhenMissingStyleAttrs(AppCompatTextView textView)
        {
            var useDefaultColor = false;
            try
            {
                TextViewCompat.SetTextAppearance(textView, textHelperTextAppearance);
                if (MarshmallowApis.AreAvailable && textView.TextColors.DefaultColor == fallbackTextColorAfterMarshmallow)
                {
                    useDefaultColor = true;
                }
            }
            catch (Exception e)
            {
                useDefaultColor = true;
            }

            if (useDefaultColor)
            {
                TextViewCompat.SetTextAppearance(textView, Resource.Style.TextAppearance_AppCompat_Caption);
                Color defaultTextColor = new Color(ContextCompat.GetColor(Context, Resource.Color.defaultText));
                textView.SetTextColor(defaultTextColor);
            }
        }

        public ICharSequence HelperText
        {
            set
            {
                helperText = value;
                if (!helperTextEnabled)
                {
                    if (string.IsNullOrWhiteSpace(helperText.ToString())) return;
                    HelperTextEnabled = true;
                }

                if (helperTextTextView == null) return;

                if (!string.IsNullOrWhiteSpace(helperText.ToString()))
                {
                    helperTextTextView.TextFormatted = helperText;
                    helperTextTextView.Visibility = ViewStates.Visible;
                    helperTextTextView.Alpha = 0f;
                    ViewCompat.Animate(helperTextTextView)
                        .Alpha(1.0f).SetDuration(animationDuration)
                        .SetInterpolator(interpolator)
                        .SetListener(null)
                        .Start();
                }
                else if (helperTextTextView.Visibility == ViewStates.Visible)
                {
                    ViewCompat.Animate(helperTextTextView)
                        .Alpha(0.0f).SetDuration(animationDuration)
                        .SetInterpolator(interpolator)
                        .SetListener(this)
                        .Start();
                }

                SendAccessibilityEvent(EventTypes.WindowContentChanged);
            }
        }


        public void OnAnimationCancel(View view)
        {
        }

        public void OnAnimationStart(View view)
        {
        }

        public void OnAnimationEnd(View view)
        {
            if (helperTextTextView == null) return;
            helperTextTextView.TextFormatted = null;
            helperTextTextView.Visibility = ViewStates.Invisible;
        }
    }
}

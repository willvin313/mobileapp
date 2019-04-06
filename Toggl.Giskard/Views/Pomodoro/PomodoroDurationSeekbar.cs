using Android.Content;
using Android.Runtime;
using Android.Util;
using Android.Widget;
using System;
using System.Collections.Generic;
using System.Linq;
using Toggl.Multivac.Extensions;

namespace Toggl.Giskard.Views.Pomodoro
{
    [Register("toggl.giskard.views.PomodoroDurationSeekbar")]
    public class PomodoroDurationSeekbar : SeekBar
    {
        private List<int> durations;

        public int Duration
        {
            get
            {
                return durations[Progress];
            }
            set
            {
                var newProgress = durations.IndexOf(value);
                if (newProgress == Progress)
                    return;

                SetProgress(newProgress, true);
            }
        }

        #region Constructors

        protected PomodoroDurationSeekbar(IntPtr javaReference, JniHandleOwnership transfer) : base(javaReference, transfer)
        {
        }

        public PomodoroDurationSeekbar(Context context) : base(context)
        {
            init(context);
        }

        public PomodoroDurationSeekbar(Context context, IAttributeSet attrs) : base(context, attrs)
        {
            initializeAttributeSet(context, attrs);
            init(context);
        }

        public PomodoroDurationSeekbar(Context context, IAttributeSet attrs, int defStyleAttrs) : base(context, attrs, defStyleAttrs)
        {
            initializeAttributeSet(context, attrs, 0, defStyleAttrs);
            init(context);
        }

        public PomodoroDurationSeekbar(Context context, IAttributeSet attrs, int defStyleAttrs, int defStyleRes) : base(context, attrs, defStyleAttrs, defStyleRes)
        {
            initializeAttributeSet(context, attrs, defStyleAttrs, defStyleRes);
            init(context);
        }

        private void initializeAttributeSet(Context context, IAttributeSet attrs, int defStyleAttrs = 0, int defStyleRes = 0)
        {
            var customsAttrs =
                context.ObtainStyledAttributes(attrs, Resource.Styleable.PomodoroDurationSeekbar, defStyleAttrs, defStyleRes);

            try
            {
                var durationsText = customsAttrs.GetString(Resource.Styleable.PomodoroDurationSeekbar_allowedDurations);

                durations = durationsText.Split(',')
                    .Select(CommonFunctions.Trim)
                    .Select(durationText => int.Parse(durationText))
                    .ToList();
            }
            finally
            {
                customsAttrs.Recycle();
            }
        }

        #endregion

        private void init(Context context)
        {
            Min = 0;
            Max = durations.LastIndex();
        }
    }
}

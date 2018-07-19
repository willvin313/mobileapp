using System;
using Android.Widget;

namespace Toggl.Giskard.Extensions
{
    public static partial class ViewExtensions
    {
        public static Action<bool> BindChecked(this CompoundButton compoundButton)
            => isChecked => compoundButton.Checked = isChecked;
    }
}

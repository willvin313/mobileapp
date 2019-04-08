using System;
using Toggl.Multivac.Extensions;
using static Toggl.Foundation.Helper.Constants;

namespace Toggl.Foundation.UI.Extensions
{
    public static class StringExtensions
    {
        public static bool IsSameCaseInsensitiveTrimedTextAs(this string self, string tagText)
            => self.Trim().Equals(tagText.Trim(), StringComparison.CurrentCultureIgnoreCase);

        public static bool IsAllowedTagByteSize(this string self)
            => self.LengthInBytes() <= MaxTagNameLengthInBytes;
    }
}

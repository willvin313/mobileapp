using System;
using Toggl.Foundation.Reports;
using MvvmCross.Droid.Support.V7.RecyclerView.ItemTemplates;
using Toggl.Foundation.MvvmCross.ViewModels.Reports;

namespace Toggl.Giskard.TemplateSelectors
{
    public sealed class ReportsTemplateSelector : IMvxTemplateSelector
    {
        public const int WorkspaceName = 0;
        public const int Header = 1;
        public const int Item = 2;

        public int ItemTemplateId { get; set; }

        public int GetItemLayoutId(int fromViewType)
        {
            switch (fromViewType)
            {
                case WorkspaceName:
                    return Resource.Layout.ReportsFragmentWorkspaceName;
                case Header:
                    return Resource.Layout.ReportsFragmentHeader;
                case Item:
                    return Resource.Layout.ReportsFragmentItem;
            }

            throw new ArgumentOutOfRangeException(nameof(fromViewType));
        }


        public int GetItemViewType(object forItemObject)
        {
            if (forItemObject is string)
                return WorkspaceName;

            if (forItemObject is ReportsViewModel)
                return Header;

            if (forItemObject is ChartSegment)
                return Item;

            throw new ArgumentOutOfRangeException(nameof(forItemObject));
        }
    }
}

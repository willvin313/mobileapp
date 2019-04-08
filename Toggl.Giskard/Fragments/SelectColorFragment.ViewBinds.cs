using Android.Support.V7.Widget;
using Android.Views;
using Android.Widget;
using Toggl.Foundation.UI.ViewModels;
using Toggl.Giskard.Adapters;
using Toggl.Giskard.Views;

namespace Toggl.Giskard.Fragments
{
    public partial class SelectColorFragment
    {
        private HueSaturationPickerView hueSaturationPicker;
        private ValueSlider valueSlider;
        private Button saveButton;
        private Button closeButton;
        private RecyclerView recyclerView;
        private SimpleAdapter<SelectableColorViewModel> selectableColorsAdapter;

        private void initializeViews(View view) 
        {
            recyclerView = view.FindViewById<RecyclerView>(Resource.Id.SelectColorRecyclerView);
            saveButton = view.FindViewById<Button>(Resource.Id.SelectColorSave);
            closeButton = view.FindViewById<Button>(Resource.Id.SelectColorClose);
            hueSaturationPicker = view.FindViewById<HueSaturationPickerView>(Resource.Id.SelectColorHueSaturationPicker);
            valueSlider = view.FindViewById<ValueSlider>(Resource.Id.SelectColorValueSlider);
        }
    }
}

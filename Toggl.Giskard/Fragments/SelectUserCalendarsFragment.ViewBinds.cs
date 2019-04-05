﻿using System;
using Android.Support.V7.Widget;
using Android.Views;
using Android.Widget;

namespace Toggl.Giskard.Fragments
{
    public sealed partial class SelectUserCalendarsFragment
    {
        private Button cancelButton;
        private Button doneButton;
        private RecyclerView recyclerView;
        private TextView selectCalendarsTextView;
        private TextView selectCalendarsSubTextView;
        private View separator;

        protected override void InitializeViews(View view)
        {
            cancelButton = view.FindViewById<Button>(Resource.Id.CancelButton);
            doneButton = view.FindViewById<Button>(Resource.Id.DoneButton);
            recyclerView = view.FindViewById<RecyclerView>(Resource.Id.CalendarsRecyclerView);
            selectCalendarsTextView = view.FindViewById<TextView>(Resource.Id.SelectCalendarsTextView);
            selectCalendarsSubTextView = view.FindViewById<TextView>(Resource.Id.SelectCalendarsSubTextView);
            separator = view.FindViewById(Resource.Id.Separator);
        }
    }
}

using System;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Android.Graphics;
using Android.Support.V7.Widget;
using Android.Views;
using Toggl.Giskard.Extensions;

namespace Toggl.Giskard.ViewHelpers.Calendar
{
    public class CalendarEditItemTouchHelper : RecyclerView.ItemDecoration, RecyclerView.IOnItemTouchListener
    {
        private RecyclerView recyclerView;
        private RecyclerView.ViewHolder itemInEditMode;

        private float originalElevation;
        private float initialY;
        private float deltaY;
        private float tempY;

        private readonly ISubject<Unit> clearedItemInEditMode = new Subject<Unit>();

        public IObservable<Unit> ItemInEditModeClearedObservable
            => clearedItemInEditMode.AsObservable();

        public void AttachToRecyclerView(RecyclerView recyclerViewToAttach)
        {
            if (recyclerView == recyclerViewToAttach) return;

            if (recyclerViewToAttach == null)
            {
                throw new InvalidOperationException("You can't attach a null recycler view");
            }

            if (recyclerView != null)
            {
                destroyCallbacks();
            }

            recyclerView = recyclerViewToAttach;
            setupCallbacks();
        }

        private void setupCallbacks()
        {
            recyclerView.AddItemDecoration(this);
            recyclerView.AddOnItemTouchListener(this);
        }

        private void destroyCallbacks()
        {
            recyclerView.RemoveItemDecoration(this);
            recyclerView.RemoveOnItemTouchListener(this);
        }

        public bool OnInterceptTouchEvent(RecyclerView recycler, MotionEvent @event)
        {
            switch (@event.Action)
            {
                case MotionEventActions.Down:
                    initialY = @event.GetY();
                    if (itemInEditMode != null)
                    {
                        var childUnderTouch = recycler.FindChildViewUnder(@event.GetX(), @event.GetY());
                        if (childUnderTouch != itemInEditMode.ItemView)
                        {
                            UpdateViewInEditMode(null);
                        }
                    }

                    break;
            }
            return itemInEditMode != null;
        }

        public void OnTouchEvent(RecyclerView recycler, MotionEvent @event)
        {
            var currentItemInEditMode = itemInEditMode;
            if (currentItemInEditMode == null) return;

            switch (@event.Action)
            {
                case MotionEventActions.Move:
                    updateDeltaY(@event);
                    recycler.Invalidate();
                    break;

                case MotionEventActions.Up:
                    //todo: revert if didn't move enough
                    break;
            }
        }

        private void updateDeltaY(MotionEvent motionEvent)
        {
            deltaY = motionEvent.GetY() - initialY;
        }

        public void OnRequestDisallowInterceptTouchEvent(bool disallowIntercept)
        {
            if (!disallowIntercept) return;

            UpdateViewInEditMode(null);
        }

        public void UpdateViewInEditMode(RecyclerView.ViewHolder viewHolder)
        {
            if (itemInEditMode != null)
            {
                clearItemInEditMode(itemInEditMode);
            }

            itemInEditMode = viewHolder;
            if (itemInEditMode != null)
            {
                originalElevation = itemInEditMode.ItemView.Elevation;
                itemInEditMode.ItemView.Elevation = originalElevation + 4.DpToPixels(itemInEditMode.ItemView.Context);
                initialY = itemInEditMode.ItemView.Top;
            }
        }

        private void clearItemInEditMode(RecyclerView.ViewHolder viewHolder)
        {
            viewHolder.ItemView.Elevation = originalElevation;
            viewHolder.ItemView.TranslationY = 0f;
            clearedItemInEditMode.OnNext(Unit.Default);
        }

        public override void OnDraw(Canvas canvas, RecyclerView parent, RecyclerView.State state)
        {
            var currentItemInEditMode = itemInEditMode;
            if (currentItemInEditMode != null)
            {
                tempY = calculateCurrentItemInEditModeY(currentItemInEditMode);
                drawWithYOffset(canvas, currentItemInEditMode, tempY);
            }
        }

        private void drawWithYOffset(Canvas canvas, RecyclerView.ViewHolder currentItemInEditMode, float yTranslation)
        {
            var count = canvas.Save();
            currentItemInEditMode.ItemView.TranslationY = yTranslation;
            canvas.RestoreToCount(count);
        }

        private float calculateCurrentItemInEditModeY(RecyclerView.ViewHolder currentItemInEditMode)
            => initialY + deltaY - currentItemInEditMode.ItemView.Top;
    }
}

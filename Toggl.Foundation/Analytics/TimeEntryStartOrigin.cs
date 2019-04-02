using System;

namespace Toggl.Foundation.Analytics
{
    public enum TimeEntryStartOrigin
    {
        /// <summary>
        /// Starting a time entry by pressing the Play button in the Timer mode
        /// </summary>
        Timer,

        /// <summary>
        /// Starting a time entry by pressing the Play button in the Manual mode
        /// </summary>
        Manual,

        /// <summary>
        /// Starting a time entry by swiping a single time entry in the main log
        /// </summary>
        SingleTimeEntrySwipe,

        /// <summary>
        /// Starting a time entry by pressing the "right arrow" button
        /// on a single time entry in the main log
        /// </summary>
        SingleTimeEntryContinueButton,

        /// <summary>
        /// Starting a time entry by swiping a time entries group in the main log
        /// </summary>
        TimeEntriesGroupSwipe,

        /// <summary>
        /// Starting a time entry by pressing the "right arrow" button
        /// on the time entries group in the main log
        /// </summary>
        TimeEntriesGroupContinueButton,

        /// <summary>
        /// Starting a time entry by pressing the time entry suggestion
        /// </summary>
        Suggestion,

        /// <summary>
        /// Starting a time entry from App shortcut
        /// </summary>
        ContinueMostRecent,

        /// <summary>
        /// Starting a time entry by long-pressing an event in the calendar
        /// </summary>
        CalendarEvent,

        /// <summary>
        /// Starting a time entry by tapping and dragging a time slot in a calendar
        /// </summary>
        CalendarTapAndDrag,

        /// <summary>
        /// Starting a time entry from a calendar notification
        /// </summary>
        CalendarNotification,

        /// <summary>
        /// Starting a time entry via Siri
        /// </summary>
        Siri
    }
}

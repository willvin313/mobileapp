using System;
using System.Threading.Tasks;

namespace Toggl.Foundation.MvvmCross.Services
{

    [Obsolete("Use SendFeedbackInteractor")]
    public interface IFeedbackService
    {
        Task SubmitFeedback();
    }
}

using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using Toggl.Foundation.Services;
using Toggl.Multivac;

namespace Toggl.Foundation.MvvmCross.Services
{
    public sealed class FeedbackService : IFeedbackService
    {
        private const string feedbackRecipient = "support@toggl.com";
        private const string subject = "Toggl Mobile App Feedback";

        private readonly IMailService mailService;
        private readonly IPlatformInfo platformInfo;
        private readonly IDialogService dialogService;

        public FeedbackService(
            IMailService mailService,
            IDialogService dialogService,
            IPlatformInfo platformInfo)
        {
            Ensure.Argument.IsNotNull(mailService, nameof(mailService));
            Ensure.Argument.IsNotNull(platformInfo, nameof(platformInfo));
            Ensure.Argument.IsNotNull(dialogService, nameof(dialogService));

            this.mailService = mailService;
            this.platformInfo = platformInfo;
            this.dialogService = dialogService;
        }

        public async Task SubmitFeedback()
        {
            var phone = platformInfo.PhoneModel;
            var os = platformInfo.OperatingSystem;
            var version = platformInfo.UserAgent.ToString();

            var messageBuilder = new StringBuilder();
            messageBuilder.Append("\n\n"); // 2 leading newlines, so user user can type something above this info
            messageBuilder.Append($"Version: {version}\n");
            if (phone != null)
            {
                messageBuilder.Append($"Phone: {phone}\n");
            }
            messageBuilder.Append($"OS: {os}");

            var mailResult = await mailService.Send(
                feedbackRecipient,
                subject,
                messageBuilder.ToString()
            );

            if (mailResult.Success || string.IsNullOrEmpty(mailResult.ErrorTitle))
                return;

            await dialogService.Alert(
                mailResult.ErrorTitle,
                mailResult.ErrorMessage,
                Resources.Ok
            );
        }
    }
}

using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using Toggl.Foundation.Services;
using Toggl.Multivac;
using Toggl.Ultrawave.Network;

namespace Toggl.Foundation.MvvmCross.Services
{
    public sealed class FeedbackService : IFeedbackService
    {
        private const string feedbackRecipient = "support@toggl.com";
        private const string subject = "Toggl Mobile App Feedback";

        private readonly UserAgent userAgent;
        private readonly IMailService mailService;
        private readonly IDialogService dialogService;
        private readonly IPlatformInfo platformInfo;

        public FeedbackService(
            UserAgent userAgent,
            IMailService mailService,
            IDialogService dialogService,
            IPlatformInfo platformInfo)
        {
            Ensure.Argument.IsNotNull(userAgent, nameof(userAgent));
            Ensure.Argument.IsNotNull(mailService, nameof(mailService));
            Ensure.Argument.IsNotNull(dialogService, nameof(dialogService));
            Ensure.Argument.IsNotNull(platformInfo, nameof(platformInfo));

            this.userAgent = userAgent;
            this.mailService = mailService;
            this.dialogService = dialogService;
            this.platformInfo = platformInfo;
        }

        public async Task SubmitFeedback()
        {
            var version = userAgent.ToString();
            var phone = platformInfo.PhoneModel;
            var os = platformInfo.OperatingSystem;

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

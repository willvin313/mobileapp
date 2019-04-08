﻿using MvvmCross.Navigation;
using MvvmCross.ViewModels;
using System.Threading.Tasks;
using Toggl.Foundation.UI.Parameters;
using Toggl.Foundation.Services;
using Toggl.Multivac;
using Toggl.Multivac.Extensions;
using System.Threading.Tasks;

namespace Toggl.Foundation.UI.ViewModels
{
    [Preserve(AllMembers = true)]
    public sealed class BrowserViewModel : MvxViewModel<BrowserParameters>
    {
        public string Url { get; private set; }

        public string Title { get; private set; }

        public UIAction Close { get; }

        public BrowserViewModel(IMvxNavigationService navigationService, IRxActionFactory rxActionFactory)
        {
            Ensure.Argument.IsNotNull(navigationService, nameof(navigationService));
            Ensure.Argument.IsNotNull(rxActionFactory, nameof(rxActionFactory));

            Close = rxActionFactory.FromAsync(close);

            Task close() => navigationService.Close(this);
        }

        public override void Prepare(BrowserParameters parameter)
        {
            Url = parameter.Url;
            Title = parameter.Title;
        }
    }
}

using System;
using Toggl.Foundation.DataSources.Interfaces;
using Toggl.Foundation.Models.Interfaces;

namespace Toggl.Foundation.Interactors
{
    public sealed class GetCurrentUserInteractor : IInteractor<IObservable<IThreadSafeUser>>
    {
        private readonly ISingletonDataSource<IThreadSafeUser> dataSource;

        public GetCurrentUserInteractor(ISingletonDataSource<IThreadSafeUser> dataSource)
        {
            this.dataSource = dataSource;
        }

        public IObservable<IThreadSafeUser> Execute()
        {
            return dataSource.Get();
        }
    }
}

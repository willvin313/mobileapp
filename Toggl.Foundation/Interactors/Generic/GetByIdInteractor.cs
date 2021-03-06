﻿using System;
using Toggl.Foundation.Analytics;
using Toggl.Foundation.DataSources.Interfaces;
using Toggl.Foundation.Models.Interfaces;
using Toggl.Multivac;
using Toggl.PrimeRadiant.Models;

namespace Toggl.Foundation.Interactors.Generic
{
    public sealed class GetByIdInteractor<TThreadsafe, TDatabase>
        : TrackableInteractor, IInteractor<IObservable<TThreadsafe>>
        where TDatabase : IDatabaseModel
        where TThreadsafe : TDatabase, IThreadSafeModel
    {
        private readonly IDataSource<TThreadsafe, TDatabase> dataSource;
        private readonly long id;

        public GetByIdInteractor(IDataSource<TThreadsafe, TDatabase> dataSource, IAnalyticsService analyticsService, long id)
            : base(analyticsService)
        {
            Ensure.Argument.IsNotNull(dataSource, nameof(dataSource));

            this.dataSource = dataSource;
            this.id = id;
        }

        public IObservable<TThreadsafe> Execute()
            => dataSource.GetById(id);
    }
}

using System;
using Toggl.Multivac;
using Toggl.Multivac.Models;
using Toggl.Ultrawave.ApiClients.Interfaces;
using Toggl.Ultrawave.Models;
using Toggl.Ultrawave.Network;
using Toggl.Ultrawave.Serialization;

namespace Toggl.Ultrawave.ApiClients
{
    internal sealed class LocationApi : BaseApi, ILocationApi
    {
        private readonly LocationEndpoints endPoints;

        public LocationApi(Endpoints endPoints, IApiClient apiClient, IJsonSerializer serializer, Credentials credentials)
            : base(apiClient, serializer, credentials, endPoints.LoggedIn)
        {
            Ensure.Argument.IsNotNull(endPoints, nameof(endPoints));

            this.endPoints = endPoints.Location;
        }

        public IObservable<ILocation> Get()
            => SendRequest<Location>(endPoints.Get, AuthHeader);
    }
}

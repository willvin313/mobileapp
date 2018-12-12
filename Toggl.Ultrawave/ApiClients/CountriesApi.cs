using System;
using System.Collections.Generic;
using Toggl.Multivac.Models;
using Toggl.Ultrawave.Models;
using Toggl.Ultrawave.Network;
using Toggl.Ultrawave.Serialization;

namespace Toggl.Ultrawave.ApiClients
{
    internal sealed class CountriesApi : BaseApi, ICountriesApi
    {
        private readonly CountryEndpoints endPoints;

        public CountriesApi(Endpoints endPoints, IApiClient apiClient, IJsonSerializer serializer, Credentials credentials)
            : base(apiClient, serializer, credentials, endPoints.LoggedIn)
        {
            this.endPoints = endPoints.Countries;
        }

        public IObservable<List<ICountry>> GetAll()
            => SendRequest<Country, ICountry>(endPoints.Get, AuthHeader);
    }
}

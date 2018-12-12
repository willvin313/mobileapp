using System;
using System.Collections.Generic;
using Toggl.Multivac;
using Toggl.Multivac.Models;
using Toggl.Ultrawave.Models;
using Toggl.Ultrawave.Network;
using Toggl.Ultrawave.Serialization;

namespace Toggl.Ultrawave.ApiClients
{
    internal sealed class ProjectsApi : BaseApi, IProjectsApi
    {
        private readonly ProjectEndpoints endPoints;
        private readonly Network.Reports.ProjectEndpoints reportsEndPoints;

        public ProjectsApi(Endpoints endPoints, IApiClient apiClient, IJsonSerializer serializer, Credentials credentials)
            : base(apiClient, serializer, credentials, endPoints.LoggedIn)
        {
            this.endPoints = endPoints.Projects;
            this.reportsEndPoints = endPoints.ReportsEndpoints.Projects;
        }

        public IObservable<List<IProject>> GetAll()
            => SendRequest<Project, IProject>(endPoints.Get, AuthHeader);

        public IObservable<List<IProject>> GetAllSince(DateTimeOffset threshold)
            => SendRequest<Project, IProject>(endPoints.GetSince(threshold), AuthHeader);

        public IObservable<IProject> Create(IProject project)
        {
            var endPoint = endPoints.Post(project.WorkspaceId);
            var projectCopy = project as Project ?? new Project(project);
            var observable = SendRequest(endPoint, AuthHeader, projectCopy, SerializationReason.Post);
            return observable;
        }

        public IObservable<List<IProject>> Search(long workspaceId, long[] projectIds)
        {
            Ensure.Argument.IsNotNull(projectIds, nameof(projectIds));

            var json = $"{{\"ids\":[{String.Join(",", projectIds)}]}}";
            return SendRequest<Project, IProject>(reportsEndPoints.Search(workspaceId), AuthHeader, json);
        }
    }
}

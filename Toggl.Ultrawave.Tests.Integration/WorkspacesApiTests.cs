using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Reactive.Linq;
using FluentAssertions;
using Toggl.Multivac;
using Toggl.Multivac.Models;
using Toggl.Ultrawave.Exceptions;
using Toggl.Ultrawave.Extensions;
using Toggl.Ultrawave.Helpers;
using Toggl.Ultrawave.Models;
using Toggl.Ultrawave.Network;
using Toggl.Ultrawave.Tests.Integration.BaseTests;
using Xunit;
using Task = System.Threading.Tasks.Task;

namespace Toggl.Ultrawave.Tests.Integration
{
    public sealed class WorkspacesApiTests
    {
        public sealed class TheGetMethod : AuthenticatedEndpointBaseTests<List<IWorkspace>>
        {
            protected override IObservable<List<IWorkspace>> CallEndpointWith(ITogglApi togglApi)
                => togglApi.Workspaces.GetAll();

            [Fact, LogTestInfo]
            public async Task ReturnsOneWorkspaceForANewUser()
            {
                var (togglClient, user) = await SetupTestUser();

                var workspaces = await CallEndpointWith(togglClient);

                workspaces.Should().HaveCount(1);
                workspaces.Should().Contain(ws => ws.Id == user.DefaultWorkspaceId);
            }

            [Fact, LogTestInfo]
            public async Task ReturnsAllWorkspaces()
            {
                var (togglClient, user) = await SetupTestUser();
                var secondWorkspace =
                    await togglClient.Workspaces.Create(new Workspace { Name = Guid.NewGuid().ToString() });

                var workspaces = await CallEndpointWith(togglClient);

                workspaces.Should().HaveCount(2);
                workspaces.Should().Contain(ws => ws.Id == user.DefaultWorkspaceId);
                workspaces.Should().Contain(ws => ws.Id == secondWorkspace.Id);
            }

            [Fact, LogTestInfo]
            public async Task ReturnsEmptyListWhenTheDefaultWorkspaceIsDeleted()
            {
                var (togglClient, user) = await SetupTestUser();
                await deleteDefaultWorkspaceUsingV8Api(user);

                var workspaces = await CallEndpointWith(togglClient);

                workspaces.Should().BeEmpty();
            }

            private async Task deleteDefaultWorkspaceUsingV8Api(IUser user)
            {
                var credentials = Credentials.WithApiToken(user.ApiToken);
                var httpClient = new HttpClient();
                var urlPrefixForV8 = BaseUrls.ForApi(ApiEnvironment.Staging).AbsoluteUri.Replace("/v9/", "/v8");
                var requestMessage = new HttpRequestMessage(HttpMethod.Delete, $"{urlPrefixForV8}/workspaces/{user.DefaultWorkspaceId}/leave");
                requestMessage.Headers.AddRange(new[] { credentials.Header });
                await httpClient.SendAsync(requestMessage).ConfigureAwait(false);
            }
        }

        public sealed class TheGetByIdMethod : AuthenticatedGetEndpointBaseTests<IWorkspace>
        {
            protected override IObservable<IWorkspace> CallEndpointWith(ITogglApi togglApi)
                => Observable.Defer(async () =>
                {
                    var user = await togglApi.User.Get();
                    return CallEndpointWith(togglApi, user.DefaultWorkspaceId.Value);
                });

            private Func<Task> CallingEndpointWith(ITogglApi togglApi, long id)
                => async () => await CallEndpointWith(togglApi, id);

            private IObservable<IWorkspace> CallEndpointWith(ITogglApi togglApi, long id)
                => togglApi.Workspaces.GetById(id);

            [Fact, LogTestInfo]
            public async Task ReturnsDefaultWorkspace()
            {
                var (togglClient, user) = await SetupTestUser();

                var workspace = await CallEndpointWith(togglClient, user.DefaultWorkspaceId.Value);

                workspace.Id.Should().Be(user.DefaultWorkspaceId);
            }

            [Fact, LogTestInfo]
            public async Task ReturnsCreatedWorkspace()
            {
                var (togglClient, user) = await SetupTestUser();
                var secondWorkspace = await togglClient.Workspaces.Create(new Workspace { Name = Guid.NewGuid().ToString() });

                var workspace = await CallEndpointWith(togglClient, secondWorkspace.Id);

                workspace.Id.Should().Be(secondWorkspace.Id);
                workspace.Name.Should().Be(secondWorkspace.Name);
            }

            [Fact, LogTestInfo]
            public async Task FailsForWrongWorkspaceId()
            {
                var (togglClient, user) = await SetupTestUser();

                CallingEndpointWith(togglClient, user.DefaultWorkspaceId.Value - 1).Should().Throw<ForbiddenException>();
            }
        }

        public sealed class TheCreateMethod : AuthenticatedPostEndpointBaseTests<IWorkspace>
        {
            protected override IObservable<IWorkspace> CallEndpointWith(ITogglApi togglApi)
                => togglApi.Workspaces.Create(new Workspace { Name = Guid.NewGuid().ToString() });

            [Fact, LogTestInfo]
            public async Task CreatesWorkspaceWithGivenName()
            {
                var (api, user) = await SetupTestUser();
                var name = Guid.NewGuid().ToString();

                var workspace = await api.Workspaces.Create(new Workspace { Name = name });

                workspace.Name.Should().Be(name);
            }

            [Fact, LogTestInfo]
            public async Task CreatesWorkspaceWithTheFreePlan()
            {
                var (api, user) = await SetupTestUser();
                var name = Guid.NewGuid().ToString();

                var workspace = await api.Workspaces.Create(new Workspace { Name = name });
                var features = await api.WorkspaceFeatures.GetAll()
                    .SelectMany(all => all.Where(workspaceFeatures => workspaceFeatures.WorkspaceId == workspace.Id))
                    .SingleAsync();

                features.Features.Should().Contain(feature =>
                    feature.FeatureId == WorkspaceFeatureId.Pro && feature.Enabled == false);
            }
        }
    }
}

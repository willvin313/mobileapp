using System;
using Toggl.Foundation.DTOs;
using Toggl.Foundation.Models.Interfaces;

namespace Toggl.Foundation.Interactors
{
    public sealed partial class InteractorFactory : IInteractorFactory
    {
        public IInteractor<IObservable<byte[]>> GetUserAvatar(string url)
            => new GetUserAvatarInteractor(url);

        public IInteractor<IObservable<IThreadSafeUser>> UpdateUser(EditUserDTO dto)
            => new UpdateUserInteractor(timeService, dataSource.User, dto);

        public IInteractor<IObservable<IThreadSafeUser>> UpdateDefaultWorkspace(long selectedWorkspaceId)
            => new UpdateWorkspaceInteractor(dataSource.User, selectedWorkspaceId);

        public IInteractor<IObservable<IThreadSafeUser>> GetCurrentUser()
            => new GetCurrentUserInteractor(dataSource.User);
    }
}

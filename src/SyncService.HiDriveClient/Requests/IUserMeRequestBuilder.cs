using System.Collections.Generic;
using SyncService.HiDriveClient.Models;

namespace SyncService.HiDriveClient.Requests
{
    public interface IUserMeRequestBuilder
    {
        IRequest<User> Get(IEnumerable<string> fields = null);
    }

}
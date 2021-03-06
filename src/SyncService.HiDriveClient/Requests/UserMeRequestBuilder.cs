using System.Collections.Generic;
using SyncService.HiDriveClient.Models;

namespace SyncService.HiDriveClient.Requests
{
    internal class UserMeRequestBuilder : BaseRequestBuilder, IUserMeRequestBuilder
    {
        public UserMeRequestBuilder(string requestUrl, IBaseClient client)
            : base(requestUrl, client)
        {
        }

        public IRequest<User> Get(IEnumerable<string> fields = null)
        {
            var request = new Request<User>(RequestUrl, Client);

            if (fields != null)
                request.QueryOptions.Add(new KeyValuePair<string, string>("fields", string.Join(",", fields)));

            return request;
        }
    }
}
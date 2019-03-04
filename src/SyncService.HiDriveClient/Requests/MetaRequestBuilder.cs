using System;
using System.Collections.Generic;
using System.Globalization;
using SyncService.HiDriveClient.Models;

namespace SyncService.HiDriveClient.Requests
{
    public class MetaRequestBuilder : BaseRequestBuilder, IMetaRequestBuilder
    {
        public MetaRequestBuilder(IBaseClient client) : base("meta", client)
        {
        }

        public IRequest<Meta> Get(string path = null, string pid = null, IEnumerable<string> fields = null)
        {
            var request = new Request<Meta>(RequestUrl, Client);

            if (path != null) request.QueryOptions.Add(new KeyValuePair<string, string>("path", Uri.EscapeDataString(path)));
            if (pid != null) request.QueryOptions.Add(new KeyValuePair<string, string>("pid", pid));

            if (fields != null)
                request.QueryOptions.Add(new KeyValuePair<string, string>("fields", string.Join(",", fields)));

            return request;
        }

        public IRequest SetModificationDate(string path = null, string pid = null, DateTime modificationTime = default(DateTime))
        {
            var request = new Request(RequestUrl, Client)
            {
                Method = "PATCH"
            };

            if (path != null) request.QueryOptions.Add(new KeyValuePair<string, string>("path", Uri.EscapeDataString(path)));
            if (pid != null) request.QueryOptions.Add(new KeyValuePair<string, string>("pid", pid));
            if (modificationTime != default(DateTime))
            {
                request.QueryOptions.Add(new KeyValuePair<string, string>("mtime", new DateTimeOffset(modificationTime).ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture)));
            }

            return request;
        }
    }
}
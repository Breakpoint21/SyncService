using System;
using System.Collections.Generic;
using SyncService.HiDriveClient.Models;

namespace SyncService.HiDriveClient.Requests
{
    public interface IMetaRequestBuilder
    {
        IRequest<Meta> Get(string path = null, string pid = null, IEnumerable<string> fields = null);

        IRequest SetModificationDate(string path = null, string pid = null,
            DateTime modificationTime = default(DateTime));
    }
}
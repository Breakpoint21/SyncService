using System.Collections.Generic;
using System.Net.Http;

namespace SyncService.HiDriveClient.Requests
{
    public interface IBaseRequest
    {
        string ContentType { get; set; }

        //IList<HeaderOption> Headers { get; }

        IBaseClient Client { get; }

        string Method { get; }

        string RequestUrl { get; }

        IList<KeyValuePair<string, string>> QueryOptions { get; }

        HttpRequestMessage GetHttpRequestMessage();
    }
}
namespace SyncService.HiDriveClient.Requests
{
    public class BaseRequestBuilder
    {
        public IBaseClient Client { get; private set; }

        public string RequestUrl { get; internal set; }

        public BaseRequestBuilder(string requestUrl, IBaseClient client)
        {
            Client = client;
            RequestUrl = requestUrl;
        }

        public string AppendSegmentToRequestUrl(string urlSegment)
        {
            return string.Format("{0}/{1}", RequestUrl, urlSegment);
        }

    }
}
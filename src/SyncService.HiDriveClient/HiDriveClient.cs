using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using SyncService.HiDriveClient.Authentication;
using SyncService.HiDriveClient.Requests;

namespace SyncService.HiDriveClient
{
    public class HiDriveClient : IHiDriveClient, IBaseClient
    {
        public const string ApiUrl = "https://api.hidrive.strato.com/2.1/";

        public IHiDriveAuthenticator Authenticator { get; }
        private readonly HttpClient _httpClient;


        public HiDriveClient(IHiDriveAuthenticator authenticator)
        {
            Authenticator = authenticator ?? throw new ArgumentNullException(nameof(authenticator));
            _httpClient = new HttpClient {BaseAddress = new Uri(ApiUrl), Timeout = Timeout.InfiniteTimeSpan };
            _httpClient.DefaultRequestHeaders.AcceptCharset.Add(new StringWithQualityHeaderValue("utf-8"));
            _httpClient.DefaultRequestHeaders.Accept.Add(MediaTypeWithQualityHeaderValue.Parse("application/json"));
            Directory = new DirectoryRequestBuilder(this);
            File = new FileRequestBuilder(this);
            Meta = new MetaRequestBuilder(this);
            User = new UserRequestBuilder(this);
        }

        public IDirectoryRequestBuilder Directory { get; }

        public IFileRequestBuilder File { get; }

        public IMetaRequestBuilder Meta { get; }

        public IUserRequestBuilder User { get; }

        public async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request)
        {
            return await SendAsync(request, HttpCompletionOption.ResponseContentRead, CancellationToken.None);
        }

        public async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, HttpCompletionOption completionOption, CancellationToken cancellationToken)
        {
            return await _httpClient.SendAsync(request, completionOption, cancellationToken);
        }

        public Task AuthenticateRequestAsync(HttpRequestMessage request)
        {
            return Authenticator.AuthenticateRequestAsync(request);
        }
    }
}
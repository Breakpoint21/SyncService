using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace SyncService.HiDriveClient.Requests
{
    public class ReceiveStreamRequest : BaseRequest, IReceiveStreamRequest
    {
        public ReceiveStreamRequest(string requestUrl, IBaseClient client)
            : base(requestUrl, client)
        {
        }

        public Task<Stream> ExecuteAsync()
        {
            return ExecuteAsync(CancellationToken.None);
        }

        public async Task<Stream> ExecuteAsync(CancellationToken cancellationToken)
        {
            var retrievedStream = await SendStreamAsync(null, cancellationToken).ConfigureAwait(false);
            return retrievedStream;
        }
    }
}
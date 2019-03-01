using System.Threading;
using System.Threading.Tasks;

namespace Kyrodan.HiDrive.Requests
{
    public class Request<T> : BaseRequest, IRequest<T>
    {
        public Request(string requestUrl, IBaseClient client) 
            : base(requestUrl, client)
        {
        }

        public Task<T> ExecuteAsync()
        {
            return ExecuteAsync(CancellationToken.None);
        }

        public async Task<T> ExecuteAsync(CancellationToken cancellationToken)
        {
            var retrievedEntity = await SendAsync<T>(null, cancellationToken).ConfigureAwait(false);
            //this.InitializeCollectionProperties(retrievedEntity);
            return retrievedEntity;
        }

    }

    public class Request : BaseRequest, IRequest
    {
        public Request(string requestUrl, IBaseClient client)
            : base(requestUrl, client)
        {
        }

        public Task ExecuteAsync()
        {
            return ExecuteAsync(CancellationToken.None);
        }

        public async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            await SendAsync(null, cancellationToken).ConfigureAwait(false);
            //this.InitializeCollectionProperties(retrievedEntity);
        }

    }

}
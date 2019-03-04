using SyncService.HiDriveClient.Authentication;
using SyncService.HiDriveClient.Requests;

namespace SyncService.HiDriveClient
{
    public interface IHiDriveClient
    {
        IDirectoryRequestBuilder Directory { get; }
        IFileRequestBuilder File { get; }
        IUserRequestBuilder User { get; }
        IMetaRequestBuilder Meta { get; }
        IHiDriveAuthenticator Authenticator { get; }
    }
}
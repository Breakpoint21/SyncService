using Kyrodan.HiDrive.Authentication;
using Kyrodan.HiDrive.Requests;

namespace Kyrodan.HiDrive
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
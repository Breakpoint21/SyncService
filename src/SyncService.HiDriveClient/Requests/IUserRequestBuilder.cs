namespace SyncService.HiDriveClient.Requests
{
    public interface IUserRequestBuilder
    {
        IUserMeRequestBuilder Me { get; }
    }
}
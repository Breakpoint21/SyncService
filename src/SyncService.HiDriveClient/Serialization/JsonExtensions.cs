namespace SyncService.HiDriveClient.Serialization
{
    public static class JsonExtensions
    {
        public static string ToJsonBool(this bool value)
        {
            return value ? "true" : "false";
        }
    }
}

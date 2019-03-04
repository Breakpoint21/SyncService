using System.Runtime.Serialization;

namespace SyncService.HiDriveClient.Models
{
    [DataContract]
    public class DirectoryChildItem : DirectoryBaseItem
    {
        [DataMember(Name = "mime_type")]
        public string MimeType { get; set; }
    }
}
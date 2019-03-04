using System.Collections.Generic;
using System.Runtime.Serialization;

namespace SyncService.HiDriveClient.Models
{
    [DataContract]
    public class DirectoryItem : DirectoryBaseItem
    {
        [DataMember(Name = "members")]
        public IEnumerable<DirectoryChildItem> Members { get; set; }
    }
}
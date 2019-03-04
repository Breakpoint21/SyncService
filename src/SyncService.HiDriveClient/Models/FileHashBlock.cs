using System.Collections.Generic;
using System.Runtime.Serialization;

namespace SyncService.HiDriveClient.Models
{
    [DataContract]
    public class FileHashBlock
    {
        [DataMember(Name = "level")]
        public int Level { get; set; }

        [DataMember(Name = "block")]
        public int Block { get; set; }

        [DataMember(Name = "hash")]
        public string Hash { get; set; }
    }

    [DataContract]
    public class FileHash
    {
        [DataMember(Name = "level")]
        public int Level { get; set; }

        [DataMember(Name = "chash")]
        public string ContentHash { get; set; }

        [DataMember(Name = "list")]
        public IEnumerable<IEnumerable<FileHashBlock>> Blocks { get; set; }
    }
}
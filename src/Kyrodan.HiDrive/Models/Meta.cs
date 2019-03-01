using System;
using System.Runtime.Serialization;
using Kyrodan.HiDrive.Serialization;
using Newtonsoft.Json;

namespace Kyrodan.HiDrive.Models
{
    [DataContract]
    public class Meta
    {
        public static class Fields
        {
            public const string Size = "size";
            public const string CreationTime = "ctime";
            public const string ModificationTime = "mtime";
            public const string IsReadable = "readable";
            public const string IsWritable = "writable";
            public const string Type = "type";
            public const string HasDirectories = "has_dirs";
            public const string ContentHash = "chash";
            public const string MetadataHash = "mhash";
            public const string MetadataOnlyHash = "mohash";
            public const string NameHash = "nhash";
        }

        [DataMember(Name = "type")]
        public string Type { get; set; }

        [DataMember(Name = "ctime")]
        [JsonConverter(typeof(TimestampConverter))]
        public DateTime? CreatedDateTime { get; set; }

        [DataMember(Name = "mtime")]
        [JsonConverter(typeof(TimestampConverter))]
        public DateTime? ModifiedDateTime { get; set; }

        [DataMember(Name = "size")]
        public int? Size { get; set; }

        [DataMember(Name = "readable")]
        public bool? IsReadable { get; set; }

        [DataMember(Name = "writable")]
        public bool? IsWritable { get; set; }

        [DataMember(Name = "has_dirs")]
        public bool? HasDirectories { get; set; }

        [DataMember(Name = "chash")]
        public string ContentHash { get; set; }

        [DataMember(Name = "mhash")]
        public string MetadataHash { get; set; }

        [DataMember(Name = "mohash")]
        public string MetadataOnlyHash { get; set; }

        [DataMember(Name = "nhash")]
        public string NameHash { get; set; }
    }
}
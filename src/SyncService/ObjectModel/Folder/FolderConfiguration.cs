using System;

namespace SyncService.ObjectModel.Folder
{
    public class FolderConfiguration
    {
        public Guid Id { get; set; }    
        public string AccountId { get; set; }
        public string Label { get; set; }
        public string SourcePath { get; set; }
        public string DestinationPath { get; set; }
        public string LogPath { get; set; }
        public string Schedule { get; set; }
        public LogLevel LogLevel { get; set; }
        public FolderNotificationConfiguration NotificationConfiguration { get; set; }
    }
}
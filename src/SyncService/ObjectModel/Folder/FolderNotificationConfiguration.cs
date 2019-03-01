using System;

namespace SyncService.ObjectModel.Folder
{
    public class FolderNotificationConfiguration
    {
        public bool SendEmail { get; set; }
        public bool SendEmailOnlyOnError { get; set; }
        public Guid EmailConfigurationId { get; set; }
    }

    public class EmailConfiguration
    {
        public Guid Id { get; set; }
        public string Label { get; set; }
        public string Server { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public string EmailTo { get; set; }
    }
}
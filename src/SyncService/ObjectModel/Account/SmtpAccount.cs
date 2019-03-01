using System;

namespace SyncService.ObjectModel.Account
{
    public class SmtpAccount
    {
        public Guid Id { get; set; }
        public string Label { get; set; }
        public string Server { get; set; }
        public int Port { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public string PasswordEncrypted { get; set; }
        public string EmailTo { get; set; }
    }
}
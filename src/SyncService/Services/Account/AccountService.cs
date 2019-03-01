using System;
using System.IO;
using Microsoft.AspNetCore.DataProtection;
using Newtonsoft.Json;
using SyncService.ObjectModel.Account;

namespace SyncService.Services.Account
{
    public class AccountService
    {
        public const string FileName = "accounts.json";
        private readonly string _filePath;
        private readonly IDataProtector _dataProtector;
        public Accounts Accounts { get; private set; }

        public AccountService(IDataProtectionProvider dataProtectionProvider)
        {
            _dataProtector = dataProtectionProvider.CreateProtector("SmtpProtector");
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            _filePath = Path.Combine(appDataPath, "SyncService", FileName);
            Load();
        }

        private void Load()
        {
            Accounts = null;
            if (File.Exists(_filePath))
            {
                using (var file = File.OpenText(_filePath))
                {
                    var content = file.ReadToEnd();
                    Accounts = JsonConvert.DeserializeObject<Accounts>(content);
                }
                foreach (var smtpAccount in Accounts.SmtpAccounts)
                {
                    if (smtpAccount.PasswordEncrypted != null)
                    {
                        smtpAccount.Password = _dataProtector.Unprotect(smtpAccount.PasswordEncrypted);
                    }
                }
            }
            else
            {
                Accounts = new Accounts();
            }
        }

        public void Save()
        {
            if (File.Exists(_filePath))
            {
                File.Delete(_filePath);
            }

            Directory.CreateDirectory(Path.GetDirectoryName(_filePath));
            foreach (var smtpAccount in Accounts.SmtpAccounts)
            {
                if (smtpAccount.Password != null)
                {
                    smtpAccount.PasswordEncrypted = _dataProtector.Protect(smtpAccount.Password);
                    smtpAccount.Password = null;
                }
            }

            using (var file = File.CreateText(_filePath))
            {
                file.WriteLine(JsonConvert.SerializeObject(Accounts, Formatting.Indented));
            }

            foreach (var smtpAccount in Accounts.SmtpAccounts)
            {
                smtpAccount.Password = _dataProtector.Unprotect(smtpAccount.PasswordEncrypted);
            }
        }

        public void AddHiDriveAccount(HiDriveAccount hiDriveAccount)
        {
            Accounts.HiDriveAccount = hiDriveAccount;
            Save();
        }

        public void RevokeHiDriveAccount()
        {
            Accounts.HiDriveAccount = null;
            Save();
        }

        public void AddSmtpAccount(SmtpAccount smtpAccount)
        {
            smtpAccount.PasswordEncrypted = _dataProtector.Protect(smtpAccount.Password);
            
            Accounts.SmtpAccounts.Add(smtpAccount);

            Save();
        }
    }
}
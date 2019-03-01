using System.Collections;
using System.Collections.Generic;

namespace SyncService.ObjectModel.Account
{
    public class Accounts
    {
        public HiDriveAccount HiDriveAccount { get; set; }

        public IList<SmtpAccount> SmtpAccounts { get; set; } = new List<SmtpAccount>();
    }
}
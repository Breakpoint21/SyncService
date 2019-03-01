using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SyncService.Services.Account;

namespace SyncService.Pages.Account
{
    public class IndexModel : PageModel
    {
        private readonly AccountService _accountService;

        public IndexModel(AccountService accountService)
        {
            _accountService = accountService;
        }

        public void OnGet()
        {
            Data = new ViewModel();

            if (_accountService.Accounts.HiDriveAccount != null)
            {
                Data.HiDriveAccount = new ViewModel.HiDriveAccountViewModel
                {
                    AccountId = _accountService.Accounts.HiDriveAccount.AccountId,
                    UserName = _accountService.Accounts.HiDriveAccount.UserName,
                    RefreshToken = _accountService.Accounts.HiDriveAccount.RefreshToken
                };
            }

            if (_accountService.Accounts.SmtpAccounts != null)
            {
                foreach (var smtpAccount in _accountService.Accounts.SmtpAccounts)
                {
                    Data.SmtpAccounts.Add(new ViewModel.SmtpAccountViewModel {Label = smtpAccount.Label, Server = smtpAccount.Server, Port = smtpAccount.Port, Username = smtpAccount.Username, EmailTo = smtpAccount.EmailTo, Id = smtpAccount.Id});
                }
            }
            
        }

        public ViewModel Data { get; set; }

        public class ViewModel
        {
            public HiDriveAccountViewModel HiDriveAccount { get; set; }
            public IList<SmtpAccountViewModel> SmtpAccounts { get; set; } = new List<SmtpAccountViewModel>();

            public class HiDriveAccountViewModel
            {
                public string AccountId { get; set; }
                public string RefreshToken { get; set; }
                public string UserName { get; set; }
            }

            public class SmtpAccountViewModel
            {
                public Guid Id { get; set; }
                public string Label { get; set; }
                public string Server { get; set; }
                public int Port { get; set; }
                public string Username { get; set; }
                public string EmailTo { get; set; }
            }
        }
    }
}
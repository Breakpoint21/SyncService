using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SyncService.Services.Account;

namespace SyncService.Pages.Account.Smtp
{
    public class DeleteModel : PageModel
    {
        private readonly AccountService _accountService;

        public DeleteModel(AccountService accountService)
        {
            _accountService = accountService;
        }

        public IActionResult OnGet(Guid id)
        {
            var account = _accountService.Accounts.SmtpAccounts.FirstOrDefault(smtpAccount => smtpAccount.Id == id);
            if (account == null)
            {
                return NotFound();
            }
            Data = new ViewModel
            {
                Id = account.Id,
                Label = account.Label
            };
            return Page();
        }

        [BindProperty]
        public ViewModel Data { get; set; }

        public IActionResult OnPost()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }
            var account = _accountService.Accounts.SmtpAccounts.FirstOrDefault(smtpAccount => smtpAccount.Id == Data.Id);
            if (account == null)
            {
                return NotFound();
            }

            if (!_accountService.Accounts.SmtpAccounts.Remove(account))
            {
                return NotFound();
            }
            _accountService.Save();
            return RedirectToPage("/Account/Index");
        }

        public class ViewModel
        {
            public Guid Id { get; set; }
            public string Label { get; set; }
        }
    }
}
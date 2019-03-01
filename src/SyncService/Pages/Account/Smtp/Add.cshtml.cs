using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SyncService.ObjectModel.Account;
using SyncService.Services.Account;

namespace SyncService.Pages.Account.Smtp
{
    public class AddModel : PageModel
    {
        private readonly AccountService _accountService;

        public AddModel(AccountService accountService)
        {
            _accountService = accountService;
        }
        
        [BindProperty]
        public ViewModel Data { get; set; }

        public void OnGet()
        {
            Data = new ViewModel();
        }

        public IActionResult OnPost()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            var smtpAccount = new SmtpAccount
            {
                Id = Guid.NewGuid(), Label = Data.Label, Server = Data.Server, Port = Data.Port, Username = Data.Username,
                EmailTo = Data.EmailTo, Password = Data.Password
            };

            _accountService.AddSmtpAccount(smtpAccount);
            
            return RedirectToPage("/Account/Index");
        }

        public class ViewModel
        {
            public Guid Id { get; set; }
            public string Label { get; set; }
            public string Server { get; set; }
            public int Port { get; set; }
            public string Username { get; set; }
            public string EmailTo { get; set; }
            public string Password { get; set; }
        }

        public class ViewModelValidator : AbstractValidator<ViewModel>
        {
            public ViewModelValidator()
            {
                RuleFor(model => model.Label).NotEmpty();
                RuleFor(model => model.Server).NotEmpty();
                RuleFor(model => model.Port).NotEmpty();
                RuleFor(model => model.Username).NotEmpty();
                RuleFor(model => model.Password).NotEmpty();
                RuleFor(model => model.EmailTo).NotEmpty().EmailAddress();
            }
        }
    }
}
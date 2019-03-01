using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentValidation;
using MailKit.Net.Smtp;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MimeKit;
using Serilog;
using SyncService.Services.Account;

namespace SyncService.Pages.Account.Smtp
{
    public class EditModel : PageModel
    {
        private readonly AccountService _accountService;

        public EditModel(AccountService accountService)
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
                Label = account.Label,
                Server = account.Server,
                Port = account.Port,
                Username = account.Username,
                EmailTo = account.EmailTo
            };
            return Page();
        }

        [BindProperty]
        public ViewModel Data { get; set; }

        public bool? TestMailSendSuccessfully { get; set; } 
        public string TestMailMessage { get; set; }

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

            account.Label = Data.Label;
            account.Server = Data.Server;
            account.Port = Data.Port;
            account.Username = Data.Username;
            account.EmailTo = Data.EmailTo;

            if (Data.Password != null)
            {
                account.Password = Data.Password;
            }

            _accountService.Save();
            return RedirectToPage("/Account/Index");
        }

        public async Task<IActionResult> OnPostTest()
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

            try
            {
                var mimeMessage = new MimeMessage();
                mimeMessage.From.Add(new MailboxAddress("SyncService", "noreply@syncservice.com"));
                mimeMessage.To.Add(new MailboxAddress(Data.EmailTo));
                mimeMessage.Subject = "[Test]";
                mimeMessage.Body = new TextPart("plain") { Text = "" };

                using (var client = new SmtpClient())
                {
                    await client.ConnectAsync(Data.Server, Data.Port);
                    await client.AuthenticateAsync(Data.Username, Data.Password ?? account.Password);
                    await client.SendAsync(mimeMessage);
                }
            }
            catch (Exception exception)
            {
                TestMailSendSuccessfully = false;
                TestMailMessage = exception.Message;
                return Page();
            }

            TestMailSendSuccessfully = true;
            return Page();
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
                RuleFor(model => model.EmailTo).NotEmpty().EmailAddress();
            }
        }
    }
}
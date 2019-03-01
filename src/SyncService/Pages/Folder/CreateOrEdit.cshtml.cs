using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using NCrontab;
using SyncService.ObjectModel.Account;
using SyncService.ObjectModel.Folder;
using SyncService.Services.Account;
using SyncService.Services.Folder;
using SyncService.Services.Sync;

namespace SyncService.Pages.Folder
{
    public class CreateEditModel : PageModel
    {
        private readonly FolderConfigurationService _folderConfigService;
        private readonly AccountService _accountService;
        private readonly HiDriveSyncService _hiDriveSyncService;

        public CreateEditModel(FolderConfigurationService folderConfigService, AccountService accountService, HiDriveSyncService hiDriveSyncService)
        {
            _folderConfigService = folderConfigService;
            _accountService = accountService;
            _hiDriveSyncService = hiDriveSyncService;
        }

        public IActionResult OnGetCreate()
        {
            Data = new ViewModel
            {
                SmptAccounts = _accountService.Accounts.SmtpAccounts.Select(account =>
                    new SelectListItem(account.Label, account.Id.ToString())).Concat(new []{new SelectListItem("Add New", Guid.Empty.ToString())}).ToList(),
                Accounts = new List<SelectListItem>
                {
                    new SelectListItem(_accountService.Accounts.HiDriveAccount.UserName,
                        _accountService.Accounts.HiDriveAccount.AccountId)
                },
                NewSmtpAccount = new ViewModel.SmtpAccountViewModel()
            };

            return Page();
        }

        public IActionResult OnGetEdit(Guid id)
        {
            var folderConfiguration = _folderConfigService.GetAllConfigs().FirstOrDefault(configuration => configuration.Id == id);
            if (folderConfiguration == null)
            {
                return NotFound();
            }

            var hiDriveSyncTask = _hiDriveSyncService.GetTask(folderConfiguration.Id);

            Data = new ViewModel
            {
                SmptAccounts = _accountService.Accounts.SmtpAccounts.Select(account =>
                    new SelectListItem(account.Label, account.Id.ToString())).Concat(new[] { new SelectListItem("Add New", Guid.Empty.ToString()) }).ToList(),
                Accounts = new List<SelectListItem>
                {
                    new SelectListItem(_accountService.Accounts.HiDriveAccount.UserName,
                        _accountService.Accounts.HiDriveAccount.AccountId)
                },
                AccountId = folderConfiguration.AccountId,
                Id = folderConfiguration.Id,
                Label = folderConfiguration.Label,
                SourcePath = folderConfiguration.SourcePath,
                DestinationPath = folderConfiguration.DestinationPath,
                Schedule = folderConfiguration.Schedule,
                SmptAccountId = folderConfiguration.NotificationConfiguration?.EmailConfigurationId.ToString(),
                LogPath = folderConfiguration.LogPath,
                LogLevel = folderConfiguration.LogLevel,
                SendEmail = folderConfiguration.NotificationConfiguration?.SendEmail ?? false,
                SendEmailOnlyOnError = folderConfiguration.NotificationConfiguration?.SendEmailOnlyOnError ?? false,
                NewSmtpAccount = new ViewModel.SmtpAccountViewModel()
            };
            return Page();
        }

        [BindProperty]
        public ViewModel Data { get; set; }
        
        public IActionResult OnPostCreate()
        {
            if (!ModelState.IsValid)
            {
                Data.SmptAccounts = _accountService.Accounts.SmtpAccounts.Select(account =>
                        new SelectListItem(account.Label, account.Id.ToString()))
                    .Concat(new[] {new SelectListItem("Add New", Guid.Empty.ToString())}).ToList();
                Data.Accounts = new List<SelectListItem>
                {
                    new SelectListItem(_accountService.Accounts.HiDriveAccount.UserName,
                        _accountService.Accounts.HiDriveAccount.AccountId)
                };

                return Page();
            }

            if (Data.SmptAccountId == Guid.Empty.ToString() && Data.NewSmtpAccount != null)
            {
                var smtpAccount = new SmtpAccount
                {
                    Id = Guid.NewGuid(),
                    Label = Data.NewSmtpAccount.Label,
                    Server = Data.NewSmtpAccount.Server,
                    Username = Data.NewSmtpAccount.Username,
                    Password = Data.NewSmtpAccount.Password,
                    EmailTo = Data.NewSmtpAccount.EmailTo
                };
                _accountService.AddSmtpAccount(smtpAccount);
                Data.SmptAccountId = smtpAccount.Id.ToString();
            }

            Data.Id = Guid.NewGuid();

            var folderConfiguration = new FolderConfiguration
            {
                Id = Data.Id,
                Label = Data.Label,
                SourcePath = Data.SourcePath,
                DestinationPath = Data.DestinationPath,
                LogPath = Data.LogPath,
                LogLevel = Data.LogLevel,
                Schedule = Data.Schedule,
                AccountId = Data.AccountId,
                NotificationConfiguration = new FolderNotificationConfiguration
                {
                    EmailConfigurationId = Guid.Parse(Data.SmptAccountId),
                    SendEmail = Data.SendEmail,
                    SendEmailOnlyOnError = Data.SendEmailOnlyOnError
                }
            };
            _folderConfigService.AddFolderConfig(folderConfiguration);

            return RedirectToPage("/Index");
        }

        public IActionResult OnPostEdit()
        {
            if (!ModelState.IsValid)
            {
                Data.SmptAccounts = _accountService.Accounts.SmtpAccounts.Select(account =>
                        new SelectListItem(account.Label, account.Id.ToString()))
                    .Concat(new[] { new SelectListItem("Add New", Guid.Empty.ToString()) }).ToList();
                Data.Accounts = new List<SelectListItem>
                {
                    new SelectListItem(_accountService.Accounts.HiDriveAccount.UserName,
                        _accountService.Accounts.HiDriveAccount.AccountId)
                };
                return Page();
            }
            var folderConfiguration = _folderConfigService.GetAllConfigs().FirstOrDefault(configuration => configuration.Id == Data.Id);
            if (folderConfiguration == null)
            {
                return NotFound();
            }

            if (Data.SmptAccountId == Guid.Empty.ToString() && Data.NewSmtpAccount != null)
            {
                var smtpAccount = new SmtpAccount
                {
                    Id = Guid.NewGuid(),
                    Label = Data.NewSmtpAccount.Label,
                    Server = Data.NewSmtpAccount.Server,
                    Port = Data.NewSmtpAccount.Port,
                    Username = Data.NewSmtpAccount.Username,
                    Password = Data.NewSmtpAccount.Password,
                    EmailTo = Data.NewSmtpAccount.EmailTo
                };
                _accountService.AddSmtpAccount(smtpAccount);
                Data.SmptAccountId = smtpAccount.Id.ToString();
            }

            folderConfiguration.SourcePath = Data.SourcePath;
            folderConfiguration.Schedule = Data.Schedule;
            folderConfiguration.DestinationPath = Data.DestinationPath;
            folderConfiguration.LogPath = Data.LogPath;
            folderConfiguration.LogLevel = Data.LogLevel;
            folderConfiguration.AccountId = Data.AccountId;
            folderConfiguration.NotificationConfiguration.EmailConfigurationId = Guid.Parse(Data.SmptAccountId);
            folderConfiguration.NotificationConfiguration.SendEmail = Data.SendEmail;
            folderConfiguration.NotificationConfiguration.SendEmailOnlyOnError = Data.SendEmailOnlyOnError;
            _folderConfigService.Save(folderConfiguration);
            return RedirectToPage("/Index");
        }

        public IActionResult OnPostStart()
        {
            if (!ModelState.IsValid)
            {
                Data.SmptAccounts = _accountService.Accounts.SmtpAccounts.Select(account =>
                        new SelectListItem(account.Label, account.Id.ToString()))
                    .Concat(new[] { new SelectListItem("Add New", Guid.Empty.ToString()) }).ToList();
                Data.Accounts = new List<SelectListItem>
                {
                    new SelectListItem(_accountService.Accounts.HiDriveAccount.UserName,
                        _accountService.Accounts.HiDriveAccount.AccountId)
                };
                return Page();
            }
            var folderConfiguration = _folderConfigService.GetAllConfigs().FirstOrDefault(configuration => configuration.Id == Data.Id);
            if (folderConfiguration == null)
            {
                return NotFound();
            }
            if (Data.SmptAccountId == Guid.Empty.ToString() && Data.NewSmtpAccount != null)
            {
                var smtpAccount = new SmtpAccount
                {
                    Id = Guid.NewGuid(),
                    Label = Data.NewSmtpAccount.Label,
                    Server = Data.NewSmtpAccount.Server,
                    Port = Data.NewSmtpAccount.Port,
                    Username = Data.NewSmtpAccount.Username,
                    Password = Data.NewSmtpAccount.Password,
                    EmailTo = Data.NewSmtpAccount.EmailTo
                };
                _accountService.AddSmtpAccount(smtpAccount);
                Data.SmptAccountId = smtpAccount.Id.ToString();
            }

            folderConfiguration.SourcePath = Data.SourcePath;
            folderConfiguration.Schedule = Data.Schedule;
            folderConfiguration.DestinationPath = Data.DestinationPath;
            folderConfiguration.LogPath = Data.LogPath;
            folderConfiguration.LogLevel = Data.LogLevel;
            folderConfiguration.AccountId = Data.AccountId;
            folderConfiguration.NotificationConfiguration.EmailConfigurationId = Guid.Parse(Data.SmptAccountId);
            folderConfiguration.NotificationConfiguration.SendEmail = Data.SendEmail;
            folderConfiguration.NotificationConfiguration.SendEmailOnlyOnError = Data.SendEmailOnlyOnError;
            _folderConfigService.Save(folderConfiguration);
            var hiDriveSyncTask = _hiDriveSyncService.GetTask(folderConfiguration.Id);
            hiDriveSyncTask?.StartNow();
            return RedirectToPage("/Index");
        }

        public class ViewModel
        {
            public ViewModel()
            {
            }

            public Guid Id { get; set; }
            public string AccountId { get; set; }
            public string Label { get; set; }
            public string SourcePath { get; set; }
            public string DestinationPath { get; set; }
            public string LogPath { get; set; }
            public LogLevel LogLevel { get; set; }
            public string Schedule { get; set; }
            public string SmptAccountId { get; set; }
            public bool SendEmail { get; set; }
            public bool SendEmailOnlyOnError { get; set; }
            public IEnumerable<SelectListItem> SmptAccounts { get; set; }
            public IEnumerable<SelectListItem> Accounts { get; set; }
            public SmtpAccountViewModel NewSmtpAccount { get; set; }

            public class SmtpAccountViewModel
            {
                public string Label { get; set; }
                public string Server { get; set; }
                public int Port { get; set; }
                public string Username { get; set; }
                public string Password { get; set; }
                public string EmailTo { get; set; }
            }
        }

        public class ViewModelValidator : AbstractValidator<ViewModel>
        {
            public ViewModelValidator()
            {
                RuleFor(model => model.Label).NotEmpty();
                RuleFor(model => model.DestinationPath).NotEmpty();
                RuleFor(model => model.Schedule).NotEmpty().Custom((s, context) =>
                {
                    try
                    {
                        CrontabSchedule.Parse(s);
                    }
                    catch (Exception)
                    {
                        context.AddFailure("The Expression must be a valid Crontab expression!");
                    }
                });
                RuleFor(model => model.SourcePath).NotEmpty().Must(source => System.IO.Directory.Exists(source))
                    .WithMessage("Source Path must exist on local drive");
                RuleFor(model => model.SmptAccountId).NotEmpty().When(model => model.SendEmail);
                RuleFor(model => model.NewSmtpAccount).NotNull().SetValidator(model => new SmptAccountViewModelValidator()).When(model => model.SmptAccountId == Guid.Empty.ToString());
            }
        }

        public class SmptAccountViewModelValidator : AbstractValidator<ViewModel.SmtpAccountViewModel>
        {
            public SmptAccountViewModelValidator()
            {
                RuleFor(model => model.Label).NotEmpty();
                RuleFor(model => model.Server).NotEmpty();
                RuleFor(model => model.Username).NotEmpty();
                RuleFor(model => model.Password).NotEmpty();
                RuleFor(model => model.Port).NotEmpty();
                RuleFor(model => model.EmailTo).NotEmpty().EmailAddress();
            }
        }
    }
}
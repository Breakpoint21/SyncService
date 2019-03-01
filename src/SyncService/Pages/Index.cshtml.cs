using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SyncService.Services.Account;
using SyncService.Services.Folder;
using SyncService.Services.Sync;

namespace SyncService.Pages
{
    public class IndexModel : PageModel
    {
        private readonly AccountService _accountService;
        private readonly FolderConfigurationService _folderConfigurationService;
        private readonly HiDriveSyncService _hiDriveSyncService;

        public IndexModel(AccountService accountService, FolderConfigurationService folderConfigurationService, HiDriveSyncService hiDriveSyncService)
        {
            _accountService = accountService;
            _folderConfigurationService = folderConfigurationService;
            _hiDriveSyncService = hiDriveSyncService;
        }

        public ViewModel Data { get; set; }
        
        public void OnGet()
        {
            if (_accountService.Accounts.HiDriveAccount != null)
            {
                Data = new ViewModel
                {
                    HiDriveAccount = new ViewModel.HiDriveAccountViewModel
                    {
                        AccountId = _accountService.Accounts.HiDriveAccount.AccountId,
                        RefreshToken = _accountService.Accounts.HiDriveAccount.RefreshToken,
                        UserName = _accountService.Accounts.HiDriveAccount.UserName
                    },
                    Folders = _folderConfigurationService.GetAllConfigs().Select(configuration =>
                        new ViewModel.FolderConfigurationViewModel
                        {
                            Id = configuration.Id,
                            Label = configuration.Label, SourcePath = configuration.SourcePath,
                            DestinationPath = configuration.DestinationPath, Schedule = configuration.Schedule,
                            IsRunning = _hiDriveSyncService.GetTask(configuration.Id)?.IsRunning ?? false
                        }).ToList()
                };
            }
            else
            {
                Data = new ViewModel( );
            }
        }

        public class ViewModel
        {
            public HiDriveAccountViewModel HiDriveAccount { get; set; }
            public IList<FolderConfigurationViewModel> Folders { get; set; } = new List<FolderConfigurationViewModel>();

            public class HiDriveAccountViewModel
            {
                public string AccountId { get; set; }
                public string RefreshToken { get; set; }
                public string UserName { get; set; }
            }

            public class FolderConfigurationViewModel
            {
                public Guid Id { get; set; }
                public string Label { get; set; }
                public string SourcePath { get; set; }
                public string DestinationPath { get; set; }
                public string Schedule { get; set; }
                public bool IsRunning { get; set; }
            }
        }
    }
}

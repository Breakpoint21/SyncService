using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;
using SyncService.HiDriveClient.Authentication;
using SyncService.Options;
using SyncService.Services.Account;

namespace SyncService.Pages.Account
{
    public class RevokeModel : PageModel
    {
        private readonly IOptions<HiDriveApiOptions> _hiDriveApiOptions;
        private readonly AccountService _accountService;

        public RevokeModel(IOptions<HiDriveApiOptions> hiDriveApiOptions, AccountService accountService)
        {
            _hiDriveApiOptions = hiDriveApiOptions;
            _accountService = accountService;
        }

        public ViewModel Data { get; set; }

        public void OnGet()
        {
            Data = new ViewModel {HiDriveUsername = _accountService.Accounts.HiDriveAccount.UserName };
        }

        public async Task<IActionResult> OnPost()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            try
            {
                await Handle(_accountService.Accounts.HiDriveAccount.RefreshToken);
                _accountService.RevokeHiDriveAccount();
            }
            catch (AuthenticationException exception)
            {
                ModelState.AddModelError("Data.HiDriveUsername", exception.Error.Description);
                return Page();
            }

            return RedirectToPage("/Index");
        }

        public async Task Handle(string refreshToken)
        {
            var hiDriveAuthenticator = new HiDriveAuthenticator(_hiDriveApiOptions.Value.HiDriveClientId,
                _hiDriveApiOptions.Value.HiDriveClientSecret);
            await hiDriveAuthenticator.RevokeRefreshToken(refreshToken);
        }

        public class ViewModel
        {
            public string HiDriveUsername { get; set; }

        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentValidation;
using Kyrodan.HiDrive;
using Kyrodan.HiDrive.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;
using SyncService.ObjectModel.Account;
using SyncService.Options;
using SyncService.Services.Account;

namespace SyncService.Pages.Account
{
    public class AddModel : PageModel
    {
        private readonly IOptions<HiDriveApiOptions> _hiDriveApiOptions;
        private readonly AccountService _accountService;

        public AddModel(IOptions<HiDriveApiOptions> hiDriveApiOptions, AccountService accountService)
        {
            _hiDriveApiOptions = hiDriveApiOptions;
            _accountService = accountService;
        }

        public void OnGet()
        {
            Data = new ViewModel {LoginUrl = $"https://my.hidrive.com/client/authorize?client_id={_hiDriveApiOptions.Value.HiDriveClientId}&response_type=code&scope=admin,rw" };
        }

        [BindProperty]
        public ViewModel Data { get; set; }

        public async Task<IActionResult> OnPost()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            try
            {
                var hiDriveAccount = await Handle(Data.Code);
                _accountService.AddHiDriveAccount(hiDriveAccount);
            }
            catch (AuthenticationException exception)
            {
                ModelState.AddModelError("Data.Code", exception.Error.Description);
                return Page();
            }

            return RedirectToPage("/Index");
        }

        public async Task<HiDriveAccount> Handle(string authorizationCode)
        {
            var hiDriveAuthenticator = new HiDriveAuthenticator(_hiDriveApiOptions.Value.HiDriveClientId,
                _hiDriveApiOptions.Value.HiDriveClientSecret);
            var oAuth2Token = await hiDriveAuthenticator.AuthenticateByAuthorizationCodeAsync(authorizationCode);

            var hiDriveClient = new HiDriveClient(hiDriveAuthenticator);
            var user = await hiDriveClient.User.Me.Get().ExecuteAsync();

            return new HiDriveAccount { AccountId = user.Account, UserName = user.Alias, RefreshToken = oAuth2Token.RefreshToken };
        }

        public class ViewModel
        {
            public string LoginUrl { get; set; }
            
            public string Code { get; set; }
        }

        public class ViewModelValidator : AbstractValidator<ViewModel>
        {
            public ViewModelValidator()
            {
                RuleFor(model => model.Code).NotEmpty();
            }
        }
    }
}
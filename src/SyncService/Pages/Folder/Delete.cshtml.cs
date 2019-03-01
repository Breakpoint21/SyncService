using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SyncService.Services.Folder;

namespace SyncService.Pages.Folder
{
    public class DeleteModel : PageModel
    {
        private readonly FolderConfigurationService _folderConfigurationService;

        public DeleteModel(FolderConfigurationService folderConfigurationService)
        {
            _folderConfigurationService = folderConfigurationService;
        }

        [BindProperty]
        public ViewModel Data { get; set; }

        public IActionResult OnGet(Guid id)
        {
            var folderConfiguration = _folderConfigurationService.GetAllConfigs().FirstOrDefault(configuration => configuration.Id == id);
            if (folderConfiguration == null)
            {
                return NotFound();
            }
            Data = new ViewModel {Id = folderConfiguration.Id, Label = folderConfiguration.Label};
            return Page();
        }

        public IActionResult OnPost()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            var folderConfiguration = _folderConfigurationService.GetAllConfigs().FirstOrDefault(configuration => configuration.Id == Data.Id);
            if (folderConfiguration == null)
            {
                return NotFound();
            }
            _folderConfigurationService.DeleteConfig(folderConfiguration);
            return RedirectToPage("/Index");
        }

        public class ViewModel
        {
            public Guid Id { get; set; }
            public string Label { get; set; }
        }
    }
}
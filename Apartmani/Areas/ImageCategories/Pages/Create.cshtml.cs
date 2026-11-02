using System.Threading.Tasks;
using Apartmani.Data;
using Apartmani.Models;
using Apartmani.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Apartmani.Pages.ImageCategories
{
    [Authorize(Roles = SD.AdminEndUser)]
    public class CreateModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        public CreateModel(ApplicationDbContext context) { _context = context; }
        [BindProperty] public ImageCategory ImageCategory { get; set; }
        public IActionResult OnGet() => Page();

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid || ImageCategory == null) return Page();
            var name = ImageCategory.Name.Trim();
            if (EditModel.IsSystemCategory(name) || await _context.ImageCategory.AnyAsync(category => category.Name == name))
            {
                ModelState.AddModelError(string.Empty, "A category with this name already exists or is reserved.");
                return Page();
            }
            _context.ImageCategory.Add(new ImageCategory { Name = name });
            await _context.SaveChangesAsync();
            return RedirectToPage("./Index");
        }
    }
}

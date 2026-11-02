using System;
using System.Linq;
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
    public class EditModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        public EditModel(ApplicationDbContext context) { _context = context; }
        [BindProperty] public ImageCategory ImageCategory { get; set; }

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            ImageCategory = await _context.ImageCategory.FirstOrDefaultAsync(category => category.Id == id);
            return ImageCategory == null ? NotFound() : Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid || ImageCategory == null) return Page();
            var category = await _context.ImageCategory.FindAsync(ImageCategory.Id);
            if (category == null) return NotFound();
            var name = ImageCategory.Name.Trim();
            if (IsSystemCategory(category.Name) || IsSystemCategory(name))
            {
                ModelState.AddModelError(string.Empty, "The built-in categories cannot be renamed or reused.");
                return Page();
            }
            if (await _context.ImageCategory.AnyAsync(item => item.Id != category.Id && item.Name == name))
            {
                ModelState.AddModelError(string.Empty, "A category with this name already exists.");
                return Page();
            }
            // Existing image paths remain unchanged; category names are not filesystem paths.
            category.Name = name;
            await _context.SaveChangesAsync();
            return RedirectToPage("./Index");
        }

        public static bool IsSystemCategory(string name) => new[] { "Carousel", "Apartmani", "Gallery" }
            .Any(value => string.Equals(value, name, StringComparison.OrdinalIgnoreCase));
    }
}

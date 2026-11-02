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
    public class DeleteModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        public DeleteModel(ApplicationDbContext context) { _context = context; }
        [BindProperty] public ImageCategory ImageCategory { get; set; }

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            ImageCategory = await _context.ImageCategory.FirstOrDefaultAsync(category => category.Id == id);
            return ImageCategory == null ? NotFound() : Page();
        }

        public async Task<IActionResult> OnPostAsync(int? id)
        {
            var category = await _context.ImageCategory.FindAsync(id);
            if (category == null) return NotFound();
            if (EditModel.IsSystemCategory(category.Name) || await _context.Image.AnyAsync(image => image.CategoryId == category.Id))
            {
                ImageCategory = category;
                ModelState.AddModelError(string.Empty, "Built-in categories and categories containing images cannot be deleted.");
                return Page();
            }
            _context.ImageCategory.Remove(category);
            await _context.SaveChangesAsync();
            return RedirectToPage("./Index");
        }
    }
}

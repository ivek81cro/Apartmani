using System.Threading.Tasks;
using Apartmani.Data;
using Apartmani.Models;
using Apartmani.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace Apartmani.Pages.Images
{
    [Authorize(Roles = SD.AdminEndUser)]
    public class EditModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        public EditModel(ApplicationDbContext context) { _context = context; }
        [BindProperty] public Image Image { get; set; }

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            Image = await _context.Image.FirstOrDefaultAsync(item => item.Id == id);
            if (Image == null) return NotFound();
            LoadCategories();
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (Image == null) return BadRequest();
            var existing = await _context.Image.FindAsync(Image.Id);
            if (existing == null) return NotFound();
            if (!await _context.ImageCategory.AnyAsync(category => category.Id == Image.CategoryId))
            {
                ModelState.AddModelError(string.Empty, "Choose a valid category.");
                Image.Path = existing.Path;
                LoadCategories();
                return Page();
            }
            existing.CategoryId = Image.CategoryId;
            await _context.SaveChangesAsync();
            return RedirectToPage("./Index");
        }

        private void LoadCategories() => ViewData["CategoryId"] = new SelectList(_context.ImageCategory, "Id", "Name");
    }
}

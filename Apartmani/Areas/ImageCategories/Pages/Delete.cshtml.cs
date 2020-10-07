using Apartmani.Data;
using Apartmani.Models;
using Apartmani.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.IO;
using System.Threading.Tasks;

namespace Apartmani.Pages.ImageCategories
{
    [Authorize(Roles = SD.AdminEndUser)]
    public class DeleteModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _hostEnvironment;

        public DeleteModel(ApplicationDbContext context, IWebHostEnvironment hostEnvironment)
        {
            _context = context;
            _hostEnvironment = hostEnvironment;
        }

        [BindProperty]
        public ImageCategory ImageCategory { get; set; }

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            ImageCategory = await _context.ImageCategory.FirstOrDefaultAsync(m => m.Id == id);

            if (ImageCategory == null)
            {
                return NotFound();
            }
            return Page();
        }

        public async Task<IActionResult> OnPostAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            ImageCategory = await _context.ImageCategory.FindAsync(id);

            if (ImageCategory != null)
            {
                _context.ImageCategory.Remove(ImageCategory);
                await _context.SaveChangesAsync();

                string webRoot = _hostEnvironment.WebRootPath;
                var path = Path.Combine(webRoot, $"img\\{ImageCategory.Name.ToLower()}");
                if (Directory.Exists(path))
                    Directory.Delete(path, true);
            }

            return RedirectToPage("./Index");
        }
    }
}

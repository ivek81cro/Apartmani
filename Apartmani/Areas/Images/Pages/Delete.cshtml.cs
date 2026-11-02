using System;
using System.IO;
using System.Threading.Tasks;
using Apartmani.Data;
using Apartmani.Models;
using Apartmani.Utilities;
using Apartmani.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Apartmani.Pages.Images
{
    [Authorize(Roles = SD.AdminEndUser)]
    public class DeleteModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<DeleteModel> _logger;

        public DeleteModel(ApplicationDbContext context, IWebHostEnvironment environment, ILogger<DeleteModel> logger)
        {
            _context = context;
            _environment = environment;
            _logger = logger;
        }

        [BindProperty] public Image Image { get; set; }

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            Image = await _context.Image.Include(item => item.ImageCategory).FirstOrDefaultAsync(item => item.Id == id);
            return Image == null ? NotFound() : Page();
        }

        public async Task<IActionResult> OnPostAsync(int? id)
        {
            var image = await _context.Image.FindAsync(id);
            if (image == null) return NotFound();
            var path = ImageStorage.ResolveImagePath(_environment.WebRootPath, image.Path);
            var shared = await _context.Image.AnyAsync(item => item.Id != image.Id && item.Path == image.Path);
            _context.Image.Remove(image);
            await _context.SaveChangesAsync();
            if (path != null && !shared)
            {
                try { System.IO.File.Delete(path); }
                catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
                {
                    _logger.LogWarning(ex, "Could not remove image file after deleting image {ImageId}", image.Id);
                }
            }
            return RedirectToPage("./Index");
        }
    }
}

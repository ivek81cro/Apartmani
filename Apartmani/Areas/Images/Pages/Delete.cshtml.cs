using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Apartmani.Data;
using Apartmani.Models;
using Microsoft.AspNetCore.Authorization;
using Apartmani.Utility;
using Microsoft.AspNetCore.Hosting;
using System.IO;
using Microsoft.Extensions.FileProviders;

namespace Apartmani.Pages.Images
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
        public Image Image { get; set; }

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            Image = await _context.Image
                .Include(i => i.ImageCategory).FirstOrDefaultAsync(m => m.Id == id);

            if (Image == null)
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

            Image = await _context.Image.FindAsync(id);

            if (Image != null)
            {
                _context.Image.Remove(Image);

                string webRoot = _hostEnvironment.WebRootPath;
                var path = Path.Combine(webRoot, $"{Image.Path}");
                var file = new FileInfo(path);
                if (file.Exists)
                    file.Delete();

                await _context.SaveChangesAsync();
            }

            return RedirectToPage("./Index");
        }
    }
}

using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Apartmani.Data;
using Apartmani.Models;
using Microsoft.AspNetCore.Hosting;
using System.IO;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Apartmani.Utility;

namespace Apartmani.Pages.Images
{
    [Authorize(Roles = SD.AdminEndUser)]
    
    //TODO: Change upload model for images, fixed dir names
    public class CreateModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _hostingEnviroment;

        public CreateModel(ApplicationDbContext context, IWebHostEnvironment hostingEnvironment)
        {
            _context = context;
            _hostingEnviroment = hostingEnvironment;
        }

        public IActionResult OnGet()
        {
            ViewData["CategoryId"] = new SelectList(_context.ImageCategory, "Id", "Name");
            return Page();
        }

        [BindProperty]
        public Image Image { get; set; }

        // To protect from overposting attacks, enable the specific properties you want to bind to, for
        // more details, see https://aka.ms/RazorPagesCRUD.
        public async Task<IActionResult> OnPostAsync()
        {
            var files = HttpContext.Request.Form.Files;

            if (!ModelState.IsValid || files.Count < 1 )
            {
                ViewData["CategoryId"] = new SelectList(_context.ImageCategory, "Id", "Name");
                ViewData["FileMessage"] = $"Select image for upload!";
                return Page();
            }

            Image.Path = files.FirstOrDefault().FileName;
            _context.Image.Add(Image);
            await _context.SaveChangesAsync();

            var imageFromDb = await _context.Image.FindAsync(Image.Id);
            var categoryName = await _context.ImageCategory.Where(c => c.Id == imageFromDb.CategoryId).ToListAsync();
            string name = categoryName.FirstOrDefault().Name.ToLower();
            string webRootPath = _hostingEnviroment.WebRootPath;

            if (files.Count > 0)
            {
                var uploads = Path.Combine(webRootPath, $"img/{ name }"); //for IIS ...$"img\\...
                var extension = Path.GetExtension(files[0].FileName);

                using (var fileStream = new FileStream(Path.Combine(uploads, imageFromDb.Id + extension), FileMode.Create))
                {
                    files[0].CopyTo(fileStream);
                }
                Image.Path = $"img\\{ name }\\" + imageFromDb.Id + extension;
            }

            await _context.SaveChangesAsync();

            return RedirectToPage("./Index");
        }
    }
}

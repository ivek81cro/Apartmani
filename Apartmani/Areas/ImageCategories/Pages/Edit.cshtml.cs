using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Apartmani.Data;
using Apartmani.Models;
using Apartmani.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using System.IO;
using Microsoft.AspNetCore.Http;
using System;

namespace Apartmani.Pages.ImageCategories
{
    [Authorize(Roles = SD.AdminEndUser)]
    public class EditModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _hostEnvironment;

        public EditModel(ApplicationDbContext context, IWebHostEnvironment hostEnvironment)
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

            CookieOptions option = new CookieOptions();
            option.Expires = DateTime.Now.AddSeconds(60);

            Response.Cookies.Append("Category", ImageCategory.Name.ToLower(), option);

            if (ImageCategory == null)
            {
                return NotFound();
            }

            return Page();
        }

        // To protect from overposting attacks, enable the specific properties you want to bind to, for
        // more details, see https://aka.ms/RazorPagesCRUD.
        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            _context.Attach(ImageCategory).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();

                string cookieValueFromReq = Request.Cookies["Category"];
                string webRoot = _hostEnvironment.WebRootPath;
                string pathSource = Path.Combine(webRoot, $"img\\{cookieValueFromReq}");
                string pathDestin = Path.Combine(webRoot, $"img\\{ImageCategory.Name.ToLower()}");
                if (pathSource != pathDestin)
                    if (Directory.Exists(pathSource))
                        Directory.Move(pathSource, pathDestin);
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ImageCategoryExists(ImageCategory.Id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return RedirectToPage("./Index");
        }

        private bool ImageCategoryExists(int id)
        {
            return _context.ImageCategory.Any(e => e.Id == id);
        }
    }
}

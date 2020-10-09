using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Apartmani.Data;
using Apartmani.Models;
using Apartmani.Utility;
using Microsoft.AspNetCore.Authorization;
using System.IO;
using Microsoft.AspNetCore.Hosting;

namespace Apartmani.Pages.ImageCategories
{
    [Authorize(Roles = SD.AdminEndUser)]
    public class CreateModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _hostEnviroment;

        public CreateModel(ApplicationDbContext context, IWebHostEnvironment hostEnvironment)
        {
            _context = context;
            _hostEnviroment = hostEnvironment;
        }

        public IActionResult OnGet()
        {
            return Page();
        }

        [BindProperty]
        public ImageCategory ImageCategory { get; set; }

        // To protect from overposting attacks, enable the specific properties you want to bind to, for
        // more details, see https://aka.ms/RazorPagesCRUD.
        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            _context.ImageCategory.Add(ImageCategory);

            await _context.SaveChangesAsync();

            string webRoot = _hostEnviroment.WebRootPath;
            var path = Path.Combine(webRoot, $"img/{ImageCategory.Name.ToLower()}"); //for IIS ...$"img\\..
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);

            return RedirectToPage("./Index");
        }
    }
}

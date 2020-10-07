using Apartmani.Models;
using Apartmani.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace Apartmani.Pages.ImageCategories
{
    [Authorize(Roles = SD.AdminEndUser)]
    public class DetailsModel : PageModel
    {
        private readonly Apartmani.Data.ApplicationDbContext _context;

        public DetailsModel(Apartmani.Data.ApplicationDbContext context)
        {
            _context = context;
        }

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
    }
}

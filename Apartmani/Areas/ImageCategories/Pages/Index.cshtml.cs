using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Apartmani.Models;
using Microsoft.AspNetCore.Authorization;
using Apartmani.Utility;

namespace Apartmani.Pages.ImageCategories
{
    [Authorize(Roles = SD.AdminEndUser)]
    public class IndexModel : PageModel
    {
        private readonly Apartmani.Data.ApplicationDbContext _context;

        public IndexModel(Apartmani.Data.ApplicationDbContext context)
        {
            _context = context;
        }

        public IList<ImageCategory> ImageCategory { get;set; }

        public async Task OnGetAsync()
        {
            ImageCategory = await _context.ImageCategory.ToListAsync();
        }
    }
}

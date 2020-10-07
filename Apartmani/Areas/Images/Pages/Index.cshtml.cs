using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Apartmani.Data;
using Apartmani.Models;
using Microsoft.AspNetCore.Authorization;
using Apartmani.Utility;
using Microsoft.AspNetCore.Mvc;

namespace Apartmani.Pages.Images
{
    [Authorize(Roles = SD.AdminEndUser)]
    [Area("Images")]
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public IndexModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public IList<Image> Image { get; set; }

        public async Task OnGetAsync()
        {
            Image = await _context.Image
                .Include(i => i.ImageCategory).ToListAsync();
        }
    }
}

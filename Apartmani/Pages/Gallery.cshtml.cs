using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Apartmani.Data;
using Apartmani.Models;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Apartmani.Pages
{
    public class GalleryModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public GalleryModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public List<Image> Images { get; private set; }

        public async Task OnGetAsync()
        {
            Images = await _context.Image.Where(i => i.ImageCategory.Name == "Gallery").ToListAsync();
        }
    }
}

using Apartmani.Data;
using Apartmani.Models;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Apartmani.Pages
{
    //TODO: Fix mobile view
    public class ApartmaniModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public ApartmaniModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public IList<Image> Images { get; set; }
        public async Task OnGetAsync()
        {
            Images = await _context.Image.Where(i => i.ImageCategory.Name == "Apartmani").ToListAsync();
        }
    }
}

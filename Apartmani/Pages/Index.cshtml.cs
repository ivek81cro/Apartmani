using Apartmani.Data;
using Apartmani.Models;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Apartmani.Pages
{
    public class IndexModel : PageModel
    {
        private readonly ILogger<IndexModel> _logger;
        private readonly ApplicationDbContext _context;

        public IndexModel(ILogger<IndexModel> logger, ApplicationDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        public IList<Image> Images { get; set; }
        public async Task OnGetAsync()
        {
            Images = await _context.Image.Where(i => i.ImageCategory.Name == "Carousel").ToListAsync();
        }
    }
}

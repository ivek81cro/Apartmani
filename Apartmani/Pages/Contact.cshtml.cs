using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

namespace Apartmani.Pages
{
    //TODO: Add contact form and fix mobile view
    public class ContactModel : PageModel
    {
        [Required]
        public string Name { get; set; }
        [Required]
        [EmailAddress]
        public string Email { get; set; }
        public string Message { get; set; }

        private readonly IEmailSender _emailSender;
        public ContactModel(IEmailSender emailSender)
        {
            _emailSender = emailSender;
        }

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            if (ModelState.IsValid)
            {
                await _emailSender.SendEmailAsync("apartman.tokic@gmail.com",
                    "Poruka sa stranice apartmani-tokic.com, kontakt forma",
                    $"<h3>Name: {Request.Form["Name"]}</h3><a href='{Request.Form["Email"]}'>Email pošiljatelja: " +
                    $"{Request.Form["Email"]}</a><p>Poruka:<br /> {Request.Form["Message"]}</p>");

                return Redirect("ContactSuccess");
            }
            else
            {
                return Page();
            }
        }
    }
}

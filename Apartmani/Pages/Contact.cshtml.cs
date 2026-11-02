using Apartmani.Services;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.RateLimiting;
using System.ComponentModel.DataAnnotations;
using System;
using System.IO;
using System.Net.Mail;
using System.Threading.Tasks;

namespace Apartmani.Pages
{
    [EnableRateLimiting("contact")]
    public class ContactModel : PageModel
    {
        [BindProperty, Required, StringLength(100), Display(Name = "Ime i prezime")]
        public string Name { get; set; }

        [BindProperty, Required, EmailAddress, StringLength(254), Display(Name = "E-mail adresa")]
        public string Email { get; set; }

        [BindProperty, Required, StringLength(5000), Display(Name = "Poruka")]
        public string Message { get; set; }

        [BindProperty]
        public string Website { get; set; }

        private readonly IContactEmailSender _emailSender;
        private readonly IStringLocalizer<SharedResource> _localizer;
        private readonly ILogger<ContactModel> _logger;

        public ContactModel(IContactEmailSender emailSender, IStringLocalizer<SharedResource> localizer, ILogger<ContactModel> logger)
        {
            _emailSender = emailSender;
            _localizer = localizer;
            _logger = logger;
        }

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!string.IsNullOrWhiteSpace(Website))
            {
                return RedirectToPage("/ContactSuccess");
            }

            if (!ModelState.IsValid)
            {
                return Page();
            }

            try
            {
                await _emailSender.SendContactAsync("apartman.tokic@gmail.com", Name, Email, Message);
                return RedirectToPage("/ContactSuccess");
            }
            catch (FormatException)
            {
                ModelState.AddModelError(nameof(Email), _localizer["Provjerite adresu e-pošte."]);
                return Page();
            }
            catch (Exception exception) when (exception is SmtpException || exception is IOException || exception is InvalidOperationException || exception is OperationCanceledException)
            {
                _logger.LogError(exception, "Could not send a contact enquiry.");
                ModelState.AddModelError(string.Empty, _localizer["Poruku trenutačno nije moguće poslati. Pokušajte ponovno ili nam se javite telefonom."]);
                return Page();
            }
        }
    }
}

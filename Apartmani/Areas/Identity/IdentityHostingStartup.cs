using Microsoft.AspNetCore.Hosting;

[assembly: HostingStartup(typeof(Apartmani.Areas.Identity.IdentityHostingStartup))]
namespace Apartmani.Areas.Identity
{
    public class IdentityHostingStartup : IHostingStartup
    {
        public void Configure(IWebHostBuilder builder)
        {
            builder.ConfigureServices((context, services) => {
            });
        }
    }
}
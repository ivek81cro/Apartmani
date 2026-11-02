using System.Threading.Tasks;

namespace Apartmani.Services
{
    public interface IContactEmailSender
    {
        Task SendContactAsync(string recipient, string name, string email, string message);
    }
}

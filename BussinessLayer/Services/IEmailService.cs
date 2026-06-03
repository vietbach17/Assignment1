using System.Threading.Tasks;

namespace BussinessLayer.Services
{
    public interface IEmailService
    {
        Task SendEmailAsync(string toEmail, string subject, string body);
    }
}

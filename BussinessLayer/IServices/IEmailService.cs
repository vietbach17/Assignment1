using System.Threading.Tasks;

namespace BussinessLayer.IServices
{
    public interface IEmailService
    {
        Task SendEmailAsync(string toEmail, string subject, string body);
    }
}

using System;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace BussinessLayer.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;

        public EmailService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task SendEmailAsync(string toEmail, string subject, string body)
        {
            try
            {
                var smtpHost = _configuration["EmailSettings:SmtpServer"];
                var smtpPortStr = _configuration["EmailSettings:Port"];
                var fromEmail = _configuration["EmailSettings:SenderEmail"];
                var password = _configuration["EmailSettings:SenderPassword"];

                Console.WriteLine($"[SMTP DEBUG] Host: {smtpHost}, Port: {smtpPortStr}, Sender: {fromEmail}, Pwd Length: {password?.Length ?? 0}");

                if (string.IsNullOrEmpty(smtpHost) || string.IsNullOrEmpty(fromEmail))
                {
                    // Fallback to Console logging if not configured (Mock Email Sender)
                    Console.WriteLine("==================================================");
                    Console.WriteLine($"[EMAIL MOCK SENDER] Gửi email đến: {toEmail}");
                    Console.WriteLine($"Tiêu đề: {subject}");
                    Console.WriteLine($"Nội dung:\n{body}");
                    Console.WriteLine("==================================================");
                    return;
                }

                int smtpPort = int.TryParse(smtpPortStr, out int p) ? p : 587;

                using (var client = new SmtpClient(smtpHost, smtpPort))
                {
                    client.DeliveryMethod = SmtpDeliveryMethod.Network;
                    client.EnableSsl = true;
                    client.UseDefaultCredentials = false;
                    client.Credentials = new NetworkCredential(fromEmail, password);

                    var mailMessage = new MailMessage
                    {
                        From = new MailAddress(fromEmail, "StudyMind System"),
                        Subject = subject,
                        Body = body,
                        IsBodyHtml = true
                    };
                    mailMessage.To.Add(toEmail);

                    await client.SendMailAsync(mailMessage);
                    
                    Console.WriteLine($"[EMAIL SUCCESS] Đã gửi email đến: {toEmail} qua SMTP.");
                }
            }
            catch (Exception ex)
            {
                // Ghi nhận lỗi và fallback sang Console để tránh làm gián đoạn luồng chính
                Console.WriteLine("==================================================");
                Console.WriteLine($"[EMAIL ERROR] Gửi mail lỗi: {ex.Message}");
                Console.WriteLine($"[EMAIL FALLBACK] Gửi email đến: {toEmail}");
                Console.WriteLine($"Tiêu đề: {subject}");
                Console.WriteLine($"Nội dung:\n{body}");
                Console.WriteLine("==================================================");
            }
        }
    }
}

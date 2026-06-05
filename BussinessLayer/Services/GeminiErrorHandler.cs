using System;
using System.Net;
using System.Text.Json;
// file này để demo, tránh output ra lỗi của AI nha
namespace BussinessLayer.Services
{
    public static class GeminiErrorHandler
    {
        public static string HandleErrorResponse(HttpStatusCode statusCode, string responseContent)
        {
            // Trường hợp lỗi Service Unavailable hoặc mô hình đang quá tải (HTTP 503 / UNAVAILABLE)
            if (statusCode == HttpStatusCode.ServiceUnavailable || 
                responseContent.Contains("UNAVAILABLE") || 
                responseContent.Contains("experiencing high demand") || 
                responseContent.Contains("503"))
            {
                return "Hệ thống AI hiện đang bận do số lượng yêu cầu quá tải (Service Unavailable). Vui lòng thử lại sau giây lát.";
            }

            try
            {
                using var doc = JsonDocument.Parse(responseContent);
                var root = doc.RootElement;
                if (root.TryGetProperty("error", out var errorElement))
                {
                    if (errorElement.TryGetProperty("message", out var messageElement))
                    {
                        var msg = messageElement.GetString();
                        if (!string.IsNullOrEmpty(msg))
                        {
                            if (msg.Contains("API key not valid") || msg.Contains("API_KEY_INVALID"))
                            {
                                return "API Key của Gemini không hợp lệ hoặc đã hết hạn. Vui lòng kiểm tra cấu hình trong file .env.";
                            }
                            if (msg.Contains("quota") || msg.Contains("Quota exceeded"))
                            {
                                return "Tài khoản Gemini của bạn đã hết hạn mức sử dụng (Quota Exceeded). Vui lòng thử lại hoặc nâng cấp tài khoản.";
                            }
                            return $"[Lỗi API Gemini]: {msg}";
                        }
                    }
                }
            }
            catch
            {
                // Bỏ qua lỗi parse JSON để trả về lỗi mặc định ở dưới
            }

            return $"[Lỗi API từ Google Gemini (HTTP {statusCode})]: {responseContent}";
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using BussinessLayer.Interfaces;
using Microsoft.Extensions.Configuration;

namespace BussinessLayer.Services
{
    public class GeminiService : IGeminiService
    {
        private static readonly HttpClient _httpClient = new HttpClient();
        private readonly string _apiKey;
        private readonly string _model;
        private readonly string _apiUrl;

        public GeminiService(IConfiguration configuration)
        {
            var geminiSection = configuration.GetSection("Gemini");
            _apiKey = geminiSection["ApiKey"] ?? string.Empty;
            _model = geminiSection["Model"] ?? "gemini-3.5-flash";
            _apiUrl = geminiSection["ApiUrl"] ?? "https://generativelanguage.googleapis.com/v1beta/models/";
        }

        public async Task<string> GenerateContentAsync(string prompt, IEnumerable<BussinessLayer.DTOs.ChatMessageDto> history, IEnumerable<string> documentPaths, bool restrictToDocs)
        {
            if (string.IsNullOrWhiteSpace(_apiKey))
            {
                return "Cảnh báo từ hệ thống: Chưa cấu hình Gemini API Key trong appsettings.json. Vui lòng thêm ApiKey của bạn để bắt đầu trò chuyện.";
            }

            try
            {
                var contents = new List<object>();

                // 1. Nạp lịch sử hội thoại trước đó (không gửi lại file để tiết kiệm token)
                if (history != null)
                {
                    foreach (var msg in history)
                    {
                        contents.Add(new
                        {
                            role = msg.Role == "model" ? "model" : "user",
                            parts = new[]
                            {
                                new { text = msg.Text }
                            }
                        });
                    }
                }

                // 2. Chuẩn bị phần nội dung của lượt chat hiện tại
                var currentParts = new List<object>();

                // Xử lý các tài liệu đính kèm (chỉ gửi trong lượt chat cuối)
                if (documentPaths != null)
                {
                    foreach (var path in documentPaths)
                    {
                        if (!File.Exists(path)) continue;

                        var ext = Path.GetExtension(path).ToLower();
                        var fileName = Path.GetFileName(path);

                        if (ext == ".pdf")
                        {
                            try
                            {
                                var bytes = await File.ReadAllBytesAsync(path);
                                var base64 = Convert.ToBase64String(bytes);
                                currentParts.Add(new
                                {
                                    inlineData = new
                                    {
                                        mimeType = "application/pdf",
                                        data = base64
                                    }
                                });
                            }
                            catch (Exception ex)
                            {
                                currentParts.Add(new { text = $"[Lỗi đọc file PDF {fileName}: {ex.Message}]" });
                            }
                        }
                        else if (ext == ".docx")
                        {
                            var text = ExtractTextFromDocx(path);
                            if (!string.IsNullOrWhiteSpace(text))
                            {
                                currentParts.Add(new { text = $"[Nội dung tài liệu DOCX: {fileName}]\n{text}" });
                            }
                        }
                        else if (ext == ".pptx")
                        {
                            var text = ExtractTextFromPptx(path);
                            if (!string.IsNullOrWhiteSpace(text))
                            {
                                currentParts.Add(new { text = $"[Nội dung tài liệu PPTX (Slide): {fileName}]\n{text}" });
                            }
                        }
                        else if (ext == ".txt")
                        {
                            try
                            {
                                var text = await File.ReadAllTextAsync(path);
                                currentParts.Add(new { text = $"[Nội dung tài liệu TXT: {fileName}]\n{text}" });
                            }
                            catch (Exception ex)
                            {
                                currentParts.Add(new { text = $"[Lỗi đọc file TXT {fileName}: {ex.Message}]" });
                            }
                        }
                    }
                }

                // Thêm câu hỏi của user
                currentParts.Add(new { text = prompt });

                // Thêm lượt chat hiện tại vào danh sách hội thoại
                contents.Add(new
                {
                    role = "user",
                    parts = currentParts.ToArray()
                });

                // Xác định System Instruction để cấu hình hành vi của AI
                string systemInstructionText;
                if (restrictToDocs)
                {
                    systemInstructionText = "Bạn là trợ lý học tập AI của EduManager. Nhiệm vụ của bạn là trả lời các câu hỏi của sinh viên. " +
                                            "Tuy nhiên, do người dùng đã bật chế độ 'Trong phạm vi tài liệu', bạn CHỈ ĐƯỢC PHÉP TRẢ LỜI câu hỏi dựa trên các tài liệu đã được gửi kèm ở trên. " +
                                            "Nếu thông tin cần để trả lời câu hỏi không tồn tại trong các tài liệu đã cung cấp, bạn phải lịch sự từ chối trả lời và nói rõ rằng thông tin này không nằm trong phạm vi các tài liệu học tập hiện có. " +
                                            "Hãy trả lời bằng tiếng Việt một cách tự nhiên, mạch lạc, dễ hiểu.";
                }
                else
                {
                    systemInstructionText = "Bạn là trợ lý học tập AI của EduManager. Bạn có nhiệm vụ giải đáp các câu hỏi học tập của sinh viên một cách chi tiết, chính xác và chuyên nghiệp. " +
                                            "Bạn có thể kết hợp thông tin trong tài liệu gửi kèm (nếu có) cùng với kiến thức chung của bạn để giải thích chi tiết nhất có thể. " +
                                            "Hãy trả lời bằng tiếng Việt, định dạng markdown đẹp mắt, dễ đọc.";
                }

                // Cấu trúc payload request body
                var requestBody = new
                {
                    contents = contents.ToArray(),
                    system_instruction = new
                    {
                        parts = new[]
                        {
                            new { text = systemInstructionText }
                        }
                    }
                };

                var url = $"{_apiUrl.TrimEnd('/')}/{_model}:generateContent?key={_apiKey}";
                var jsonPayload = JsonSerializer.Serialize(requestBody);
                var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync(url, content);
                var responseString = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    return $"[Lỗi API từ Google Gemini (HTTP {response.StatusCode})]: {responseString}";
                }

                using var doc = JsonDocument.Parse(responseString);
                var root = doc.RootElement;

                if (root.TryGetProperty("candidates", out var candidates) && candidates.GetArrayLength() > 0)
                {
                    var firstCandidate = candidates[0];
                    if (firstCandidate.TryGetProperty("content", out var contentObj) &&
                        contentObj.TryGetProperty("parts", out var responseParts) &&
                        responseParts.GetArrayLength() > 0)
                    {
                        return responseParts[0].GetProperty("text").GetString() ?? "Không thể nhận câu trả lời.";
                    }
                }

                return "Không nhận được phản hồi hợp lệ từ mô hình AI.";
            }
            catch (Exception ex)
            {
                return $"Đã xảy ra lỗi trong quá trình kết nối với Gemini AI: {ex.Message}";
            }
        }

        public async Task<string> GetDocumentTextAsync(string path)
        {
            if (!File.Exists(path)) return "Tài liệu không tồn tại trên máy chủ.";

            var ext = Path.GetExtension(path).ToLower();
            if (ext == ".pdf")
            {
                return ExtractTextFromPdf(path);
            }
            if (ext == ".docx")
            {
                return ExtractTextFromDocx(path);
            }
            if (ext == ".pptx")
            {
                return ExtractTextFromPptx(path);
            }
            if (ext == ".txt")
            {
                try
                {
                    return await File.ReadAllTextAsync(path);
                }
                catch (Exception ex)
                {
                    return $"[Lỗi đọc file văn bản TXT: {ex.Message}]";
                }
            }

            return "Định dạng tài liệu này hiện chưa hỗ trợ xem trước văn bản thô.";
        }

        /// <summary>
        /// Trích xuất text thô từ file PDF sử dụng PdfPig
        /// </summary>
        private string ExtractTextFromPdf(string path)
        {
            try
            {
                var sb = new StringBuilder();
                using (var document = UglyToad.PdfPig.PdfDocument.Open(path))
                {
                    foreach (var page in document.GetPages())
                    {
                        sb.AppendLine($"--- Trang {page.Number} ---");
                        sb.AppendLine(page.Text);
                    }
                }
                var result = sb.ToString().Trim();
                if (string.IsNullOrWhiteSpace(result))
                {
                    return "Tài liệu PDF này không chứa văn bản dạng ký tự (có thể là file quét ảnh).";
                }
                return result;
            }
            catch (Exception ex)
            {
                return $"[Lỗi trích xuất văn bản từ PDF bằng PdfPig: {ex.Message}]";
            }
        }

        /// <summary>
        /// Trích xuất text thô từ file Word (.docx)
        /// </summary>
        private string ExtractTextFromDocx(string path)
        {
            try
            {
                using (var archive = ZipFile.OpenRead(path))
                {
                    var entry = archive.GetEntry("word/document.xml");
                    if (entry != null)
                    {
                        using (var stream = entry.Open())
                        using (var reader = new StreamReader(stream))
                        {
                            var xmlContent = reader.ReadToEnd();
                            // Trích xuất text từ thẻ <w:t>
                            var matches = Regex.Matches(xmlContent, @"<w:t[^>]*>(.*?)</w:t>");
                            var sb = new StringBuilder();
                            foreach (Match match in matches)
                            {
                                sb.Append(match.Groups[1].Value);
                            }
                            return System.Net.WebUtility.HtmlDecode(sb.ToString());
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return $"[Lỗi trích xuất DOCX: {ex.Message}]";
            }
            return string.Empty;
        }

        /// <summary>
        /// Trích xuất text thô từ file PowerPoint (.pptx)
        /// </summary>
        private string ExtractTextFromPptx(string path)
        {
            try
            {
                var sb = new StringBuilder();
                using (var archive = ZipFile.OpenRead(path))
                {
                    // Lọc tất cả các file slide xml
                    var slideEntries = archive.Entries
                        .Where(e => e.FullName.StartsWith("ppt/slides/slide") && e.FullName.EndsWith(".xml"))
                        .OrderBy(e =>
                        {
                            // Sắp xếp theo số slide đúng thứ tự
                            var match = Regex.Match(e.Name, @"\d+");
                            return match.Success ? int.Parse(match.Value) : 0;
                        });

                    foreach (var entry in slideEntries)
                    {
                        using (var stream = entry.Open())
                        using (var reader = new StreamReader(stream))
                        {
                            var xmlContent = reader.ReadToEnd();
                            // Trích xuất text từ thẻ <a:t>
                            var matches = Regex.Matches(xmlContent, @"<a:t[^>]*>(.*?)</a:t>");
                            sb.AppendLine($"--- Slide {entry.Name} ---");
                            foreach (Match match in matches)
                            {
                                sb.Append(match.Groups[1].Value).Append(" ");
                            }
                            sb.AppendLine();
                        }
                    }
                }
                return System.Net.WebUtility.HtmlDecode(sb.ToString());
            }
            catch (Exception ex)
            {
                return $"[Lỗi trích xuất PPTX: {ex.Message}]";
            }
        }
    }
}

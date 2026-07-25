using BussinessLayer.IServices;
using BussinessLayer.DTOs;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace BussinessLayer.Services
{
    public class GeminiService : IGeminiService
    {
        private static readonly HttpClient _httpClient = new HttpClient();
        private readonly string _apiKey;
        private readonly string _model;
        private readonly string _apiUrl;
        private readonly string _embeddingModel;

        // Số request tối đa mỗi lần gọi batchEmbedContents của Gemini.
        private const int EmbeddingBatchSize = 100;

        public GeminiService(IConfiguration configuration)
        {
            var geminiSection = configuration.GetSection("Gemini");
            _apiKey = geminiSection["ApiKey"] ?? string.Empty;
            _model = geminiSection["Model"] ?? "gemini-2.5-flash";
            _apiUrl = geminiSection["ApiUrl"] ?? "https://generativelanguage.googleapis.com/v1beta/models/";
            // gemini-embedding-001: model embedding ổn định, còn hỗ trợ tới 2028.
            // (text-embedding-004 đã bị Google shut down 14/01/2026.)
            _embeddingModel = geminiSection["EmbeddingModel"] ?? "gemini-embedding-001";
        }

        // ─────────────────────────────────────────────────────────────────────
        // CHAT (generateContent) — có retrieval top-K theo embedding
        // ─────────────────────────────────────────────────────────────────────
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
                            parts = new[] { new { text = msg.Text } }
                        });
                    }
                }

                // 2. Chuẩn bị phần nội dung của lượt chat hiện tại
                var currentParts = new List<object>();

                if (documentPaths != null)
                {
                    // 2a. Chọn ra các chunk LIÊN QUAN nhất tới câu hỏi (RAG) thay vì lấy N phần đầu.
                    var (selected, scannedPdfPaths) = await SelectRelevantChunksAsync(documentPaths, prompt);

                    foreach (var item in selected)
                    {
                        var header = item.Page.HasValue
                            ? $"[Tài liệu {item.FileName} - Trang {item.Page}]"
                            : $"[Tài liệu {item.FileName}]";
                        currentParts.Add(new { text = $"{header}\n{item.Text}" });
                    }

                    // 2b. PDF quét ảnh (không rút được text) → gửi thẳng file cho Gemini đọc.
                    foreach (var path in scannedPdfPaths)
                    {
                        try
                        {
                            var bytes = await File.ReadAllBytesAsync(path);
                            currentParts.Add(new
                            {
                                inlineData = new { mimeType = "application/pdf", data = Convert.ToBase64String(bytes) }
                            });
                        }
                        catch (Exception ex)
                        {
                            currentParts.Add(new { text = $"[Lỗi đọc file PDF {Path.GetFileName(path)}: {ex.Message}]" });
                        }
                    }
                }

                currentParts.Add(new { text = prompt });

                contents.Add(new { role = "user", parts = currentParts.ToArray() });

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

                var requestBody = new
                {
                    contents = contents.ToArray(),
                    system_instruction = new { parts = new[] { new { text = systemInstructionText } } }
                };

                var url = $"{_apiUrl.TrimEnd('/')}/{_model}:generateContent?key={_apiKey}";
                var jsonPayload = JsonSerializer.Serialize(requestBody);
                var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync(url, content);
                var responseString = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    return GeminiErrorHandler.HandleErrorResponse(response.StatusCode, responseString);
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

        /// <summary>
        /// Chọn các chunk liên quan nhất tới câu hỏi từ danh sách tài liệu.
        /// Ưu tiên dùng embedding đã lưu (chunks_*.json). Nếu không có embedding thì
        /// fallback lấy vài chunk đầu của mỗi tài liệu.
        /// </summary>
        private async Task<(List<RetrievedChunk> Selected, List<string> ScannedPdfPaths)> SelectRelevantChunksAsync(
            IEnumerable<string> documentPaths, string query)
        {
            const int topK = 6;          // số chunk liên quan lấy theo embedding
            const int fallbackPerDoc = 4; // số chunk đầu lấy khi tài liệu chưa có embedding
            const int maxParts = 8;       // trần tổng số phần đính kèm để tránh payload quá lớn

            var embedded = new List<RetrievedChunk>();   // chunk có embedding → xếp hạng theo cosine
            var fallback = new List<RetrievedChunk>();    // chunk không embedding → lấy phần đầu
            var scannedPdfs = new List<string>();

            foreach (var path in documentPaths ?? Enumerable.Empty<string>())
            {
                if (!File.Exists(path)) continue;
                var fileName = Path.GetFileName(path);

                // Ưu tiên đọc chunk đã tiền xử lý (kèm embedding) từ file JSON cạnh tài liệu.
                var saved = TryLoadSavedChunks(path);
                if (saved != null && saved.Count > 0)
                {
                    if (saved.Any(c => c.Embedding != null && c.Embedding.Length > 0))
                    {
                        foreach (var c in saved.Where(c => c.Embedding != null && c.Embedding.Length > 0))
                            embedded.Add(new RetrievedChunk(fileName, c.Text, c.Page, c.Embedding!));
                    }
                    else
                    {
                        foreach (var c in saved.Take(fallbackPerDoc))
                            fallback.Add(new RetrievedChunk(fileName, c.Text, c.Page, null));
                    }
                    continue;
                }

                // Chưa có file JSON → chunk tươi từ tài liệu.
                var fresh = await BuildChunksAsync(path);
                if (fresh.Count > 0)
                {
                    foreach (var c in fresh.Take(fallbackPerDoc))
                        fallback.Add(new RetrievedChunk(fileName, c.Text, c.Page, null));
                }
                else if (Path.GetExtension(path).ToLower() == ".pdf")
                {
                    scannedPdfs.Add(path); // PDF không có text → gửi nguyên file
                }
            }

            var selected = new List<RetrievedChunk>();

            // Xếp hạng theo cosine nếu có embedding và tạo được embedding cho câu hỏi.
            if (embedded.Count > 0)
            {
                var queryEmbedding = await EmbedTextAsync(query, "RETRIEVAL_QUERY");
                if (queryEmbedding != null)
                {
                    selected = embedded
                        .Select(c => new { Chunk = c, Score = CosineSimilarity(queryEmbedding, c.Embedding!) })
                        .OrderByDescending(x => x.Score)
                        .Take(topK)
                        .Select(x => x.Chunk)
                        .ToList();
                }
                else
                {
                    // Không tạo được embedding câu hỏi → lấy tạm phần đầu.
                    selected = embedded.Take(topK).ToList();
                }
            }

            // Bổ sung fallback (tài liệu chưa index) cho tới khi đạt trần.
            foreach (var c in fallback)
            {
                if (selected.Count >= maxParts) break;
                selected.Add(c);
            }

            if (selected.Count > maxParts)
                selected = selected.Take(maxParts).ToList();

            return (selected, scannedPdfs);
        }

        private sealed record RetrievedChunk(string FileName, string Text, int? Page, float[]? Embedding);

        // ─────────────────────────────────────────────────────────────────────
        // CHUNKING — trích xuất theo block (giữ số trang) + recursive splitter
        // ─────────────────────────────────────────────────────────────────────

        public async Task<IReadOnlyList<DocChunk>> BuildChunksAsync(string path, int maxWords = 300, int overlapWords = 50)
        {
            if (!File.Exists(path)) return Array.Empty<DocChunk>();

            var blocks = await ExtractBlocksAsync(path);
            if (blocks.Count == 0) return Array.Empty<DocChunk>();

            if (maxWords < 50) maxWords = 50;
            if (overlapWords < 0) overlapWords = 0;
            if (overlapWords >= maxWords) overlapWords = maxWords / 5; // giữ overlap < maxWords

            return RecursiveSplit(blocks, maxWords, overlapWords);
        }

        // Giữ tương thích chữ ký cũ — trả về danh sách text thuần.
        public async Task<IEnumerable<string>> GetDocumentTextChunksAsync(string path, int maxWords = 300, int overlapWords = 50)
        {
            var chunks = await BuildChunksAsync(path, maxWords, overlapWords);
            return chunks.Select(c => c.Text).ToList();
        }

        // Giữ tương thích chữ ký cũ. (Không còn nhồi tóm tắt toàn cục vào mỗi chunk vì
        // điều đó làm mọi embedding giống nhau, giảm chất lượng tìm kiếm.)
        public Task<IEnumerable<string>> GetContextualDocumentTextChunksAsync(string path, int maxWords = 300, int overlapWords = 50)
            => GetDocumentTextChunksAsync(path, maxWords, overlapWords);

        /// <summary>
        /// Tách tài liệu thành các "block" (đoạn văn) kèm số trang/slide.
        /// Không chèn marker "--- Trang N ---" vào text; số trang được giữ ở metadata.
        /// </summary>
        private async Task<List<(int? Page, string Text)>> ExtractBlocksAsync(string path)
        {
            var ext = Path.GetExtension(path).ToLower();
            try
            {
                return ext switch
                {
                    ".pdf" => ExtractPdfBlocks(path),
                    ".docx" => ExtractDocxBlocks(path),
                    ".pptx" => ExtractPptxBlocks(path),
                    ".txt" => SplitParagraphs(await File.ReadAllTextAsync(path)).Select(t => ((int?)null, t)).ToList(),
                    _ => new List<(int?, string)>()
                };
            }
            catch
            {
                return new List<(int?, string)>();
            }
        }

        private static List<string> SplitParagraphs(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return new List<string>();
            var paras = Regex.Split(text, @"\n\s*\n")
                             .Select(p => p.Trim())
                             .Where(p => p.Length > 0)
                             .ToList();
            // Nếu không có ranh giới đoạn (PDF thường như vậy), coi cả khối là 1 block.
            return paras.Count > 0 ? paras : new List<string> { text.Trim() };
        }

        private static List<(int? Page, string Text)> ExtractPdfBlocks(string path)
        {
            var blocks = new List<(int?, string)>();
            using var document = UglyToad.PdfPig.PdfDocument.Open(path);
            foreach (var page in document.GetPages())
            {
                if (string.IsNullOrWhiteSpace(page.Text)) continue;
                foreach (var para in SplitParagraphs(page.Text))
                    blocks.Add((page.Number, para));
            }
            return blocks;
        }

        private static List<(int? Page, string Text)> ExtractDocxBlocks(string path)
        {
            var blocks = new List<(int?, string)>();
            using var archive = ZipFile.OpenRead(path);
            var entry = archive.GetEntry("word/document.xml");
            if (entry == null) return blocks;

            using var stream = entry.Open();
            using var reader = new StreamReader(stream);
            var xml = reader.ReadToEnd();

            // Mỗi <w:p> là một đoạn văn; ghép text trong các <w:t> của đoạn đó.
            foreach (Match p in Regex.Matches(xml, @"<w:p[ >].*?</w:p>", RegexOptions.Singleline))
            {
                var sb = new StringBuilder();
                foreach (Match t in Regex.Matches(p.Value, @"<w:t[^>]*>(.*?)</w:t>", RegexOptions.Singleline))
                    sb.Append(WebUtility.HtmlDecode(t.Groups[1].Value));
                var para = sb.ToString().Trim();
                if (para.Length > 0) blocks.Add((null, para));
            }
            return blocks;
        }

        private static List<(int? Page, string Text)> ExtractPptxBlocks(string path)
        {
            var blocks = new List<(int?, string)>();
            using var archive = ZipFile.OpenRead(path);
            var slideEntries = archive.Entries
                .Where(e => e.FullName.StartsWith("ppt/slides/slide") && e.FullName.EndsWith(".xml"))
                .OrderBy(e => { var m = Regex.Match(e.Name, @"\d+"); return m.Success ? int.Parse(m.Value) : 0; });

            foreach (var entry in slideEntries)
            {
                var slideNo = int.TryParse(Regex.Match(entry.Name, @"\d+").Value, out var n) ? n : (int?)null;
                using var stream = entry.Open();
                using var reader = new StreamReader(stream);
                var xml = reader.ReadToEnd();
                var sb = new StringBuilder();
                foreach (Match t in Regex.Matches(xml, @"<a:t(?:\s+[^>]*)?>(.*?)</a:t>", RegexOptions.Singleline))
                    sb.Append(WebUtility.HtmlDecode(t.Groups[1].Value)).Append(' ');
                var text = sb.ToString().Trim();
                if (text.Length > 0) blocks.Add((slideNo, text));
            }
            return blocks;
        }

        /// <summary>
        /// Đóng gói các block thành chunk không vượt maxWords, có overlap giữa các chunk,
        /// tôn trọng ranh giới đoạn/câu (recursive) và giữ số trang của chunk.
        /// </summary>
        private static List<DocChunk> RecursiveSplit(List<(int? Page, string Text)> blocks, int maxWords, int overlapWords)
        {
            var chunks = new List<DocChunk>();
            var currentWords = new List<string>();
            int? currentPage = null;

            void Flush()
            {
                if (currentWords.Count == 0) return;
                chunks.Add(new DocChunk
                {
                    Index = chunks.Count,
                    Page = currentPage,
                    Text = CleanText(string.Join(" ", currentWords))
                });
                // Gieo overlap cho chunk kế tiếp.
                var overlap = overlapWords > 0 && currentWords.Count > overlapWords
                    ? currentWords.Skip(currentWords.Count - overlapWords).ToList()
                    : new List<string>();
                currentWords = overlap;
                currentPage = null; // trang sẽ được đặt lại theo nội dung mới thêm vào
            }

            // "unit" = một mảnh đã đảm bảo <= maxWords, kèm trang nguồn.
            foreach (var (page, text) in blocks)
            {
                foreach (var unit in SplitIntoUnits(text, maxWords))
                {
                    var unitWords = unit.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
                    if (unitWords.Length == 0) continue;

                    if (currentWords.Count + unitWords.Length > maxWords && currentWords.Count > 0)
                        Flush();

                    if (currentPage == null) currentPage = page;
                    currentWords.AddRange(unitWords);
                }
            }
            Flush();

            // Re-index sau khi có thể bỏ chunk rỗng.
            var result = chunks.Where(c => !string.IsNullOrWhiteSpace(c.Text)).ToList();
            for (int i = 0; i < result.Count; i++) result[i].Index = i;
            return result;
        }

        /// <summary>
        /// Chia một đoạn văn thành các mảnh &lt;= maxWords theo thứ tự: câu → cụm → từ.
        /// </summary>
        private static IEnumerable<string> SplitIntoUnits(string paragraph, int maxWords)
        {
            var words = paragraph.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
            if (words.Length <= maxWords)
            {
                yield return paragraph;
                yield break;
            }

            // Quá dài → tách theo câu.
            var sentences = Regex.Split(paragraph, @"(?<=[.!?…])\s+")
                                 .Where(s => !string.IsNullOrWhiteSpace(s));
            foreach (var sentence in sentences)
            {
                var sWords = sentence.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
                if (sWords.Length <= maxWords)
                {
                    yield return sentence.Trim();
                }
                else
                {
                    // Câu vẫn quá dài (PDF lỗi) → cắt cứng theo số từ.
                    for (int i = 0; i < sWords.Length; i += maxWords)
                        yield return string.Join(" ", sWords.Skip(i).Take(maxWords));
                }
            }
        }

        private static string CleanText(string text)
        {
            if (string.IsNullOrEmpty(text)) return string.Empty;
            // Gộp khoảng trắng thừa, bỏ ký tự điều khiển.
            text = Regex.Replace(text, @"[\u0000-\u0008\u000B\u000C\u000E-\u001F]", " ");
            text = Regex.Replace(text, @"\s+", " ");
            return text.Trim();
        }

        // ─────────────────────────────────────────────────────────────────────
        // EMBEDDING — Gemini text-embedding-004 (embedContent / batchEmbedContents)
        // ─────────────────────────────────────────────────────────────────────

        public async Task<IEnumerable<float>?> CreateTextEmbeddingAsync(string input)
        {
            var emb = await EmbedTextAsync(input, "RETRIEVAL_DOCUMENT");
            return emb;
        }

        /// <summary>Tạo embedding cho toàn bộ chunk theo lô. Trả về số chunk tạo được embedding.</summary>
        public async Task<int> EmbedChunksAsync(IList<DocChunk> chunks)
        {
            if (string.IsNullOrWhiteSpace(_apiKey) || chunks == null || chunks.Count == 0) return 0;

            int done = 0;
            for (int start = 0; start < chunks.Count; start += EmbeddingBatchSize)
            {
                var batch = chunks.Skip(start).Take(EmbeddingBatchSize).ToList();
                var vectors = await BatchEmbedAsync(batch.Select(c => c.Text), "RETRIEVAL_DOCUMENT");
                if (vectors == null) continue;

                for (int i = 0; i < batch.Count && i < vectors.Count; i++)
                {
                    if (vectors[i] != null)
                    {
                        batch[i].Embedding = vectors[i];
                        done++;
                    }
                }
            }
            return done;
        }

        private async Task<float[]?> EmbedTextAsync(string input, string taskType)
        {
            if (string.IsNullOrWhiteSpace(_apiKey) || string.IsNullOrWhiteSpace(input)) return null;
            try
            {
                var requestBody = new
                {
                    model = $"models/{_embeddingModel}",
                    content = new { parts = new[] { new { text = input } } },
                    taskType = taskType
                };

                var url = $"{_apiUrl.TrimEnd('/')}/{_embeddingModel}:embedContent?key={_apiKey}";
                var response = await PostWithRetryAsync(url, JsonSerializer.Serialize(requestBody));
                if (response == null || !response.IsSuccessStatusCode) return null;

                var responseString = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(responseString);
                if (doc.RootElement.TryGetProperty("embedding", out var embedding) &&
                    embedding.TryGetProperty("values", out var values) &&
                    values.ValueKind == JsonValueKind.Array)
                {
                    return values.EnumerateArray().Select(v => (float)v.GetDouble()).ToArray();
                }
                return null;
            }
            catch
            {
                return null;
            }
        }

        private async Task<List<float[]?>?> BatchEmbedAsync(IEnumerable<string> inputs, string taskType)
        {
            var texts = inputs.ToList();
            if (string.IsNullOrWhiteSpace(_apiKey) || texts.Count == 0) return null;
            try
            {
                var requestBody = new
                {
                    requests = texts.Select(t => new
                    {
                        model = $"models/{_embeddingModel}",
                        content = new { parts = new[] { new { text = t } } },
                        taskType = taskType
                    }).ToArray()
                };

                var url = $"{_apiUrl.TrimEnd('/')}/{_embeddingModel}:batchEmbedContents?key={_apiKey}";
                var response = await PostWithRetryAsync(url, JsonSerializer.Serialize(requestBody));
                if (response == null || !response.IsSuccessStatusCode) return null;

                var responseString = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(responseString);
                var result = new List<float[]?>();
                if (doc.RootElement.TryGetProperty("embeddings", out var embeddings) &&
                    embeddings.ValueKind == JsonValueKind.Array)
                {
                    foreach (var emb in embeddings.EnumerateArray())
                    {
                        if (emb.TryGetProperty("values", out var values) && values.ValueKind == JsonValueKind.Array)
                            result.Add(values.EnumerateArray().Select(v => (float)v.GetDouble()).ToArray());
                        else
                            result.Add(null);
                    }
                }
                return result;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>POST có retry đơn giản với backoff khi bị 429/5xx.</summary>
        private async Task<HttpResponseMessage?> PostWithRetryAsync(string url, string jsonPayload, int maxRetries = 3)
        {
            for (int attempt = 0; attempt <= maxRetries; attempt++)
            {
                try
                {
                    var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
                    var response = await _httpClient.PostAsync(url, content);
                    if (response.IsSuccessStatusCode) return response;

                    if ((response.StatusCode == HttpStatusCode.TooManyRequests ||
                         (int)response.StatusCode >= 500) && attempt < maxRetries)
                    {
                        await Task.Delay(TimeSpan.FromMilliseconds(500 * Math.Pow(2, attempt)));
                        continue;
                    }
                    return response;
                }
                catch when (attempt < maxRetries)
                {
                    await Task.Delay(TimeSpan.FromMilliseconds(500 * Math.Pow(2, attempt)));
                }
            }
            return null;
        }

        private static double CosineSimilarity(float[] a, float[] b)
        {
            if (a == null || b == null || a.Length == 0 || a.Length != b.Length) return 0;
            double dot = 0, na = 0, nb = 0;
            for (int i = 0; i < a.Length; i++)
            {
                dot += a[i] * b[i];
                na += a[i] * a[i];
                nb += b[i] * b[i];
            }
            if (na == 0 || nb == 0) return 0;
            return dot / (Math.Sqrt(na) * Math.Sqrt(nb));
        }

        /// <summary>Đọc chunk (kèm embedding) đã lưu ở chunks_{fileName}.json cạnh tài liệu.</summary>
        private static List<DocChunk>? TryLoadSavedChunks(string documentPath)
        {
            try
            {
                var dir = Path.GetDirectoryName(documentPath);
                if (dir == null) return null;
                var jsonPath = Path.Combine(dir, $"chunks_{Path.GetFileName(documentPath)}.json");
                if (!File.Exists(jsonPath)) return null;

                using var doc = JsonDocument.Parse(File.ReadAllText(jsonPath));
                if (doc.RootElement.TryGetProperty("chunkData", out var arr) && arr.ValueKind == JsonValueKind.Array)
                {
                    var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    var list = new List<DocChunk>();
                    foreach (var el in arr.EnumerateArray())
                    {
                        var c = el.Deserialize<DocChunk>(opts);
                        if (c != null && !string.IsNullOrWhiteSpace(c.Text)) list.Add(c);
                    }
                    return list;
                }

                // Tương thích ngược: file cũ chỉ có "chunks" (mảng string), không có embedding.
                if (doc.RootElement.TryGetProperty("chunks", out var chArr) && chArr.ValueKind == JsonValueKind.Array)
                {
                    var list = new List<DocChunk>();
                    int i = 0;
                    foreach (var el in chArr.EnumerateArray())
                    {
                        var text = el.GetString();
                        if (!string.IsNullOrWhiteSpace(text)) list.Add(new DocChunk { Index = i++, Text = text });
                    }
                    return list;
                }
                return null;
            }
            catch
            {
                return null;
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // TRÍCH XUẤT TEXT THÔ (giữ nguyên cho tính năng xem trước)
        // ─────────────────────────────────────────────────────────────────────

        public async Task<string> GetDocumentTextAsync(string path)
        {
            if (!File.Exists(path)) return "Tài liệu không tồn tại trên máy chủ.";

            var ext = Path.GetExtension(path).ToLower();
            if (ext == ".pdf") return ExtractTextFromPdf(path);
            if (ext == ".docx") return ExtractTextFromDocx(path);
            if (ext == ".pptx") return ExtractTextFromPptx(path);
            if (ext == ".txt")
            {
                try { return await File.ReadAllTextAsync(path); }
                catch (Exception ex) { return $"[Lỗi đọc file văn bản TXT: {ex.Message}]"; }
            }
            return "Định dạng tài liệu này hiện chưa hỗ trợ xem trước văn bản thô.";
        }

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
                    return "Tài liệu PDF này không chứa văn bản dạng ký tự (có thể là file quét ảnh).";
                return result;
            }
            catch (Exception ex)
            {
                return $"[Lỗi trích xuất văn bản từ PDF bằng PdfPig: {ex.Message}]";
            }
        }

        private string ExtractTextFromDocx(string path)
        {
            try
            {
                using var archive = ZipFile.OpenRead(path);
                var entry = archive.GetEntry("word/document.xml");
                if (entry != null)
                {
                    using var stream = entry.Open();
                    using var reader = new StreamReader(stream);
                    var xmlContent = reader.ReadToEnd();
                    var matches = Regex.Matches(xmlContent, @"<w:t[^>]*>(.*?)</w:t>");
                    var sb = new StringBuilder();
                    foreach (Match match in matches) sb.Append(match.Groups[1].Value);
                    return System.Net.WebUtility.HtmlDecode(sb.ToString());
                }
            }
            catch (Exception ex)
            {
                return $"[Lỗi trích xuất DOCX: {ex.Message}]";
            }
            return string.Empty;
        }

        private string ExtractTextFromPptx(string path)
        {
            try
            {
                var sb = new StringBuilder();
                using (var archive = ZipFile.OpenRead(path))
                {
                    var slideEntries = archive.Entries
                        .Where(e => e.FullName.StartsWith("ppt/slides/slide") && e.FullName.EndsWith(".xml"))
                        .OrderBy(e => { var match = Regex.Match(e.Name, @"\d+"); return match.Success ? int.Parse(match.Value) : 0; });

                    foreach (var entry in slideEntries)
                    {
                        using var stream = entry.Open();
                        using var reader = new StreamReader(stream);
                        var xmlContent = reader.ReadToEnd();
                        var matches = Regex.Matches(xmlContent, @"<a:t(?:\s+[^>]*)?>(.*?)</a:t>");
                        sb.AppendLine($"--- Slide {entry.Name} ---");
                        foreach (Match match in matches) sb.Append(match.Groups[1].Value).Append(" ");
                        sb.AppendLine();
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

using BussinessLayer.Interfaces;
using BussinessLayer.Services;
using BussinessLayer.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Hosting;
using System.Security.Claims;

namespace PresentationLayer.Controllers
{
    public class HomeController : Controller
    {
        private readonly ISubscriptionService _subscriptionService;
        private readonly IDocumentService _documentService;
        private readonly ISubjectService _subjectService;
        private readonly IGeminiService _geminiService;
        private readonly IWebHostEnvironment _webHostEnvironment;

        // Tiêm dịch vụ quản lý gói và AI vào HomeController
        public HomeController(
            ISubscriptionService subscriptionService,
            IDocumentService documentService,
            ISubjectService subjectService,
            IGeminiService geminiService,
            IWebHostEnvironment webHostEnvironment)
        {
            _subscriptionService = subscriptionService;
            _documentService = documentService;
            _subjectService = subjectService;
            _geminiService = geminiService;
            _webHostEnvironment = webHostEnvironment;
        }

        // Hàm Helper lấy nhanh UserId từ Cookie đăng nhập
        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return string.IsNullOrEmpty(userIdClaim) ? 3 : int.Parse(userIdClaim);
        }

        // Trang chủ mặc định: Chuyển hướng người dùng về trang đăng nhập nếu chưa xác thực, hoặc trang chức năng của họ
        public async Task<IActionResult> Index()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                if (User.IsInRole("Admin"))
                    return RedirectToAction("Dashboard", "Admin");

                if (User.IsInRole("Lecturer"))
                {
                    var userId = GetCurrentUserId();
                    var myDocs     = (await _documentService.GetDocumentsByUploadedByUserAsync(userId)).ToList();
                    var mySubjects = (await _subjectService.GetSubjectsByLecturerIdAsync(userId, includeDeleted: false)).ToList();

                    ViewBag.LecturerDocuments    = myDocs;
                    ViewBag.LecturerRecentDocs   = myDocs.OrderByDescending(d => d.UploadedDate).Take(5).ToList();
                    ViewBag.LecturerSubjects     = mySubjects;
                    ViewBag.LecturerTotalDocs    = myDocs.Count;
                    ViewBag.LecturerIndexedDocs  = myDocs.Count(d => d.Status == BussinessLayer.DTOs.DocumentStatus.Indexed);
                    ViewBag.LecturerSubjectCount = mySubjects.Count;

                    return View("LecturerDashboard");
                }

                if (User.IsInRole("Student"))
                {
                    var subjects = await _subjectService.GetAllSubjectsAsync(includeDeleted: false);
                    ViewBag.SubjectCount = subjects.Count();
                    return RedirectToAction("Chat");
                }
            }
            return RedirectToAction("Login", "Auth");
        }

        // Chỉ cho phép sinh viên (Student) truy cập vào chức năng Chat trực tuyến
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> Chat()
        {
            var documents = await _documentService.GetAllDocumentsAsync(includeDeleted: false);
            ViewBag.Documents = documents.ToList();
            // Trả về giao diện chat tương tác hiện đại của Sinh viên
            return View();
        }

        // --- HÀM TRỪ CÂU HỎI VỚI CƠ CHẾ RESET HÀNG NGÀY (24H) ---
        [Authorize(Roles = "Student")]
        [HttpPost]
        public IActionResult VerifyAndDeductQuestion()
        {
            int userId = GetCurrentUserId();

            // 1. Lấy thông tin gói hiện tại của học sinh (Dạng DTO)
            var currentSubDto = _subscriptionService.GetStudentSubscription(userId);

            if (currentSubDto == null)
            {
                return Json(new { success = false, message = "Không tìm thấy thông tin gói dịch vụ." });
            }

            // 2. Lấy cấu hình gói
            var planDto = _subscriptionService.GetPlanById(currentSubDto.SubscriptionPlanId);
            if (planDto == null)
            {
                return Json(new { success = false, message = "Không tìm thấy cấu hình gói." });
            }

            // 3. Kiểm tra & thực hiện Daily Reset nếu đã qua 24h
            if (planDto.QuestionLimit < 9999
                && currentSubDto.DailyResetTime.HasValue
                && DateTime.UtcNow >= currentSubDto.DailyResetTime.Value.AddHours(24))
            {
                // Đã qua 24h → Reset lại số câu hỏi và lưu ngay trạng thái reset xuống DB
                currentSubDto.RemainingQuestions = planDto.QuestionLimit;
                currentSubDto.DailyResetTime = null;
                _subscriptionService.SaveStudentSubscription(currentSubDto);
            }

            // 4. Kiểm tra còn lượt không
            if (planDto.QuestionLimit < 9999 && currentSubDto.RemainingQuestions <= 0)
            {
                // Tính thời gian còn lại trước khi reset
                string resetTimeIso = "";
                if (currentSubDto.DailyResetTime.HasValue)
                {
                    resetTimeIso = currentSubDto.DailyResetTime.Value.AddHours(24).ToString("o");
                }

                return Json(new
                {
                    success = false,
                    outOfQuota = true,
                    message = "Bạn đã hết lượt đặt câu hỏi hôm nay. Vui lòng chờ reset hoặc nâng cấp gói!",
                    resetTime = resetTimeIso
                });
            }

            // 5. Nếu gói không phải Vô hạn (Pro), tiến hành trừ 1 lượt hỏi
            if (planDto.QuestionLimit < 9999)
            {
                // Nếu chưa có DailyResetTime → đây là câu hỏi đầu tiên trong chu kỳ
                DateTime? newDailyResetTime = currentSubDto.DailyResetTime ?? DateTime.UtcNow;

                currentSubDto.RemainingQuestions -= 1;
                currentSubDto.DailyResetTime = newDailyResetTime;
                _subscriptionService.SaveStudentSubscription(currentSubDto);
            }

            // 6. Trả về kết quả kèm thời gian reset cho frontend
            string resetTimeResponse = "";
            if (currentSubDto.DailyResetTime.HasValue)
            {
                resetTimeResponse = currentSubDto.DailyResetTime.Value.AddHours(24).ToString("o");
            }

            return Json(new
            {
                success = true,
                remaining = currentSubDto.RemainingQuestions,
                resetTime = resetTimeResponse
            });
        }

        // --- DTO NHẬN DỮ LIỆU CHAT TỪ CLIENT ---
        public class ChatMessageRequestDto
        {
            public string Role { get; set; } = null!; // "user" hoặc "model"
            public string Text { get; set; } = null!;
        }

        public class ChatRequest
        {
            public string Message { get; set; } = null!;
            public ChatMessageRequestDto[]? History { get; set; }
            public int[]? SelectedDocIds { get; set; }
            public bool RestrictToDocs { get; set; }
        }

        // --- ENDPOINT XỬ LÝ GỬI TIN NHẮN CHAT VÀ GỌI GEMINI AI ---
        [Authorize(Roles = "Student")]
        [HttpPost]
        public async Task<IActionResult> SendChatMessage([FromBody] ChatRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Message))
            {
                return Json(new { success = false, message = "Vui lòng nhập tin nhắn của bạn." });
            }

            int userId = GetCurrentUserId();

            // 1. Kiểm tra & thực hiện trừ câu hỏi (Sao chép logic tương tự VerifyAndDeductQuestion)
            var currentSubDto = _subscriptionService.GetStudentSubscription(userId);
            if (currentSubDto == null)
            {
                return Json(new { success = false, message = "Không tìm thấy thông tin gói dịch vụ." });
            }

            var planDto = _subscriptionService.GetPlanById(currentSubDto.SubscriptionPlanId);
            if (planDto == null)
            {
                return Json(new { success = false, message = "Không tìm thấy cấu hình gói." });
            }

            // Kiểm tra & thực hiện Daily Reset nếu đã qua 24h
            if (planDto.QuestionLimit < 9999
                && currentSubDto.DailyResetTime.HasValue
                && DateTime.UtcNow >= currentSubDto.DailyResetTime.Value.AddHours(24))
            {
                currentSubDto.RemainingQuestions = planDto.QuestionLimit;
                currentSubDto.DailyResetTime = null;

                var resetEntity = new DataAccessLayer.Models.StudentSubscription
                {
                    Id = currentSubDto.Id,
                    UserId = currentSubDto.UserId,
                    SubscriptionPlanId = currentSubDto.SubscriptionPlanId,
                    StartDate = currentSubDto.StartDate,
                    EndDate = currentSubDto.EndDate,
                    RemainingQuestions = planDto.QuestionLimit,
                    DailyResetTime = null
                };
                _subscriptionService.SaveStudentSubscription(resetEntity);
            }

            // Kiểm tra xem học sinh còn lượt đặt câu hỏi không
            if (planDto.QuestionLimit < 9999 && currentSubDto.RemainingQuestions <= 0)
            {
                string resetTimeIso = "";
                if (currentSubDto.DailyResetTime.HasValue)
                {
                    resetTimeIso = currentSubDto.DailyResetTime.Value.AddHours(24).ToString("o");
                }

                return Json(new
                {
                    success = false,
                    outOfQuota = true,
                    message = "Bạn đã hết lượt đặt câu hỏi hôm nay. Vui lòng chờ reset hoặc nâng cấp gói!",
                    resetTime = resetTimeIso
                });
            }

            // Tiến hành trừ 1 lượt hỏi (nếu không phải gói vô hạn)
            if (planDto.QuestionLimit < 9999)
            {
                DateTime? newDailyResetTime = currentSubDto.DailyResetTime ?? DateTime.UtcNow;

                var entity = new DataAccessLayer.Models.StudentSubscription
                {
                    Id = currentSubDto.Id,
                    UserId = currentSubDto.UserId,
                    SubscriptionPlanId = currentSubDto.SubscriptionPlanId,
                    StartDate = currentSubDto.StartDate,
                    EndDate = currentSubDto.EndDate,
                    RemainingQuestions = currentSubDto.RemainingQuestions - 1,
                    DailyResetTime = newDailyResetTime
                };

                _subscriptionService.SaveStudentSubscription(entity);
                currentSubDto.RemainingQuestions = entity.RemainingQuestions;
                currentSubDto.DailyResetTime = newDailyResetTime;
            }

            // 2. Lấy danh sách đường dẫn tuyệt đối của các tài liệu được tích chọn
            var docPaths = new List<string>();
            if (request.SelectedDocIds != null && request.SelectedDocIds.Length > 0)
            {
                var wwwrootPath = _webHostEnvironment.WebRootPath;
                foreach (var docId in request.SelectedDocIds)
                {
                    var docDto = await _documentService.GetDocumentByIdAsync(docId);
                    if (docDto != null && docDto.Status == DocumentStatus.Indexed && !docDto.IsDeleted)
                    {
                        var fullPath = Path.Combine(wwwrootPath, docDto.FilePath.Replace("/", Path.DirectorySeparatorChar.ToString()));
                        if (System.IO.File.Exists(fullPath))
                        {
                            docPaths.Add(fullPath);
                        }
                    }
                }
            }

            // 3. Ánh xạ lịch sử hội thoại sang DTO của BLL
            var historyBll = request.History?.Select(h => new ChatMessageDto
            {
                Role = h.Role,
                Text = h.Text
            }) ?? Enumerable.Empty<ChatMessageDto>();

            // 4. Gọi Gemini Service để tạo câu trả lời
            var reply = await _geminiService.GenerateContentAsync(request.Message, historyBll, docPaths, request.RestrictToDocs);

            // 4. Trả về kết quả cho client
            string resetTimeResponse = "";
            if (currentSubDto.DailyResetTime.HasValue)
            {
                resetTimeResponse = currentSubDto.DailyResetTime.Value.AddHours(24).ToString("o");
            }

            return Json(new
            {
                success = true,
                reply = reply,
                remaining = currentSubDto.RemainingQuestions,
                resetTime = resetTimeResponse
            });
        }

        // --- ENDPOINT XEM TRƯỚC VĂN BẢN TRÍCH XUẤT CỦA TÀI LIỆU ---
        [Authorize(Roles = "Student")]
        [HttpGet]
        public async Task<IActionResult> GetDocumentText(int docId)
        {
            var docDto = await _documentService.GetDocumentByIdAsync(docId);
            if (docDto == null || docDto.IsDeleted)
            {
                return Json(new { success = false, message = "Không tìm thấy tài liệu này." });
            }

            var wwwrootPath = _webHostEnvironment.WebRootPath;
            var fullPath = Path.Combine(wwwrootPath, docDto.FilePath.Replace("/", Path.DirectorySeparatorChar.ToString()));

            if (!System.IO.File.Exists(fullPath))
            {
                return Json(new { success = false, message = "Tệp tin tài liệu không tồn tại trên hệ thống." });
            }

            var text = await _geminiService.GetDocumentTextAsync(fullPath);
            return Json(new { success = true, title = docDto.Title, fileType = docDto.FileType, text = text });
        }
    }
}

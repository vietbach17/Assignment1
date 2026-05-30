using BussinessLayer.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace PresentationLayer.Controllers
{
    // Sử dụng Attribute [Authorize(Roles = "Admin")] để chặn tất cả yêu cầu truy cập từ người dùng không phải Admin
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly IDocumentService _documentService;

        public AdminController(IDocumentService documentService)
        {
            _documentService = documentService;
        }

        public async Task<IActionResult> Dashboard()
        {
            // Lấy danh sách toàn bộ tài liệu đã upload để hiển thị cho Admin
            var documents = await _documentService.GetAllDocumentsAsync(includeDeleted: false);
            
            // Trả về trang quản trị dành riêng cho Admin kèm danh sách tài liệu
            return View(documents);
        }
    }
}
